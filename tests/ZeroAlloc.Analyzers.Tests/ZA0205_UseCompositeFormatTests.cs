using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0205_UseCompositeFormatTests
{
    [Fact]
    public async Task StringFormatWithLiteral_Reports()
    {
        var source = """
            class C
            {
                void M()
                {
                    var s = {|#0:string.Format("Hello {0}, you are {1}", "world", 42)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseCompositeFormatAnalyzer>
            .Diagnostic(DiagnosticIds.UseCompositeFormat)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<UseCompositeFormatAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task StringFormatWithStaticReadonlyField_Reports()
    {
        var source = """
            class C
            {
                private static readonly string Format = "Hello {0}";
                void M()
                {
                    var s = {|#0:string.Format(Format, "world")|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseCompositeFormatAnalyzer>
            .Diagnostic(DiagnosticIds.UseCompositeFormat)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<UseCompositeFormatAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task PreNet8_NoDiagnostic()
    {
        var source = """
            class C
            {
                void M()
                {
                    var s = string.Format("Hello {0}", "world");
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseCompositeFormatAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net6.0");
    }

    [Fact]
    public async Task StringFormatWithVariable_NoDiagnostic()
    {
        var source = """
            class C
            {
                void M(string fmt)
                {
                    var s = string.Format(fmt, "world");
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseCompositeFormatAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task NoFormatCall_NoDiagnostic()
    {
        var source = """
            class C
            {
                void M()
                {
                    var s = "Hello".ToUpper();
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseCompositeFormatAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }
}
