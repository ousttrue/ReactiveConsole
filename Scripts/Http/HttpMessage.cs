using System;
using System.Collections.Generic;
using System.Text;
using ReactiveConsole;


namespace ReactiveConsole
{
    public abstract class HttpMessage
    {
        public static readonly Byte[] CRLF = new Byte[] { 0x0d, 0x0a };

        List<Utf8Bytes> m_messages = new List<Utf8Bytes>();
        public IList<Utf8Bytes> Messages
        {
            get
            {
                return m_messages;
            }
        }
    }

    public static class Http10StatusLine
    {
        public static Utf8Bytes Ok = Utf8Bytes.From("HTTP/1.0 200 OK");
        public static Utf8Bytes NotFound = Utf8Bytes.From("HTTP/1.0 404 NOT FOUND");
        public static Utf8Bytes InternalError = Utf8Bytes.From("HTTP/1.0 500 INTERNAL ERROR");
    }

    public static class Http11StatusLine
    {
        public static Utf8Bytes SwitchingProtocols = Utf8Bytes.From("HTTP/1.1 101 Switching Protocols");
    }

    public class HttpRequest : HttpMessage
    {
        public Utf8Bytes RequestLine
        {
            get;
            private set;
        }

        Utf8Bytes m_slash = Utf8Bytes.From("/");
        Utf8Bytes m_index = Utf8Bytes.From("index.html");
        Utf8Bytes m_slash_index = Utf8Bytes.From("/index.html");

        public Utf8Bytes IndexPath
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

        public Utf8Bytes Path
        {
            get
            {
                var start = RequestLine.IndexOf(0x20);
                if (start < 0)
                {
                    return default(Utf8Bytes);
                }

                var end = RequestLine.IndexOf(start + 1, 0x20);
                if (end < 0)
                {
                    return default(Utf8Bytes);
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

        public HttpRequest(Utf8Bytes line)
        {
            RequestLine = line;
        }

        static Utf8Bytes s_upgrade_websocket = Utf8Bytes.From("Upgrade: websocket");
        static Utf8Bytes s_connection_upgrade = Utf8Bytes.From("Connection: Upgrade");

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

        public Utf8Bytes GetHeader(Utf8Bytes key)
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

            return default(Utf8Bytes);
        }

        static Utf8Bytes s_wskey = Utf8Bytes.From("Sec-WebSocket-Key");
        public Utf8Bytes GetWebSocketKey()
        {
            return GetHeader(s_wskey);
        }

        static Utf8Bytes s_wsversion = Utf8Bytes.From("Sec-WebSocket-Version");
        public Utf8Bytes GetWebSocketVersion()
        {
            return GetHeader(s_wsversion);
        }
    }
}
