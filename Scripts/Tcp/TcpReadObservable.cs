using System;
using System.Net.Sockets;
using UniRx;


namespace ReactiveConsole
{
    public static class TcpReadObservable
    {
        static void BeginRead(Socket socket, Byte[] buffer, IObserver<ArraySegment<Byte>> observer)
        {
            AsyncCallback callback = ar =>
            {
                try
                {
                    var s = ar.AsyncState as Socket;
                    var readSize = s.EndReceive(ar);
                    if (readSize == 0)
                    {
                        // closed
                        observer.OnCompleted();
                        return;
                    }

                    observer.OnNext(new ArraySegment<byte>(buffer, 0, readSize));

                    // next
                    BeginRead(s, buffer, observer);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            };

            try
            {
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, callback, socket);
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        }

        public static IObservable<ArraySegment<Byte>> Read(Socket socket, Byte[] bytes)
        {
            return Observable.Create<ArraySegment<Byte>>(observer =>
            {
                BeginRead(socket, bytes, observer);

                return Disposable.Create(() =>
                {
                    observer.OnCompleted();
                });
            });
        }
    }
}
