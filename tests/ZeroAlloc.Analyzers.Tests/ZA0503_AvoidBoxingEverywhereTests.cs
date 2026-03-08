using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0503_AvoidBoxingEverywhereTests
{
    /// <summary>
    /// Helper that enables ZA0503 (disabled by default) via .globalconfig.
    /// </summary>
    private static async Task VerifyWithRuleEnabled(
        string source,
        params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<AvoidBoxingEverywhereAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };

        test.TestState.AnalyzerConfigFiles.Add(
            ("/.globalconfig", """
                is_global = true
                build_property.TargetFramework = net8.0
                dotnet_diagnostic.ZA0503.severity = info
                """));

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }

    [Fact]
    public async Task IntPassedToObjectParam_Reports()
    {
        var source = """
            class C
            {
                static void Log(object value) { }

                void M()
                {
                    Log({|#0:42|});
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidBoxingEverywhereAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidBoxingEverywhere)
            .WithLocation(0)
            .WithArguments("int", "object");

        await VerifyWithRuleEnabled(source, expected);
    }

    [Fact]
    public async Task StructPassedToInterfaceParam_Reports()
    {
        var source = """
            using System;

            class C
            {
                static void Format(IFormattable value) { }

                void M()
                {
                    Format({|#0:42|});
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidBoxingEverywhereAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidBoxingEverywhere)
            .WithLocation(0)
            .WithArguments("int", "IFormattable");

        await VerifyWithRuleEnabled(source, expected);
    }

    [Fact]
    public async Task StringPassedToObjectParam_NoDiagnostic()
    {
        var source = """
            class C
            {
                static void Log(object value) { }

                void M()
                {
                    Log("hello");
                }
            }
            """;

        await VerifyWithRuleEnabled(source);
    }

    [Fact]
    public async Task GenericParam_NoDiagnostic()
    {
        var source = """
            class C
            {
                static void Log<T>(T value) { }

                void M()
                {
                    Log(42);
                }
            }
            """;

        await VerifyWithRuleEnabled(source);
    }

    [Fact]
    public async Task ExplicitlyDisabled_NoDiagnostic()
    {
        // When explicitly disabled via editorconfig, should produce no diagnostics
        var source = """
            class C
            {
                static void Log(object value) { }

                void M()
                {
                    Log(42);
                }
            }
            """;

        var test = new CSharpAnalyzerTest<AvoidBoxingEverywhereAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };

        test.TestState.AnalyzerConfigFiles.Add(
            ("/.globalconfig", """
                is_global = true
                build_property.TargetFramework = net8.0
                dotnet_diagnostic.ZA0503.severity = none
                """));

        await test.RunAsync();
    }
}
