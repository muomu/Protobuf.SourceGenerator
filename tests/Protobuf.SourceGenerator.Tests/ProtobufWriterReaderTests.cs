using System.IO;
using Protobuf.SourceGenerator.Runtime;
using Xunit;

namespace Protobuf.SourceGenerator.Tests
{
    public class ProtobufWriterReaderTests
    {
        [Fact]
        public void RoundTrip_Int32()
        {
            var ms = new MemoryStream();
            var writer = new ProtobufWriter(ms);
            writer.WriteInt32(1, 42);
            ms.Position = 0;
            var reader = new ProtobufReader(ms);
            Assert.True(reader.MoveNext());
            Assert.Equal(1, reader.FieldNumber);
            Assert.Equal(0, reader.WireType);
            Assert.Equal(42, reader.ReadInt32());
        }

        [Fact]
        public void RoundTrip_String()
        {
            var ms = new MemoryStream();
            var writer = new ProtobufWriter(ms);
            writer.WriteString(2, "hello");
            ms.Position = 0;
            var reader = new ProtobufReader(ms);
            Assert.True(reader.MoveNext());
            Assert.Equal(2, reader.FieldNumber);
            Assert.Equal(2, reader.WireType);
            Assert.Equal("hello", reader.ReadString());
        }

        [Fact]
        public void RoundTrip_Bool()
        {
            var ms = new MemoryStream();
            var writer = new ProtobufWriter(ms);
            writer.WriteBool(3, true);
            ms.Position = 0;
            var reader = new ProtobufReader(ms);
            Assert.True(reader.MoveNext());
            Assert.Equal(3, reader.FieldNumber);
            Assert.True(reader.ReadBool());
        }

        [Fact]
        public void RoundTrip_Float()
        {
            var ms = new MemoryStream();
            var writer = new ProtobufWriter(ms);
            writer.WriteFloat(4, 3.14f);
            ms.Position = 0;
            var reader = new ProtobufReader(ms);
            Assert.True(reader.MoveNext());
            Assert.Equal(4, reader.FieldNumber);
            Assert.Equal(5, reader.WireType);
            Assert.Equal(3.14f, reader.ReadFloat());
        }

        [Fact]
        public void RoundTrip_Double()
        {
            var ms = new MemoryStream();
            var writer = new ProtobufWriter(ms);
            writer.WriteDouble(5, 3.14159265);
            ms.Position = 0;
            var reader = new ProtobufReader(ms);
            Assert.True(reader.MoveNext());
            Assert.Equal(5, reader.FieldNumber);
            Assert.Equal(1, reader.WireType);
            Assert.Equal(3.14159265, reader.ReadDouble());
        }

        [Fact]
        public void RoundTrip_Int64()
        {
            var ms = new MemoryStream();
            var writer = new ProtobufWriter(ms);
            writer.WriteInt64(6, long.MaxValue);
            ms.Position = 0;
            var reader = new ProtobufReader(ms);
            Assert.True(reader.MoveNext());
            Assert.Equal(6, reader.FieldNumber);
            Assert.Equal(long.MaxValue, reader.ReadInt64());
        }

        [Fact]
        public void RoundTrip_Bytes()
        {
            var ms = new MemoryStream();
            var writer = new ProtobufWriter(ms);
            writer.WriteBytes(7, new byte[] { 1, 2, 3, 4 });
            ms.Position = 0;
            var reader = new ProtobufReader(ms);
            Assert.True(reader.MoveNext());
            Assert.Equal(7, reader.FieldNumber);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, reader.ReadBytes());
        }

        [Fact]
        public void RoundTrip_MultipleFields()
        {
            var ms = new MemoryStream();
            var writer = new ProtobufWriter(ms);
            writer.WriteString(1, "Alice");
            writer.WriteInt32(2, 30);
            writer.WriteBool(3, true);
            ms.Position = 0;

            var reader = new ProtobufReader(ms);

            Assert.True(reader.MoveNext());
            Assert.Equal(1, reader.FieldNumber);
            Assert.Equal("Alice", reader.ReadString());

            Assert.True(reader.MoveNext());
            Assert.Equal(2, reader.FieldNumber);
            Assert.Equal(30, reader.ReadInt32());

            Assert.True(reader.MoveNext());
            Assert.Equal(3, reader.FieldNumber);
            Assert.True(reader.ReadBool());

            Assert.False(reader.MoveNext());
        }

        [Fact]
        public void SkipField_Works()
        {
            var ms = new MemoryStream();
            var writer = new ProtobufWriter(ms);
            writer.WriteInt32(1, 100);
            writer.WriteString(2, "skip me");
            writer.WriteInt32(3, 200);
            ms.Position = 0;

            var reader = new ProtobufReader(ms);

            Assert.True(reader.MoveNext());
            Assert.Equal(1, reader.FieldNumber);
            Assert.Equal(100, reader.ReadInt32());

            Assert.True(reader.MoveNext());
            Assert.Equal(2, reader.FieldNumber);
            reader.SkipField(); // skip the string

            Assert.True(reader.MoveNext());
            Assert.Equal(3, reader.FieldNumber);
            Assert.Equal(200, reader.ReadInt32());
        }

        [Fact]
        public void RoundTrip_RepeatedInt32()
        {
            var ms = new MemoryStream();
            var writer = new ProtobufWriter(ms);
            writer.WriteRepeatedInt32(1, new System.Collections.Generic.List<int> { 10, 20, 30 });
            ms.Position = 0;

            var reader = new ProtobufReader(ms);
            var results = new System.Collections.Generic.List<int>();
            while (reader.MoveNext())
            {
                Assert.Equal(1, reader.FieldNumber);
                results.Add(reader.ReadInt32());
            }
            Assert.Equal(new System.Collections.Generic.List<int> { 10, 20, 30 }, results);
        }

        [Fact]
        public void RoundTrip_EmbeddedMessage()
        {
            // Write a person embedded in another message
            var ms = new MemoryStream();
            var writer = new ProtobufWriter(ms);

            // Manually write a nested message (field 1, wire type 2)
            using (var innerMs = new MemoryStream())
            {
                var innerWriter = new ProtobufWriter(innerMs);
                innerWriter.WriteString(1, "Bob");
                innerWriter.WriteInt32(2, 25);

                var innerBytes = innerMs.ToArray();
                writer.WriteTag(1, 2);
                writer.WriteVarint((ulong)innerBytes.Length);
                writer.WriteRawBytes(innerBytes);
            }

            ms.Position = 0;
            var reader = new ProtobufReader(ms);
            Assert.True(reader.MoveNext());
            Assert.Equal(1, reader.FieldNumber);
            Assert.Equal(2, reader.WireType);

            var innerData = reader.ReadBytes();
            using (var innerMs = new MemoryStream(innerData))
            {
                var innerReader = new ProtobufReader(innerMs);
                Assert.True(innerReader.MoveNext());
                Assert.Equal(1, innerReader.FieldNumber);
                Assert.Equal("Bob", innerReader.ReadString());

                Assert.True(innerReader.MoveNext());
                Assert.Equal(2, innerReader.FieldNumber);
                Assert.Equal(25, innerReader.ReadInt32());
            }
        }
    }
}
