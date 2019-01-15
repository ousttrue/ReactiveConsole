using System;
using ReactiveConsole;


namespace ReactiveConsole
{
    public enum WebSocketFrameOpCode
    {
        Continuours,
        Text,
        Binary,
    }

    public struct WebSocketFrame
    {
        public bool IsFin
        {
            get;
            private set;
        }

        public WebSocketFrameOpCode OpCode
        {
            get;
            private set;
        }

        public bool HasMask
        {
            get;
            private set;
        }

        public Int64 PayloadSize
        {
            get;
            private set;
        }

        public Int64 Size
        {
            get
            {
                return MaskKeyPosition + 4 + PayloadSize;
            }
        }

        public Int32 MaskKeyPosition
        {
            get;
            private set;
        }

        public ArraySegment<Byte> MaskKey
        {
            get;
            private set;
        }

        public ArraySegment<Byte> Payload
        {
            get;
            private set;
        }

        static void DecodePayload(ArraySegment<Byte> bytes, ArraySegment<Byte> key)
        {
            for (int i = 0; i < bytes.Count; ++i)
            {
                var unmasked = (Byte)(bytes.Get(i) ^ key.Get(i % 4));
                bytes.Set(i, unmasked);
            }
        }

        public bool Parse(ArraySegment<Byte> bytes)
        {
            if (bytes.Count < 2)
            {
                return false;
            }

            var b0 = bytes.Get(0);
            IsFin = (b0 & 0x80) != 0;
            if (!IsFin)
            {
                throw new NotImplementedException();
            }
            OpCode = (WebSocketFrameOpCode)(b0 & 0x0F);

            var b1 = bytes.Get(1);
            HasMask = (b1 & 0x80) != 0;
            if (!HasMask)
            {
                throw new ArgumentException();
            }
            var payload0 = (Byte)(b1 & 0x7F);
            switch (payload0)
            {
                case 127:
                    if (bytes.Count < 14)
                    {
                        return false;
                    }
                    PayloadSize = BitConverter.ToInt64(bytes.Array, bytes.Offset + 2);
                    if (bytes.Count < 14 + PayloadSize)
                    {
                        return false;
                    }
                    MaskKeyPosition = 10;
                    break;

                case 126:
                    if (bytes.Count < 8)
                    {
                        return false;
                    }
                    PayloadSize = BitConverter.ToUInt16(bytes.Array, bytes.Offset + 2);
                    if (bytes.Count < 8 + PayloadSize)
                    {
                        return false;
                    }
                    MaskKeyPosition = 4;
                    break;

                default:
                    if (bytes.Count < 6)
                    {
                        return false;
                    }
                    PayloadSize = payload0;
                    if (bytes.Count < 6 + PayloadSize)
                    {
                        return false;
                    }
                    MaskKeyPosition = 2;
                    break;
            }
            MaskKey = new ArraySegment<byte>(bytes.Array, bytes.Offset + MaskKeyPosition, 4);
            if (PayloadSize > int.MaxValue)
            {
                throw new OverflowException();
            }
            Payload = new ArraySegment<byte>(bytes.Array, bytes.Offset + MaskKeyPosition + 4, (int)PayloadSize);

            DecodePayload(Payload, MaskKey);

            return true;
        }

        public override string ToString()
        {
            if (OpCode == WebSocketFrameOpCode.Text)
            {
                return String.Format("FIN={0} Op={1} Payload={2}", IsFin, OpCode, new Utf8Bytes(Payload));
            }
            else
            {
                return String.Format("FIN={0} Op={1} Payload={2}", IsFin, OpCode, PayloadSize);
            }
        }
    }
}
