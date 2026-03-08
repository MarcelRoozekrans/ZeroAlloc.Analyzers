using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0602_AvoidParamsInLoopsTests
{
    [Fact]
    public async Task ParamsCallInLoop_Reports()
    {
        var source = """
            class C
            {
                static void Log(string format, params object[] args) { }

                void M()
                {
                    for (int i = 0; i < 10; i++)
                    {
                        {|#0:Log("value: {0}", i)|};
                    }
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidParamsInLoopsAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidParamsInLoops)
            .WithLocation(0)
            .WithArguments("Log");

        await CSharpAnalyzerVerifier<AvoidParamsInLoopsAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task ParamsCallOutsideLoop_NoDiagnostic()
    {
        var source = """
            class C
            {
                static void Log(string format, params object[] args) { }

                void M()
                {
                    Log("value: {0}", 42);
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidParamsInLoopsAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task NonParamsCallInLoop_NoDiagnostic()
    {
        var source = """
            class C
            {
                static void Log(string msg) { }

                void M()
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Log("hello");
                    }
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidParamsInLoopsAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }
}
