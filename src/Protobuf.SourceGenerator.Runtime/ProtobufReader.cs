using System;
using System.IO;
using System.Text;

namespace Protobuf.SourceGenerator.Runtime
{
    public class ProtobufReader
    {
        private readonly Stream _stream;

        public ProtobufReader(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public int FieldNumber { get; private set; }
        public int WireType { get; private set; }

        public bool MoveNext()
        {
            if (_stream.Position >= _stream.Length) return false;
            ulong tag = ReadVarint();
            FieldNumber = (int)(tag >> 3);
            WireType = (int)(tag & 0x7);
            return true;
        }

        public int ReadInt32()
        {
            return (int)(long)ReadVarint();
        }

        public long ReadInt64()
        {
            return (long)ReadVarint();
        }

        public bool ReadBool()
        {
            return ReadVarint() != 0;
        }

        public float ReadFloat()
        {
            byte[] bytes = ReadRawBytes(4);
            if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }

        public double ReadDouble()
        {
            byte[] bytes = ReadRawBytes(8);
            if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToDouble(bytes, 0);
        }

        public string ReadString()
        {
            int length = (int)ReadVarint();
            byte[] bytes = ReadRawBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        public byte[] ReadBytes()
        {
            int length = (int)ReadVarint();
            return ReadRawBytes(length);
        }

        public T ReadMessage<T>(Func<Stream, T> parser)
        {
            int length = (int)ReadVarint();
            byte[] data = ReadRawBytes(length);
            using (var ms = new MemoryStream(data))
            {
                return parser(ms);
            }
        }

        public void SkipField()
        {
            switch (WireType)
            {
                case 0: // varint
                    ReadVarint();
                    break;
                case 1: // 64-bit
                    ReadRawBytes(8);
                    break;
                case 2: // length-delimited
                    int len = (int)ReadVarint();
                    ReadRawBytes(len);
                    break;
                case 5: // 32-bit
                    ReadRawBytes(4);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown wire type: {WireType}");
            }
        }

        private ulong ReadVarint()
        {
            ulong result = 0;
            int shift = 0;
            while (true)
            {
                int b = _stream.ReadByte();
                if (b < 0) throw new EndOfStreamException();
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0) break;
                shift += 7;
                if (shift >= 64) throw new InvalidOperationException("Varint too long");
            }
            return result;
        }

        private byte[] ReadRawBytes(int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = _stream.Read(buffer, offset, count - offset);
                if (read == 0) throw new EndOfStreamException();
                offset += read;
            }
            return buffer;
        }
    }
}
