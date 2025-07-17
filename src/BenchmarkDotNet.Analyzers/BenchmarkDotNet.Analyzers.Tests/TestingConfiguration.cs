using Microsoft.CodeAnalysis.Testing;

namespace BenchmarkDotNet.Analyzers.Tests;

internal static class TestingConfiguration
{
    public static ReferenceAssemblies ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
}
