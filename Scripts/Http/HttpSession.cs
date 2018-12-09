using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography;
using UniRx;
using ReactiveConsole;


namespace ReactiveConsole
{
    public class HttpSession : IDisposable
    {
        public Socket Socket
        {
            get;
            private set;
        }

        IDisposable m_reader;
        public void Dispose()
        {
            Logging.Debug(string.Format("{0} Dispose", ID));
            m_reader.Dispose();
            Socket.Close();
            RaiseEnded();
        }
        public event Action Ended;
        void RaiseEnded()
        {
            if (m_wsFrameReader != null)
            {
                m_wsFrameReader.Dispose();
            }

            var handler = Ended;
            if (handler != null)
            {
                handler();
            }
        }

        Byte[] m_buffer;
        public readonly uint ID;

        const Int32 CRLFCRLF = 0x0a0d0a0d;
        const Int16 CRLF = 0x0a0d;

        IHttpRequestSolver m_solver;

        public event Action<IObservable<WebSocketFrame>> WebSocketAccepted;
        void RaiseWebSocketAccepted(IObservable<WebSocketFrame> observable)
        {
            var handler = WebSocketAccepted;
            if (handler != null)
            {
                handler(observable);
            }
        }

        public HttpSession(uint sessionID, Socket socket, IHttpRequestSolver solver)
        {
            ID = sessionID;
            Socket = socket;
            m_solver = solver;
        }

        public void Start(Byte[] buffer)
        {
            m_reader = TcpReadObservable.Read(Socket, buffer).Subscribe(x =>
            {
                PushBytes(x);
            }, ex =>
            {
                RaiseEnded();
            }, () =>
            {
                RaiseEnded();
            });
        }

        public static Utf8Bytes AcceptWebSocketKey(Utf8Bytes key)
        {
            var concat = key + Utf8Bytes.From("258EAFA5-E914-47DA-95CA-C5AB0DC85B11");

            using (var sha1 = SHA1.Create())
            {
                var bs = sha1.ComputeHash(concat.Bytes.Array, concat.Bytes.Offset, concat.Bytes.Count);
                return Utf8Bytes.From(Convert.ToBase64String(bs));
            }
        }

        #region private
        void InternalError(Exception ex)
        {
            Logging.Error(String.Format("{0} - {1}", ID, ex));
            using (var s = new NetworkStream(Socket, false))
            {
                Http10StatusLine.InternalError.WriteTo(s); s.CRLF();
                s.CRLF();

                Utf8Bytes.From(ex.ToString()).WriteTo(s);
            }
            Dispose();
        }

        HttpRequest m_request;

        WebSocketFrameReader m_wsFrameReader;

        void PushBytes(ArraySegment<Byte> bytes)
        {
            if (m_request != null)
            {
                if (m_wsFrameReader != null)
                {
                    m_wsFrameReader.PushBytes(bytes);
                }
                else
                {
                    Logging.Warning("body");
                }

                return;
            }

            try
            {
                if (m_buffer == null)
                {
                    var header = Process(bytes);
                    if (header.Count == 0)
                    {
                        // header continue
                        m_buffer = new byte[bytes.Count];
                        Buffer.BlockCopy(bytes.Array, bytes.Offset, m_buffer, 0, bytes.Count);
                        return;
                    }

                    ParseHeader(header);
                }
                else
                {
                    // concat
                    m_buffer = m_buffer.Concat(bytes);
                    var header = Process(new ArraySegment<byte>(m_buffer));
                    if (header.Count == 0)
                    {
                        // header continue
                        return;
                    }

                    ParseHeader(header);
                }
            }
            catch(Exception ex)
            {
                InternalError(ex);
            }
        }

        ArraySegment<Byte> Process(ArraySegment<Byte> bytes)
        {
            var offset = bytes.Offset;
            for (int i = 0; i < bytes.Count - 3; ++i, ++offset)
            {
                var value = BitConverter.ToInt32(bytes.Array, offset);
                if (value == CRLFCRLF)
                {
                    //Logger.Debug("header end:" + offset);
                    return new ArraySegment<byte>(bytes.Array, bytes.Offset, offset - bytes.Offset);
                }
            }

            Logging.Debug("header continue");
            return new ArraySegment<byte>();
        }

        void ParseHeader(ArraySegment<Byte> bytes)
        {
            HttpRequest request = null;
            foreach (var line in EnumLines(bytes))
            {
                if (request == null)
                {
                    request = new HttpRequest(line);
                }
                else
                {
                    request.Messages.Add(line);
                }
            }
            Request(request);
        }

        IEnumerable<Utf8Bytes> EnumLines(ArraySegment<Byte> bytes)
        {
            var start = bytes.Offset;
            var offset = start;
            for (int i = 0; i < bytes.Count - 1; ++i, ++offset)
            {
                var value = BitConverter.ToInt16(bytes.Array, offset);
                if (value == CRLF)
                {
                    yield return new Utf8Bytes(bytes.Array, start, offset - start);
                    start = offset + 2;
                    i += 2;
                }
            }
        }

        static Utf8Bytes s_upgrade_websocket = Utf8Bytes.From("Upgrade: websocket");
        static Utf8Bytes s_connection_upgrade = Utf8Bytes.From("Connection: Upgrade");
        static Utf8Bytes s_websocket_accept = Utf8Bytes.From("Sec-WebSocket-Accept: ");

        void Request(HttpRequest request)
        {
            if (request == null)
            {
                throw new Exception("no http request");
            }
            m_request = request;

            if (request.IsWebSocketUpgrade)
            {
                // WebSocket session
                // handshake
                var key = request.GetWebSocketKey();
                //var version = request.GetWebSocketVersion();

                // s3pPLMBiTxaQ9kYGzzhZRbK+xOo=
                using (var s = new NetworkStream(Socket, false))
                {
                    Http11StatusLine.SwitchingProtocols.WriteTo(s); s.CRLF();
                    s_upgrade_websocket.WriteTo(s); s.CRLF();
                    s_connection_upgrade.WriteTo(s); s.CRLF();
                    s_websocket_accept.WriteTo(s); AcceptWebSocketKey(key).WriteTo(s); s.CRLF();

                    m_wsFrameReader = new WebSocketFrameReader();
                    RaiseWebSocketAccepted(m_wsFrameReader.FrameObservable);

                    s.CRLF();
                }
            }
            else
            {
                // Http session
                // Send response
                using (var s = new NetworkStream(Socket, false))
                {
                    m_solver.Solve(s, this, request);
                }

                Dispose();
            }
        }
        #endregion
    }
}
