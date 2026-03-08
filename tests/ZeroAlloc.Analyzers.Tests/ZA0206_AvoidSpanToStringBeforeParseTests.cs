using Microsoft.CodeAnalysis.Testing;
using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0206_AvoidSpanToStringBeforeParseTests
{
    [Fact]
    public async Task IntParseSpanToString_Reports()
    {
        var source = """
            using System;
            class C
            {
                void M(ReadOnlySpan<char> span)
                {
                    var n = {|#0:int.Parse(span.ToString())|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidSpanToStringBeforeParseAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidSpanToStringBeforeParse)
            .WithLocation(0)
            .WithArguments("Int32");

        await CSharpAnalyzerVerifier<AvoidSpanToStringBeforeParseAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task PreNet6_NoDiagnostic()
    {
        var source = """
            class C
            {
                void M(string s)
                {
                    var n = int.Parse(s.ToString());
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidSpanToStringBeforeParseAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net48", ReferenceAssemblies.NetFramework.Net48.Default);
    }

    [Fact]
    public async Task IntParseString_NoDiagnostic()
    {
        var source = """
            class C
            {
                void M()
                {
                    var n = int.Parse("42");
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidSpanToStringBeforeParseAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task IntParseSpanDirect_NoDiagnostic()
    {
        var source = """
            using System;
            class C
            {
                void M(ReadOnlySpan<char> span)
                {
                    var n = int.Parse(span);
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidSpanToStringBeforeParseAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }
}
