using System;
using System.Net.Sockets;
using UniRx;


namespace ReactiveConsole
{
    class ThreadingSender : IDisposable
    {
        static readonly UniRx.Diagnostics.Logger Logger = new UniRx.Diagnostics.Logger("ThreadingSender");

        MonitorQueue<ArraySegment<Byte>> m_queue = new MonitorQueue<ArraySegment<byte>>();

        Socket m_socket;
        LockQueue<Byte[]> m_pool;

        public event Action Aborted;
        void RaiseAborted()
        {
            var handler = Aborted;
            if (handler != null)
            {
                handler();
            }
        }

        public void Dispose()
        {
            m_queue.Enqueue(default(ArraySegment<Byte>));
        }

        public ThreadingSender(Socket w, ReactiveConsole.LockQueue<Byte[]> pool)
        {
            m_socket = w;
            m_pool = pool;
            //w.Blocking = false;
            //w.SendBufferSize = 1024;
            System.Threading.Tasks.Task.Run(() => BeginSend());
        }

        public void Enqueue(ArraySegment<Byte> item)
        {
            m_queue.Enqueue(item);
        }

        void BeginSend()
        {
            var item = m_queue.Dequeue();
            var buffer = item.Array;
            if (buffer == null)
            {
                RaiseAborted();
                return;
            }

            AsyncCallback callback = ar =>
            {
                var s = (Socket)ar.AsyncState;
                try
                {
                    s.EndSend(ar);
                }
                finally
                {
                    //Logger.LogFormat("< ret pool: {0}", buffer);
                    m_pool.Enqueue(buffer);
                }

                BeginSend();
            };

            SocketError error;
            m_socket.BeginSend(item.Array, item.Offset, item.Count, SocketFlags.None, out error, callback, m_socket);
            if (error != SocketError.Success)
            {
                Logger.Warning(error);
                m_pool.Enqueue(buffer);
                RaiseAborted();
            }
        }
    }


    public class WebSocketSession : IDisposable
    {
        static readonly UniRx.Diagnostics.Logger Logger = new UniRx.Diagnostics.Logger("WebSocketSession");

        public ReactiveConsole.LockQueue<Byte[]> m_pool = new ReactiveConsole.LockQueue<byte[]>();

        /// <summary>
        /// Reader
        /// </summary>
        public IObservable<WebSocketFrame> Observable
        {
            get;
            private set;
        }

        public event Action Closed;
        public void Dispose()
        {
            Logger.Warning("[WebSocketSession] Dispose");
            m_sender.Dispose();
            var handler = Closed;
            if (handler != null)
            {
                handler();
            }
        }

        ThreadingSender m_sender;

        int m_frameSize;

        public WebSocketSession(Socket w, IObservable<WebSocketFrame> frameObservable, int frameSize)
        {
            m_frameSize = frameSize;
            Observable = frameObservable;
            m_sender = new ThreadingSender(w, m_pool);

            m_sender.Aborted += () =>
            {
                w.Close();
            };

            frameObservable.Subscribe(_ =>
            {

            },
            ex =>
            {
                Dispose();
            },
            () =>
            {
                Dispose();
            });
        }

        Byte[] GetOrCreateBuffer()
        {
            var item = m_pool.Dequeue();
            if (item != null)
            {
                return item;
            }

            //Logging.Debug("create buffer");
            Logger.Warning("> create buffer");
            return new byte[m_frameSize];
        }

        int GetHeaderSize()
        {
            /*
            if (payloadSize < 126)
            {
                return 2;
            }
            else if (payloadSize <= ushort.MaxValue)
            {
                return 4;
            }
            else
            {
                return 10;
            }
            */
            if (m_frameSize < ushort.MaxValue)
            {
                return 4;
            }
            else
            {
                return 10;
            }
        }

        public static void WriteHeader(ByteBuffer buffer, WebSocketFrameOpCode op, int payloadSize, bool fin)
        {
            Byte b0 = (Byte)(fin ? 0x80 : 0x00);
            b0 |= (Byte)op;
            buffer.Push(b0);

            if (payloadSize < 126)
            {
                Byte b1 = (Byte)payloadSize;
                buffer.Push(b1);
            }
            else if (payloadSize < UInt16.MaxValue)
            {
                Byte b1 = 126;
                buffer.Push(b1);

                var count = (UInt16)payloadSize;

                // network byte order(big endian)
                buffer.Push((Byte)(count >> 8));
                buffer.Push((Byte)(count & 0xFF));
            }
            else
            {
                Byte b1 = 127;
                buffer.Push(b1);

                var count = (UInt64)payloadSize;

                // network byte order(big endian)
                buffer.Push((Byte)(count >> 56));
                buffer.Push((Byte)(count >> 48));
                buffer.Push((Byte)(count >> 40));
                buffer.Push((Byte)(count >> 32));
                buffer.Push((Byte)(count >> 24));
                buffer.Push((Byte)(count >> 16));
                buffer.Push((Byte)(count >> 8));
                buffer.Push((Byte)(count & 0xFF));
            }
        }

        struct Splited
        {
            public ArraySegment<Byte> First;
            public ArraySegment<Byte> Second;
        }

        static Splited Split(ArraySegment<Byte> src, int count)
        {
            if (count > src.Count)
            {
                throw new Exception();
            }

            return new Splited
            {
                First = new ArraySegment<byte>(src.Array, src.Offset, count),
                Second = new ArraySegment<byte>(src.Array, src.Offset + count, src.Count - count)
            };
        }

        object m_lock = new object();

        /// <summary>
        /// フレームに分割してエンキューする
        /// </summary>
        /// <param name="op"></param>
        /// <param name="bytes"></param>
        public void SendFrame(WebSocketFrameOpCode op, ArraySegment<Byte> bytes)
        {
            lock (m_lock)
            {
                var headerSize = GetHeaderSize();
                if ((bytes.Count + headerSize) <= m_frameSize)
                {
                    // 分割無し
                    var item = GetOrCreateBuffer();
                    var buffer = new ByteBuffer(item);
                    WriteHeader(buffer, op, bytes.Count, true);
                    buffer.Push(bytes);
                    m_sender.Enqueue(buffer.Bytes);
                    return;
                }

                int payloadSize = 0;
                {
                    //Logger.LogFormat("first: {0}/{1}", payloadSize, bytes.Count);
                    // 先頭バッファ
                    var item = GetOrCreateBuffer();
                    var buffer = new ByteBuffer(item);
                    payloadSize = item.Length - headerSize;
                    WriteHeader(buffer, op, payloadSize, false);
                    var splited = Split(bytes, payloadSize);
                    buffer.Push(splited.First);
                    m_sender.Enqueue(buffer.Bytes);
                    bytes = splited.Second;
                }

                while (bytes.Count > 0)
                {
                    var item = GetOrCreateBuffer();
                    var buffer = new ByteBuffer(item);
                    if (bytes.Count <= payloadSize)
                    {
                        //Logger.LogFormat("fin: {0}", bytes.Count);
                        // last
                        WriteHeader(buffer, WebSocketFrameOpCode.Continuours, bytes.Count, true);
                        buffer.Push(bytes);
                        m_sender.Enqueue(buffer.Bytes);
                        break;
                    }
                    else
                    {
                        //Logger.LogFormat("continue: {0}", payloadSize);
                        // 継続
                        WriteHeader(buffer, WebSocketFrameOpCode.Continuours, payloadSize, false);
                        var splited = Split(bytes, payloadSize);
                        buffer.Push(splited.First);
                        m_sender.Enqueue(buffer.Bytes);
                        bytes = splited.Second;
                    }
                }
            }
        }
    }
}
