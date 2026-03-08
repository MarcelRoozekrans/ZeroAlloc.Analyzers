using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA1102_DisposeCancellationTokenSourceTests
{
    [Fact]
    public async Task CtsWithoutUsing_Reports()
    {
        var source = """
            using System.Threading;

            class C
            {
                void M()
                {
                    var cts = {|#0:new CancellationTokenSource()|};
                    cts.Token.ThrowIfCancellationRequested();
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<DisposeCancellationTokenSourceAnalyzer>
            .Diagnostic(DiagnosticIds.DisposeCancellationTokenSource)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<DisposeCancellationTokenSourceAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task CtsWithUsingStatement_NoDiagnostic()
    {
        var source = """
            using System.Threading;

            class C
            {
                void M()
                {
                    using (var cts = new CancellationTokenSource())
                    {
                        cts.Token.ThrowIfCancellationRequested();
                    }
                }
            }
            """;

        await CSharpAnalyzerVerifier<DisposeCancellationTokenSourceAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task CtsWithUsingDeclaration_NoDiagnostic()
    {
        var source = """
            using System.Threading;

            class C
            {
                void M()
                {
                    using var cts = new CancellationTokenSource();
                    cts.Token.ThrowIfCancellationRequested();
                }
            }
            """;

        await CSharpAnalyzerVerifier<DisposeCancellationTokenSourceAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task CtsAsField_NoDiagnostic()
    {
        var source = """
            using System.Threading;

            class C
            {
                private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            }
            """;

        await CSharpAnalyzerVerifier<DisposeCancellationTokenSourceAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }
}
