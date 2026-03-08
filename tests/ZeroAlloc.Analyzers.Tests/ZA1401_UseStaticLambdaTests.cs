using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA1401_UseStaticLambdaTests
{
    [Fact]
    public async Task LambdaNoCaptureNoStatic_Reports()
    {
        var source = """
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var r = items.Where({|#0:x => x > 0|});
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseStaticLambdaAnalyzer>
            .Diagnostic(DiagnosticIds.UseStaticLambda)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<UseStaticLambdaAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task StaticLambda_NoDiagnostic()
    {
        var source = """
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var r = items.Where(static x => x > 0);
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseStaticLambdaAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task LambdaCapturingVariable_NoDiagnostic()
    {
        var source = """
            using System.Linq;

            class C
            {
                void M()
                {
                    int threshold = 5;
                    var items = new[] { 1, 2, 3 };
                    var r = items.Where(x => x > threshold);
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseStaticLambdaAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task PreNet5_NoDiagnostic()
    {
        var source = """
            using System.Linq;

            class C
            {
                void M()
                {
                    var items = new[] { 1, 2, 3 };
                    var r = items.Where(x => x > 0);
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseStaticLambdaAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net48");
    }
}
