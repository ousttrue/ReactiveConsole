using System;
using System.Text;

namespace ReactiveConsole
{
    public interface ILogFormatter
    {
        bool IsBinary { get; }
        ArraySegment<Byte> Format(LogEntry value);
    }

    public class SimpleJsonFormatter : ILogFormatter
    {
        Encoding UTF8 = new UTF8Encoding(false);

        public bool IsBinary
        {
            get { return false; }
        }

        public ArraySegment<byte> Format(LogEntry value)
        {
            var json = UnityEngine.JsonUtility.ToJson(value);
            return new ArraySegment<byte>(UTF8.GetBytes(json));
        }
    }
}
