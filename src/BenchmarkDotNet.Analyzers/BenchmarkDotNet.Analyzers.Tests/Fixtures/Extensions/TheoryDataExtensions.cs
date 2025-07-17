namespace BenchmarkDotNet.Analyzers.Tests.Fixtures;

using Xunit;

using System.Collections.Generic;
using System.Linq;

public static class TheoryDataExtensions
{
    public static IEnumerable<T> ToEnumerable<T>(this TheoryData<T> theoryData) => theoryData.Select(tdr => (T)tdr);
}