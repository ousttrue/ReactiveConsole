using System;
using System.Linq;


namespace ReactiveConsole
{
    public struct Utf8Bytes : IComparable<Utf8Bytes>
    {
        public static readonly System.Text.Encoding Encoding = new System.Text.UTF8Encoding(false);

        public readonly ArraySegment<Byte> Bytes;
        public int ByteLength
        {
            get { return Bytes.Count; }
        }

        public Utf8Iterator GetIterator()
        {
            return new Utf8Iterator(Bytes);
        }

        public int CompareTo(Utf8Bytes other)
        {
            int i = 0;
            for(;  i<ByteLength && i<other.ByteLength; ++i)
            {
                if (this[i] < other[i])
                {
                    return 1;
                }
                else if(this[i] > other[i])
                {
                    return -1;
                }
            }
            if (i < ByteLength)
            {
                return -1;
            }
            else if(i < other.ByteLength)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public Byte this[int i]
        {
            get { return Bytes.Array[Bytes.Offset + i]; }
        }

        public Utf8Bytes(ArraySegment<Byte> bytes)
        {
            Bytes = bytes;
        }

        public Utf8Bytes(Byte[] bytes, int offset, int count) : this(new ArraySegment<Byte>(bytes, offset, count))
        {
        }

        public Utf8Bytes(Byte[] bytes) : this(bytes, 0, bytes.Length)
        {
        }

        public static Utf8Bytes From(string src)
        {
            return new Utf8Bytes(Encoding.GetBytes(src));
        }

        public static Utf8Bytes From(string src, Byte[] bytes)
        {
            var required = src.Sum(c => Utf8Iterator.ByteLengthFromChar(c));
            if (required > bytes.Length)
            {
                throw new OverflowException();
            }
            int pos = 0;
            foreach (var c in src)
            {
                if (c <= Utf8Iterator.Mask7)
                {
                    // 1bit
                    bytes[pos++] = (byte)c;
                }
                else if (c <= Utf8Iterator.Mask11)
                {
                    // 2bit
                    bytes[pos++] = (byte)(Utf8Iterator.Head2 | Utf8Iterator.Mask5 & (c >> 6));
                    bytes[pos++] = (byte)(Utf8Iterator.Head1 | Utf8Iterator.Mask6 & (c));
                }
                else
                {
                    // 3bit
                    bytes[pos++] = (byte)(Utf8Iterator.Head3 | Utf8Iterator.Mask4 & (c >> 12));
                    bytes[pos++] = (byte)(Utf8Iterator.Head1 | Utf8Iterator.Mask6 & (c >> 6));
                    bytes[pos++] = (byte)(Utf8Iterator.Head1 | Utf8Iterator.Mask6 & (c));
                }
            }
            return new Utf8Bytes(new ArraySegment<byte>(bytes, 0, pos));
        }

        // -2147483648 ~ 2147483647
        public static Utf8Bytes From(int src)
        {
            if (src >= 0)
            {
                if (src < 10)
                {
                    return new Utf8Bytes(new byte[] {
                        (byte)(0x30 + src),
                    });
                }
                else if (src < 100)
                {
                    return new Utf8Bytes(new byte[] {
                        (byte)(0x30 + src/10),
                        (byte)(0x30 + src%10),
                    });
                }
                else if (src < 1000)
                {
                    return new Utf8Bytes(new byte[] {
                        (byte)(0x30 + src/100),
                        (byte)(0x30 + src/10),
                        (byte)(0x30 + src%10),
                    });
                }
                else if (src < 10000)
                {
                    return new Utf8Bytes(new byte[] {
                        (byte)(0x30 + src/1000),
                        (byte)(0x30 + src/100),
                        (byte)(0x30 + src/10),
                        (byte)(0x30 + src%10),
                    });
                }
                else if (src < 100000)
                {
                    return new Utf8Bytes(new byte[] {
                        (byte)(0x30 + src/10000),
                        (byte)(0x30 + src/1000),
                        (byte)(0x30 + src/100),
                        (byte)(0x30 + src/10),
                        (byte)(0x30 + src%10),
                    });
                }
                else if (src < 1000000)
                {
                    return new Utf8Bytes(new byte[] {
                        (byte)(0x30 + src/100000),
                        (byte)(0x30 + src/10000),
                        (byte)(0x30 + src/1000),
                        (byte)(0x30 + src/100),
                        (byte)(0x30 + src/10),
                        (byte)(0x30 + src%10),
                    });
                }
                else if (src < 10000000)
                {
                    return new Utf8Bytes(new byte[] {
                        (byte)(0x30 + src/1000000),
                        (byte)(0x30 + src/100000),
                        (byte)(0x30 + src/10000),
                        (byte)(0x30 + src/1000),
                        (byte)(0x30 + src/100),
                        (byte)(0x30 + src/10),
                        (byte)(0x30 + src%10),
                    });
                }
                else if (src < 100000000)
                {
                    return new Utf8Bytes(new byte[] {
                        (byte)(0x30 + src/10000000),
                        (byte)(0x30 + src/1000000),
                        (byte)(0x30 + src/100000),
                        (byte)(0x30 + src/10000),
                        (byte)(0x30 + src/1000),
                        (byte)(0x30 + src/100),
                        (byte)(0x30 + src/10),
                        (byte)(0x30 + src%10),
                    });
                }
                else if (src < 1000000000)
                {
                    return new Utf8Bytes(new byte[] {
                        (byte)(0x30 + src/100000000),
                        (byte)(0x30 + src/10000000),
                        (byte)(0x30 + src/1000000),
                        (byte)(0x30 + src/100000),
                        (byte)(0x30 + src/10000),
                        (byte)(0x30 + src/1000),
                        (byte)(0x30 + src/100),
                        (byte)(0x30 + src/10),
                        (byte)(0x30 + src%10),
                    });
                }
                else
                {
                    return new Utf8Bytes(new byte[] {
                        (byte)(0x30 + src/1000000000),
                        (byte)(0x30 + src/100000000),
                        (byte)(0x30 + src/10000000),
                        (byte)(0x30 + src/1000000),
                        (byte)(0x30 + src/100000),
                        (byte)(0x30 + src/10000),
                        (byte)(0x30 + src/1000),
                        (byte)(0x30 + src/100),
                        (byte)(0x30 + src/10),
                        (byte)(0x30 + src%10),
                    });
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public Utf8Bytes Concat(Utf8Bytes rhs)
        {
            var bytes = new Byte[ByteLength + rhs.ByteLength];
            Buffer.BlockCopy(Bytes.Array, Bytes.Offset, bytes, 0, ByteLength);
            Buffer.BlockCopy(rhs.Bytes.Array, rhs.Bytes.Offset, bytes, ByteLength, rhs.ByteLength);
            return new Utf8Bytes(bytes);
        }

        public override string ToString()
        {
            if (ByteLength == 0) return "";
            return Encoding.GetString(Bytes.Array, Bytes.Offset, Bytes.Count);
        }

        public string ToAscii()
        {
            if (ByteLength == 0) return "";
            return System.Text.Encoding.ASCII.GetString(Bytes.Array, Bytes.Offset, Bytes.Count);
        }

        public bool IsEmpty
        {
            get
            {
                return ByteLength == 0;
            }
        }

        public bool StartsWith(Utf8Bytes rhs)
        {
            if (rhs.ByteLength > ByteLength)
            {
                return false;
            }

            for (int i = 0; i < rhs.ByteLength; ++i)
            {
                if (this[i] != rhs[i])
                {
                    return false;
                }
            }

            return true;
        }

        public bool EndsWith(Utf8Bytes rhs)
        {
            if (rhs.ByteLength > ByteLength)
            {
                return false;
            }

            for (int i = 1; i <= rhs.ByteLength; ++i)
            {
                if (this[ByteLength - i] != rhs[rhs.ByteLength - i])
                {
                    return false;
                }
            }

            return true;
        }

        public int IndexOf(Byte code)
        {
            return IndexOf(0, code);
        }

        public int IndexOf(int offset, Byte code)
        {
            var pos = offset + Bytes.Offset;
            for (int i = 0; i < Bytes.Count; ++i, ++pos)
            {
                if (Bytes.Array[pos] == code)
                {
                    return pos - Bytes.Offset;
                }
            }
            return -1;
        }

        public Utf8Bytes Subbytes(int offset)
        {
            return Subbytes(offset, ByteLength - offset);
        }

        public Utf8Bytes Subbytes(int offset, int count)
        {
            return new Utf8Bytes(Bytes.Array, Bytes.Offset + offset, count);
        }

        static bool IsSpace(Byte b)
        {
            switch (b)
            {
                case 0x20:
                case 0x0a:
                case 0x0b:
                case 0x0c:
                case 0x0d:
                case 0x09:
                    return true;
            }

            return false;
        }

        public Utf8Bytes TrimStart()
        {
            var i = 0;
            for (; i < ByteLength; ++i)
            {
                if (!IsSpace(this[i]))
                {
                    break;
                }
            }
            return Subbytes(i);
        }

        public override bool Equals(Object obj)
        {
            return obj is Utf8Bytes && Equals((Utf8Bytes)obj);
        }

        public static bool operator ==(Utf8Bytes x, Utf8Bytes y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(Utf8Bytes x, Utf8Bytes y)
        {
            return !(x == y);
        }

        public bool Equals(Utf8Bytes other)
        {
            if (ByteLength != other.ByteLength)
            {
                return false;
            }

            for (int i = 0; i < ByteLength; ++i)
            {
                if (this[i] != other[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return ByteLength.GetHashCode();
        }

        public static Utf8Bytes operator +(Utf8Bytes l, Utf8Bytes r)
        {
            return new Utf8Bytes(l.Bytes.Concat(r.Bytes));
        }

        public bool IsInt
        {
            get
            {
                //bool isInt = false;
                for (int i = 0; i < ByteLength; ++i)
                {
                    var c = this[i];
                    if (c == '0'
                        || c == '1'
                        || c == '2'
                        || c == '3'
                        || c == '4'
                        || c == '5'
                        || c == '6'
                        || c == '7'
                        || c == '8'
                        || c == '9'
                        )
                    {
                        // ok
                        //isInt = true;
                    }
                    else if (i == 0 && c == '-')
                    {
                        // ok
                    }
                    else if (c == '.' || c == 'e')
                    {
                        return false;
                    }
                    else
                    {
                        break;
                    }
                }
                return true;
            }
        }

    }

}
