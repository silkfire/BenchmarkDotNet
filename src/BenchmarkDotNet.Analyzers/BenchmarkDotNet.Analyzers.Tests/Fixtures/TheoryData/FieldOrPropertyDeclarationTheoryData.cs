using Xunit;

namespace BenchmarkDotNet.Analyzers.Tests.Fixtures;

internal sealed class FieldOrPropertyDeclarationTheoryData : TheoryData<string>
{
    public FieldOrPropertyDeclarationTheoryData()
    {
        AddRange(
                 "Property { get; init; }",
                 "_field;"
                );
    }
}
