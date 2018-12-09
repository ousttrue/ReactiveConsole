using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace ReactiveConsole
{
    public interface IHttpRequestSolver
    {
        bool Match(HttpRequest request);
        void Solve(Stream s, HttpSession session, HttpRequest request);
    }

    public class OkSolver : IHttpRequestSolver
    {
        Utf8Bytes m_body = Utf8Bytes.From("Hello");

        public bool Match(HttpRequest _)
        {
            return true;
        }

        public void Solve(Stream s, HttpSession session, HttpRequest request)
        {
            Logging.Info(string.Format("[{0}] 200 <= {1}", session.ID, request));

            // header
            Http10StatusLine.Ok.WriteTo(s);
            s.CRLF();
            // body
            m_body.WriteTo(s);
        }
    }

    public class FolderMounter : IHttpRequestSolver
    {
        Utf8Bytes m_mountPoint;
        string m_path;
        public delegate Utf8Bytes PathFilter(Utf8Bytes src);
        PathFilter m_filter;

        static readonly Utf8Bytes s_js = Utf8Bytes.From(".js");
        static readonly Utf8Bytes s_css = Utf8Bytes.From(".css");
        static readonly Utf8Bytes s_txt = Utf8Bytes.From(".txt");
        public static Utf8Bytes JsTxtFilter(Utf8Bytes src)
        {
            if (src.EndsWith(s_js) || src.EndsWith(s_css))
            {
                src = src.Concat(s_txt);
            }
            return src;
        }

        public FolderMounter(string mountPoint, string path, PathFilter pathFilter)
        {
            m_mountPoint = Utf8Bytes.From(mountPoint);
            m_path = path;
            m_filter = pathFilter;
        }

        public bool Match(HttpRequest request)
        {
            var path = request.Path;
            return path.StartsWith(m_mountPoint);
        }

        public void Solve(Stream s, HttpSession session, HttpRequest request)
        {
            var path = m_filter != null
                ? m_filter(request.IndexPath)
                : request.IndexPath
                ;

            var fullPath = Path.Combine(m_path, path.Subbytes(1).ToString());
            if (!File.Exists(fullPath))
            {
                // 404
                Logging.Info(string.Format("[{0}] 404 <= {1}", session.ID, request));
                Http10StatusLine.NotFound.WriteTo(s); s.CRLF();
                s.CRLF();
                return;
            }

            // 200
            Logging.Info(string.Format("[{0}] 200 <= {1}", session.ID, request));
            Http10StatusLine.Ok.WriteTo(s); s.CRLF();
            s.CRLF();

            // body
            using (var r = File.Open(fullPath, FileMode.Open, FileAccess.Read))
            {
                r.CopyTo(s);
            }
        }
    }

    public class FileMounter : IHttpRequestSolver
    {
        Utf8Bytes m_mountPoint;
        Utf8Bytes m_content;

        public FileMounter(String mountPoint, Byte[] content)
        {
            m_mountPoint = Utf8Bytes.From(mountPoint);
            m_content = new Utf8Bytes(content);
        }

        public bool Match(HttpRequest request)
        {
            var path = request.IndexPath;
            return m_mountPoint == path;
        }

        public void Solve(Stream s, HttpSession session, HttpRequest request)
        {
            // 200
            Logging.Info(string.Format("[{0}] 200 <= {1}", session.ID, request));
            Http10StatusLine.Ok.WriteTo(s); s.CRLF();
            s.CRLF();
            m_content.WriteTo(s);
        }
    }

    public class DispatchSolver : IHttpRequestSolver
    {
        List<IHttpRequestSolver> m_solvers = new List<IHttpRequestSolver>();
        public List<IHttpRequestSolver> Solvers
        {
            get { return m_solvers; }
        }

        IHttpRequestSolver m_defaultSolver = new OkSolver();

        public bool Match(HttpRequest request)
        {
            throw new System.NotImplementedException();
        }

        public void Solve(Stream s, HttpSession session, HttpRequest request)
        {
            var solver = m_solvers.FirstOrDefault(x => x.Match(request));
            if (solver == null)
            {
                solver = m_defaultSolver;
            }

            solver.Solve(s, session, request);
        }
    }
}
