using System;
using System.IO;
using System.Net.Sockets;


namespace ReactiveConsole
{
    public static class StreamExtensions
    {
        static byte[] s_crlf = new byte[] { 0x0d, 0x0a };

        public static void CRLF(this Stream s)
        {
            s.Write(s_crlf, 0, s_crlf.Length);
        }
    }
}
