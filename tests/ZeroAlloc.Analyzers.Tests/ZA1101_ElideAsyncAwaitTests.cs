using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA1101_ElideAsyncAwaitTests
{
    [Fact]
    public async Task SingleReturnAwait_Reports()
    {
        var source = """
            using System.Threading.Tasks;

            class C
            {
                {|#0:async|} Task<int> M()
                {
                    return await Task.FromResult(42);
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<ElideAsyncAwaitAnalyzer>
            .Diagnostic(DiagnosticIds.ElideAsyncAwait)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<ElideAsyncAwaitAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task ExpressionBodiedAsyncAwait_Reports()
    {
        var source = """
            using System.Threading.Tasks;

            class C
            {
                {|#0:async|} Task<int> M() => await Task.FromResult(42);
            }
            """;

        var expected = CSharpAnalyzerVerifier<ElideAsyncAwaitAnalyzer>
            .Diagnostic(DiagnosticIds.ElideAsyncAwait)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<ElideAsyncAwaitAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task MultipleStatements_NoDiagnostic()
    {
        var source = """
            using System.Threading.Tasks;

            class C
            {
                async Task<int> M()
                {
                    var x = 1;
                    return await Task.FromResult(x);
                }
            }
            """;

        await CSharpAnalyzerVerifier<ElideAsyncAwaitAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task AwaitInMiddle_NoDiagnostic()
    {
        var source = """
            using System.Threading.Tasks;

            class C
            {
                async Task M()
                {
                    await Task.Delay(1);
                }
            }
            """;

        await CSharpAnalyzerVerifier<ElideAsyncAwaitAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task NonAsyncMethod_NoDiagnostic()
    {
        var source = """
            using System.Threading.Tasks;

            class C
            {
                Task<int> M()
                {
                    return Task.FromResult(42);
                }
            }
            """;

        await CSharpAnalyzerVerifier<ElideAsyncAwaitAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }
}
