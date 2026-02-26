using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Protobuf.SourceGenerator.Tests
{
    public class ProtoContractGeneratorTests
    {
        private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) RunGenerator(string source)
        {
            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { CSharpSyntaxTree.ParseText(source) },
                new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.IO.Stream).Assembly.Location),
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new ProtoContractGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            var result = driver.GetRunResult();
            var generatedSources = result.GeneratedTrees
                .Select(t => t.ToString())
                .ToArray();

            return (diagnostics, generatedSources);
        }

        [Fact]
        public void Generator_EmitsAttributeFiles()
        {
            var source = "// empty";
            var (_, sources) = RunGenerator(source);

            Assert.Contains(sources, s => s.Contains("ProtoContractAttribute"));
            Assert.Contains(sources, s => s.Contains("ProtoMemberAttribute"));
        }

        [Fact]
        public void Generator_GeneratesWriteToMethod()
        {
            var source = @"
using Protobuf.SourceGenerator;

[ProtoContract]
public partial class Person
{
    [ProtoMember(1)]
    public string Name { get; set; }

    [ProtoMember(2)]
    public int Age { get; set; }
}
";
            var (_, sources) = RunGenerator(source);
            var personSource = sources.FirstOrDefault(s => s.Contains("partial class Person"));

            Assert.NotNull(personSource);
            Assert.Contains("public void WriteTo", personSource);
            Assert.Contains("WriteString(1, Name)", personSource);
            Assert.Contains("WriteInt32(2, Age)", personSource);
        }

        [Fact]
        public void Generator_GeneratesParseFromMethod()
        {
            var source = @"
using Protobuf.SourceGenerator;

[ProtoContract]
public partial class Person
{
    [ProtoMember(1)]
    public string Name { get; set; }

    [ProtoMember(2)]
    public int Age { get; set; }
}
";
            var (_, sources) = RunGenerator(source);
            var personSource = sources.FirstOrDefault(s => s.Contains("partial class Person"));

            Assert.NotNull(personSource);
            Assert.Contains("public static Person ParseFrom", personSource);
            Assert.Contains("reader.ReadString()", personSource);
            Assert.Contains("reader.ReadInt32()", personSource);
        }

        [Fact]
        public void Generator_HandlesNamespace()
        {
            var source = @"
using Protobuf.SourceGenerator;

namespace MyApp.Models
{
    [ProtoContract]
    public partial class Order
    {
        [ProtoMember(1)]
        public int Id { get; set; }
    }
}
";
            var (_, sources) = RunGenerator(source);
            var orderSource = sources.FirstOrDefault(s => s.Contains("partial class Order"));

            Assert.NotNull(orderSource);
            Assert.Contains("namespace MyApp.Models", orderSource);
        }

        [Fact]
        public void Generator_HandlesBoolProperty()
        {
            var source = @"
using Protobuf.SourceGenerator;

[ProtoContract]
public partial class Flags
{
    [ProtoMember(1)]
    public bool IsActive { get; set; }
}
";
            var (_, sources) = RunGenerator(source);
            var flagsSource = sources.FirstOrDefault(s => s.Contains("partial class Flags"));

            Assert.NotNull(flagsSource);
            Assert.Contains("WriteBool(1, IsActive)", flagsSource);
            Assert.Contains("reader.ReadBool()", flagsSource);
        }

        [Fact]
        public void Generator_HandlesMultipleTypes()
        {
            var source = @"
using Protobuf.SourceGenerator;

[ProtoContract]
public partial class Message
{
    [ProtoMember(1)]
    public string Text { get; set; }

    [ProtoMember(2)]
    public int Count { get; set; }

    [ProtoMember(3)]
    public long Timestamp { get; set; }

    [ProtoMember(4)]
    public bool IsValid { get; set; }

    [ProtoMember(5)]
    public float Score { get; set; }

    [ProtoMember(6)]
    public double Precision { get; set; }

    [ProtoMember(7)]
    public byte[] Data { get; set; }
}
";
            var (_, sources) = RunGenerator(source);
            var msgSource = sources.FirstOrDefault(s => s.Contains("partial class Message"));

            Assert.NotNull(msgSource);
            Assert.Contains("WriteString(1, Text)", msgSource);
            Assert.Contains("WriteInt32(2, Count)", msgSource);
            Assert.Contains("WriteInt64(3, Timestamp)", msgSource);
            Assert.Contains("WriteBool(4, IsValid)", msgSource);
            Assert.Contains("WriteFloat(5, Score)", msgSource);
            Assert.Contains("WriteDouble(6, Precision)", msgSource);
            Assert.Contains("WriteBytes(7, Data)", msgSource);
        }

        [Fact]
        public void Generator_ImplementsIProtobufSerializable()
        {
            var source = @"
using Protobuf.SourceGenerator;

[ProtoContract]
public partial class Item
{
    [ProtoMember(1)]
    public string Name { get; set; }
}
";
            var (_, sources) = RunGenerator(source);
            var itemSource = sources.FirstOrDefault(s => s.Contains("partial class Item"));

            Assert.NotNull(itemSource);
            Assert.Contains("IProtobufSerializable", itemSource);
        }

        [Fact]
        public void Generator_SkipFieldForUnknown()
        {
            var source = @"
using Protobuf.SourceGenerator;

[ProtoContract]
public partial class Simple
{
    [ProtoMember(1)]
    public int Value { get; set; }
}
";
            var (_, sources) = RunGenerator(source);
            var simpleSource = sources.FirstOrDefault(s => s.Contains("partial class Simple"));

            Assert.NotNull(simpleSource);
            Assert.Contains("reader.SkipField()", simpleSource);
        }
    }
}
