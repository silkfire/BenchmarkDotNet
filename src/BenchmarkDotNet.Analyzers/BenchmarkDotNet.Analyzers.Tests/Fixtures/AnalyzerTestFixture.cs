namespace BenchmarkDotNet.Analyzers.Tests.Fixtures;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public abstract class AnalyzerTestFixture<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    private readonly CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> _analyzerTest;

    private readonly DiagnosticDescriptor? _ruleUnderTest;

    private AnalyzerTestFixture(bool assertUniqueSupportedDiagnostics)
    {
        _analyzerTest = new InternalAnalyzerTest
                        {
                            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                            //ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20,
                            TestState =
                            {
                                AdditionalReferences =
                                {
                                    "BenchmarkDotNet.dll",
                                    "BenchmarkDotNet.Annotations.dll"
                                }
                            }
                        };


        if (assertUniqueSupportedDiagnostics)
        {
            AssertUniqueSupportedDiagnostics();
        }
    }

    protected AnalyzerTestFixture() : this(true) { }

    protected AnalyzerTestFixture(DiagnosticDescriptor diagnosticDescriptor) : this(false)
    {
        var analyzer = AssertUniqueSupportedDiagnostics();

        if (diagnosticDescriptor == null!)
        {
            Assert.Fail("Diagnostic under test cannot be null when using this constructor");
        }

        AssertDiagnosticUnderTestIsSupportedByAnalyzer();
        DisableAllSupportedDiagnosticsExceptDiagnosticUnderTest();

        _ruleUnderTest = diagnosticDescriptor;

        return;

        void AssertDiagnosticUnderTestIsSupportedByAnalyzer()
        {
            if (!analyzer.SupportedDiagnostics.Any(dd => dd.Id == diagnosticDescriptor.Id))
            {
                Assert.Fail($"Diagnostic descriptor with ID {diagnosticDescriptor.Id} is not supported by the analyzer {typeof(TAnalyzer).Name}");
            }
        }

        void DisableAllSupportedDiagnosticsExceptDiagnosticUnderTest()
        {
            _analyzerTest.DisabledDiagnostics.Clear();
            _analyzerTest.DisabledDiagnostics.AddRange(analyzer.SupportedDiagnostics.Select(dd => dd.Id).Except([ diagnosticDescriptor.Id ]));
        }
    }

    private static TAnalyzer AssertUniqueSupportedDiagnostics()
    {
        var allSupportedDiagnostics = new Dictionary<string, int>();

        var analyzer = new TAnalyzer();
        foreach (var supportedDiagnostic in analyzer.SupportedDiagnostics)
        {
            if (!allSupportedDiagnostics.TryAdd(supportedDiagnostic.Id, 1))
            {
                allSupportedDiagnostics[supportedDiagnostic.Id]++;
            }
        }

        var duplicateSupportedDiagnostics = allSupportedDiagnostics.Where(kvp => kvp.Value > 1)
                                                                   .OrderBy(kvp => kvp.Key)
                                                                   .ToList();

        if (duplicateSupportedDiagnostics.Count > 0)
        {
            Assert.Fail($"The analyzer {typeof(TAnalyzer).FullName} contains duplicate supported diagnostics:{Environment.NewLine}{Environment.NewLine}{string.Join(", ", duplicateSupportedDiagnostics.Select(kvp => $"❌ {kvp.Key} (x{kvp.Value})"))}{Environment.NewLine}");
        }

        return analyzer;
    }

    protected string TestCode
    {
        set => _analyzerTest.TestCode = value;
    }

    protected SourceFileList Sources => _analyzerTest.TestState.Sources;

    protected void AddDefaultExpectedDiagnostic()
    {
        AddExpectedDiagnostic();
    }

    protected void AddDefaultExpectedDiagnostic(params object[] arguments)
    {
        AddExpectedDiagnostic(arguments);
    }

    protected void AddExpectedDiagnostic(int markupKey)
    {
        AddExpectedDiagnostic(null, markupKey);
    }

    protected void AddExpectedDiagnostic(int markupKey, params object[] arguments)
    {
        AddExpectedDiagnostic(arguments, markupKey);
    }

    private void AddExpectedDiagnostic(object[]? arguments = null, int markupKey = 0)
    {
        if (_ruleUnderTest == null)
        {
            throw new InvalidOperationException("Failed to add expected diagnostic: no diagnostic rule specified for this fixture");
        }

        var diagnosticResult = new DiagnosticResult(_ruleUnderTest).WithLocation(markupKey)
                                                                   .WithMessageFormat(_ruleUnderTest.MessageFormat);

        if (arguments != null)
        {
            diagnosticResult = diagnosticResult.WithArguments(arguments);
        }

        _analyzerTest.ExpectedDiagnostics.Add(diagnosticResult);
    }

    protected void DisableCompilerDiagnostics()
    {
        _analyzerTest.CompilerDiagnostics = CompilerDiagnostics.None;
    }

    protected Task RunAsync(CancellationToken cancellationToken)
    {
        return _analyzerTest.RunAsync(cancellationToken);
    }

    protected void ReferenceDummyAttribute()
    {
        _analyzerTest.TestState.Sources.Add("""
                                            using System;

                                            public class DummyAttribute : Attribute
                                            {
                                                
                                            }
                                            """);
    }

    protected void ReferenceDummyEnum()
    {
        _analyzerTest.TestState.Sources.Add("""
                                            public enum DummyEnum
                                            {
                                                Value1,
                                                Value2,
                                                Value3
                                            }
                                            """);
    }

    protected void ReferenceDummyEnumWithFlagsAttribute()
    {
        _analyzerTest.TestState.Sources.Add("""
                                            using System;

                                            [Flags]
                                            public enum DummyEnumWithFlagsAttribute
                                            {
                                                Value1,
                                                Value2,
                                                Value3
                                            }
                                            """);
    }

    private sealed class InternalAnalyzerTest : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
    {
        protected override string DefaultTestProjectName => "BenchmarksAssemblyUnderAnalysis";
    }
}
