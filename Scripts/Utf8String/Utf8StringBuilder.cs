using System.Text;

namespace ReactiveConsole
{
    public class Utf8StringBuilder
    {
        ByteBuffer m_buffer = new ByteBuffer();

        public void Ascii(char c)
        {
            m_buffer.Push((byte)c);
        }

        static Encoding s_utf8 = new UTF8Encoding(false);

        public void Quote(string text)
        {
            Ascii('"');
            m_buffer.Push(s_utf8.GetBytes(text));
            Ascii('"');
        }

        public void Add(Utf8Bytes str)
        {
            m_buffer.Push(str.Bytes);
        }

        public Utf8Bytes ToUtf8String()
        {
            return new Utf8Bytes(m_buffer.Bytes);
        }
    }
}
