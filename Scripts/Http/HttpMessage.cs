using System;
using System.Collections.Generic;
using System.Text;
using ReactiveConsole;


namespace ReactiveConsole
{
    public abstract class HttpMessage
    {
        public static readonly Byte[] CRLF = new Byte[] { 0x0d, 0x0a };

        List<Utf8String> m_messages = new List<Utf8String>();
        public IList<Utf8String> Messages
        {
            get
            {
                return m_messages;
            }
        }
    }

    public static class Http10StatusLine
    {
        public static Utf8String Ok = Utf8String.From("HTTP/1.0 200 OK");
        public static Utf8String NotFound = Utf8String.From("HTTP/1.0 404 NOT FOUND");
        public static Utf8String InternalError = Utf8String.From("HTTP/1.0 500 INTERNAL ERROR");
    }

    public static class Http11StatusLine
    {
        public static Utf8String SwitchingProtocols = Utf8String.From("HTTP/1.1 101 Switching Protocols");
    }

    public class HttpRequest : HttpMessage
    {
        public Utf8String RequestLine
        {
            get;
            private set;
        }

        Utf8String m_slash = Utf8String.From("/");
        Utf8String m_index = Utf8String.From("index.html");
        Utf8String m_slash_index = Utf8String.From("/index.html");

        public Utf8String IndexPath
        {
            get
            {
                var path = Path;
                if (path.IsEmpty)
                {
                    path = m_slash_index;
                }

                if (path.EndsWith(m_slash))
                {
                    path = path.Concat(m_index);
                }
                return path;
            }
        }

        public Utf8String Path
        {
            get
            {
                var start = RequestLine.IndexOf(0x20);
                if (start < 0)
                {
                    return default(Utf8String);
                }

                var end = RequestLine.IndexOf(start + 1, 0x20);
                if (end < 0)
                {
                    return default(Utf8String);
                }

                return RequestLine.Subbytes(start + 1, end - start - 1);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(RequestLine);
            foreach (var line in Messages)
            {
                sb.Append(line);
            }
            return sb.ToString();
        }

        public HttpRequest(Utf8String line)
        {
            RequestLine = line;
        }

        static Utf8String s_upgrade_websocket = Utf8String.From("Upgrade: websocket");
        static Utf8String s_connection_upgrade = Utf8String.From("Connection: Upgrade");

        // Find
        // Upgrade: websocket
        // Connection: Upgrade
        public bool IsWebSocketUpgrade
        {
            get
            {
                bool hasUpgrade = false;
                bool hasConnection = false;
                foreach (var message in Messages)
                {
                    if (message == s_upgrade_websocket)
                    {
                        hasUpgrade = true;
                    }
                    else if (message == s_connection_upgrade)
                    {
                        hasConnection = true;
                    }
                }
                return hasUpgrade && hasConnection;
            }
        }

        public Utf8String GetHeader(Utf8String key)
        {
            foreach(var message in Messages)
            {
                if (message.StartsWith(key))
                {
                    var value = message.Subbytes(key.ByteLength);
                    if (value[0] == ':')
                    {
                        return value.Subbytes(1).TrimStart();
                    }
                }
            }

            return default(Utf8String);
        }

        static Utf8String s_wskey = Utf8String.From("Sec-WebSocket-Key");
        public Utf8String GetWebSocketKey()
        {
            return GetHeader(s_wskey);
        }

        static Utf8String s_wsversion = Utf8String.From("Sec-WebSocket-Version");
        public Utf8String GetWebSocketVersion()
        {
            return GetHeader(s_wsversion);
        }
    }
}
