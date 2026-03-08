using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0204_UseStringCreateTests
{
    [Fact]
    public async Task StringFormat_Reports()
    {
        var source = """
            class C
            {
                void M(string name)
                {
                    var s = {|#0:string.Format("Hello {0}", name)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseStringCreateAnalyzer>
            .Diagnostic(DiagnosticIds.UseStringCreate)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<UseStringCreateAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task StringFormat_PreNet6_NoDiagnostic()
    {
        var source = """
            class C
            {
                void M(string name)
                {
                    var s = string.Format("Hello {0}", name);
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseStringCreateAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net48");
    }

    [Fact]
    public async Task StringConcat_NoDiagnostic()
    {
        var source = """
            class C
            {
                void M()
                {
                    var s = "a" + "b";
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseStringCreateAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task InterpolatedString_NoDiagnostic()
    {
        var source = """
            class C
            {
                void M(string name)
                {
                    var s = $"Hello {name}";
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseStringCreateAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }
}
