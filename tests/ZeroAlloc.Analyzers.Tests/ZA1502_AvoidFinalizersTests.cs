using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA1502_AvoidFinalizersTests
{
    [Fact]
    public async Task ClassWithFinalizer_Reports()
    {
        var source = """
            class MyResource
            {
                {|#0:~MyResource()|} { }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidFinalizersAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidFinalizers)
            .WithLocation(0)
            .WithArguments("MyResource");

        await CSharpAnalyzerVerifier<AvoidFinalizersAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task ClassWithoutFinalizer_NoDiagnostic()
    {
        var source = """
            class MyResource : System.IDisposable
            {
                public void Dispose() { }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidFinalizersAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task ClassWithDispose_StillReports()
    {
        var source = """
            class MyResource : System.IDisposable
            {
                public void Dispose() { }
                {|#0:~MyResource()|} { }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidFinalizersAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidFinalizers)
            .WithLocation(0)
            .WithArguments("MyResource");

        await CSharpAnalyzerVerifier<AvoidFinalizersAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }
}
