using System;
using UniRx;


namespace ReactiveConsole
{
    public class WebSocketFrameReader: IDisposable
    {
        ByteBuffer m_buffer = new ByteBuffer();

        public void PushBytes(ArraySegment<Byte> bytes)
        {
            m_buffer.Push(bytes);

            while (true)
            {
                var frame = default(WebSocketFrame);
                if (!frame.Parse(m_buffer.Bytes))
                {
                    break;
                }

                m_subject.OnNext(frame);

                m_buffer.Unshift((Int32)frame.Size);
            }
        }

        public void Dispose()
        {
            //m_subject.Dispose();
            m_subject.OnCompleted();
        }

        Subject<WebSocketFrame> m_subject = new Subject<WebSocketFrame>();

        public IObservable<WebSocketFrame> FrameObservable
        {
            get
            {
                return m_subject.AsObservable();
            }
        }
    }
}
