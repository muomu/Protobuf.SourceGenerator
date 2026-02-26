using System.IO;

namespace Protobuf.SourceGenerator.Runtime
{
    public interface IProtobufSerializable
    {
        void WriteTo(Stream stream);
    }
}
