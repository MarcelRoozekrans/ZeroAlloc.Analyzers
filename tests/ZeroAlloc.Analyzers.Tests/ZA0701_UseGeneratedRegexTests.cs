using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0701_UseGeneratedRegexTests
{
    [Fact]
    public async Task NewRegexWithConstantPattern_Reports()
    {
        var source = """
            using System.Text.RegularExpressions;

            class C
            {
                void M()
                {
                    var r = {|#0:new Regex("abc")|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseGeneratedRegexAnalyzer>
            .Diagnostic(DiagnosticIds.UseGeneratedRegex)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<UseGeneratedRegexAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task NewRegexWithOptions_Reports()
    {
        var source = """
            using System.Text.RegularExpressions;

            class C
            {
                void M()
                {
                    var r = {|#0:new Regex("abc", RegexOptions.IgnoreCase)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseGeneratedRegexAnalyzer>
            .Diagnostic(DiagnosticIds.UseGeneratedRegex)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<UseGeneratedRegexAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task NewRegexWithDynamicPattern_NoDiagnostic()
    {
        var source = """
            using System.Text.RegularExpressions;

            class C
            {
                void M(string pattern)
                {
                    var r = new Regex(pattern);
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseGeneratedRegexAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task OnNet6_NoDiagnostic()
    {
        var source = """
            using System.Text.RegularExpressions;

            class C
            {
                void M()
                {
                    var r = new Regex("abc");
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseGeneratedRegexAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net6.0");
    }
}
