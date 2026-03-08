using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0203_UseSpanInsteadOfSubstringTests
{
    [Fact]
    public async Task SubstringCall_Reports()
    {
        var source = """
            class C
            {
                void M(string s)
                {
                    var sub = s.{|#0:Substring|}(1);
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseSpanInsteadOfSubstringAnalyzer>
            .Diagnostic(DiagnosticIds.UseSpanInsteadOfSubstring)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<UseSpanInsteadOfSubstringAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task SubstringWithLength_Reports()
    {
        var source = """
            class C
            {
                void M(string s)
                {
                    var sub = s.{|#0:Substring|}(1, 3);
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseSpanInsteadOfSubstringAnalyzer>
            .Diagnostic(DiagnosticIds.UseSpanInsteadOfSubstring)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<UseSpanInsteadOfSubstringAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task OnOldTfm_NoDiagnostic()
    {
        var source = """
            class C
            {
                void M(string s)
                {
                    var sub = s.Substring(1);
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseSpanInsteadOfSubstringAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net48");
    }

    [Fact]
    public async Task NonStringSubstring_NoDiagnostic()
    {
        var source = """
            class MyString
            {
                public string Substring(int start) => "";
            }

            class C
            {
                void M()
                {
                    var s = new MyString();
                    var sub = s.Substring(1);
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseSpanInsteadOfSubstringAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }
}
