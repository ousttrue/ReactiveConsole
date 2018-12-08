using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UniRx;


namespace ReactiveConsole
{
    public class HttpDispatcher
    {
        uint m_nextSessionID = 1;

        Dictionary<Socket, HttpSession> m_connectionMap = new Dictionary<Socket, HttpSession>();

        IHttpRequestSolver m_solver;

        Subject<WebSocketSession> m_wsOpenedSubject = new Subject<WebSocketSession>();
        public IObservable<WebSocketSession> WebSocketOpened
        {
            get
            {
                return m_wsOpenedSubject.AsObservable();
            }
        }

        public HttpDispatcher(IHttpRequestSolver solver)
        {
            m_solver = solver;
        }

        public void Process(Socket socket, Byte[] readBuffer)
        {
            var session = new HttpSession(m_nextSessionID++, socket, m_solver);
            lock (((ICollection)m_connectionMap).SyncRoot)
            {
                m_connectionMap.Add(socket, session);
            }

            session.Ended += () =>
            {
                lock (((ICollection)m_connectionMap).SyncRoot)
                {
                    m_connectionMap.Remove(socket);
                }
            };

            session.WebSocketAccepted += observable =>
            {
                m_wsOpenedSubject.OnNext(new WebSocketSession(
                    session.Socket, observable, 512));
            };

            session.Start(readBuffer);
        }
    }
}
