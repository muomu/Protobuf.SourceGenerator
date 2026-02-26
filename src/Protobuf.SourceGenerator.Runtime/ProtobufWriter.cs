using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Protobuf.SourceGenerator.Runtime
{
    public class ProtobufWriter
    {
        private readonly Stream _stream;

        public ProtobufWriter(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public void WriteTag(int fieldNumber, int wireType)
        {
            WriteVarint((ulong)((fieldNumber << 3) | wireType));
        }

        public void WriteInt32(int fieldNumber, int value)
        {
            WriteTag(fieldNumber, 0);
            WriteVarint((ulong)(long)value);
        }

        public void WriteInt64(int fieldNumber, long value)
        {
            WriteTag(fieldNumber, 0);
            WriteVarint((ulong)value);
        }

        public void WriteBool(int fieldNumber, bool value)
        {
            WriteTag(fieldNumber, 0);
            WriteVarint(value ? 1UL : 0UL);
        }

        public void WriteFloat(int fieldNumber, float value)
        {
            WriteTag(fieldNumber, 5);
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
            _stream.Write(bytes, 0, 4);
        }

        public void WriteDouble(int fieldNumber, double value)
        {
            WriteTag(fieldNumber, 1);
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
            _stream.Write(bytes, 0, 8);
        }

        public void WriteString(int fieldNumber, string value)
        {
            if (value == null) return;
            WriteTag(fieldNumber, 2);
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            WriteVarint((ulong)bytes.Length);
            _stream.Write(bytes, 0, bytes.Length);
        }

        public void WriteBytes(int fieldNumber, byte[] value)
        {
            if (value == null) return;
            WriteTag(fieldNumber, 2);
            WriteVarint((ulong)value.Length);
            _stream.Write(value, 0, value.Length);
        }

        public void WriteMessage<T>(int fieldNumber, T value) where T : IProtobufSerializable
        {
            if (value is null) return;
            using (var ms = new MemoryStream())
            {
                value.WriteTo(ms);
                byte[] data = ms.ToArray();
                WriteTag(fieldNumber, 2);
                WriteVarint((ulong)data.Length);
                _stream.Write(data, 0, data.Length);
            }
        }

        public void WriteRepeatedInt32(int fieldNumber, List<int> values)
        {
            if (values == null) return;
            foreach (var v in values) WriteInt32(fieldNumber, v);
        }

        public void WriteRepeatedInt64(int fieldNumber, List<long> values)
        {
            if (values == null) return;
            foreach (var v in values) WriteInt64(fieldNumber, v);
        }

        public void WriteRepeatedBool(int fieldNumber, List<bool> values)
        {
            if (values == null) return;
            foreach (var v in values) WriteBool(fieldNumber, v);
        }

        public void WriteRepeatedFloat(int fieldNumber, List<float> values)
        {
            if (values == null) return;
            foreach (var v in values) WriteFloat(fieldNumber, v);
        }

        public void WriteRepeatedDouble(int fieldNumber, List<double> values)
        {
            if (values == null) return;
            foreach (var v in values) WriteDouble(fieldNumber, v);
        }

        public void WriteRepeatedString(int fieldNumber, List<string> values)
        {
            if (values == null) return;
            foreach (var v in values) WriteString(fieldNumber, v);
        }

        public void WriteRepeatedBytes(int fieldNumber, List<byte[]> values)
        {
            if (values == null) return;
            foreach (var v in values) WriteBytes(fieldNumber, v);
        }

        public void WriteRepeatedMessage<T>(int fieldNumber, List<T> values) where T : IProtobufSerializable
        {
            if (values == null) return;
            foreach (var v in values) WriteMessage(fieldNumber, v);
        }

        public void WriteVarint(ulong value)
        {
            while (value > 127)
            {
                _stream.WriteByte((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }
            _stream.WriteByte((byte)value);
        }

        public void WriteRawBytes(byte[] bytes)
        {
            _stream.Write(bytes, 0, bytes.Length);
        }
    }
}
