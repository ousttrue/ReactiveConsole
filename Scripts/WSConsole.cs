using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;


namespace ReactiveConsole
{
    public class WSConsole : IDisposable
    {
        static readonly UniRx.Diagnostics.Logger Logger = new UniRx.Diagnostics.Logger("WSConsole");

        IDisposable m_disposable;
        public void Dispose()
        {
            m_disposable.Dispose();
            foreach (var s in Sessions)
            {
                s.Dispose();
            }
        }

        List<WebSocketSession> m_list = new List<WebSocketSession>();

        WebSocketSession[] Sessions
        {
            get
            {
                lock (((ICollection)m_list).SyncRoot)
                {
                    return m_list.ToArray();
                }
            }
        }

        void AddSession(WebSocketSession ws)
        {
            lock (((ICollection)m_list).SyncRoot)
            {
                m_list.Add(ws);
            }
        }

        void RemoveSession(WebSocketSession ws)
        {
            lock (((ICollection)m_list).SyncRoot)
            {
                m_list.Remove(ws);
            }
        }

        public void SendFrame(WebSocketFrameOpCode op, ArraySegment<byte> bytes)
        {
            var sessions = Sessions;
            foreach (var session in sessions)
            {
                try
                {
                    session.SendFrame(op, bytes);
                }
                catch (ObjectDisposedException)
                {
                    session.Dispose();
                }
                catch (Exception ex)
                {
                    //session.Dispose();
                    Logger.Exception(ex);
                }
            }
        }

        public WSConsole(int port, HttpDispatcher http)
        {
            // websocket handling
            http.WebSocketOpened
                .Subscribe(ws =>
                {
                    AddSession(ws);
                    ws.Closed += () =>
                    {
                        RemoveSession(ws);
                    };
                });

            // listen http
            m_disposable = ListenObservable.Listen(port).Subscribe(x =>
            {
                http.Process(x, new byte[1024]);
            },
            ex =>
            {
                Logging.Exception(ex);
            })
            ;
        }
    }
}
