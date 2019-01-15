using System;
using System.Net;
using System.Net.Sockets;
using UniRx;


namespace ReactiveConsole
{
    public static class ListenObservable
    {
        static void BeginAccept(TcpListener listener, IObserver<Socket> observer)
        {
            AsyncCallback callback = ar =>
            {
                var l = ar.AsyncState as TcpListener;
                try
                {
                    var socket = l.EndAcceptSocket(ar);
                    observer.OnNext(socket);

                    // next
                    BeginAccept(l, observer);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            };

            try
            {
                listener.BeginAcceptSocket(callback, listener);
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        }

        public static IObservable<Socket> Listen(int port)
        {
            return Listen(IPAddress.Any, port);
        }

        public static IObservable<Socket> Listen(IPAddress address, int port)
        {
            return Observable.Create<Socket>(observer =>
            {
                TcpListener listener = null;
                try
                {
                    listener = new TcpListener(address, port);

                    listener.Start();

                    BeginAccept(listener, observer);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }

                return Disposable.Create(() =>
                {
                    if (listener != null)
                    {
                        listener.Stop();
                    }
                });
            });
        }
    }
}
