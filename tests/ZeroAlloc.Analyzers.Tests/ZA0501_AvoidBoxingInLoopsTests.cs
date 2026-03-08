using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0501_AvoidBoxingInLoopsTests
{
    [Fact]
    public async Task IntPassedToObjectParam_InLoop_Reports()
    {
        var source = """
            class C
            {
                static void Log(object value) { }

                void M()
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Log({|#0:i|});
                    }
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidBoxingInLoopsAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidBoxingInLoops)
            .WithLocation(0)
            .WithArguments("int", "object");

        await CSharpAnalyzerVerifier<AvoidBoxingInLoopsAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task StructPassedToInterfaceParam_InLoop_Reports()
    {
        var source = """
            using System;

            class C
            {
                static void Format(IFormattable value) { }

                void M()
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Format({|#0:i|});
                    }
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidBoxingInLoopsAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidBoxingInLoops)
            .WithLocation(0)
            .WithArguments("int", "IFormattable");

        await CSharpAnalyzerVerifier<AvoidBoxingInLoopsAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task IntPassedToObjectParam_OutsideLoop_NoDiagnostic()
    {
        var source = """
            class C
            {
                static void Log(object value) { }

                void M()
                {
                    Log(42);
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidBoxingInLoopsAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task StringPassedToObjectParam_InLoop_NoDiagnostic()
    {
        var source = """
            class C
            {
                static void Log(object value) { }

                void M()
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Log("hello");
                    }
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidBoxingInLoopsAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task IntPassedToGenericParam_InLoop_NoDiagnostic()
    {
        var source = """
            class C
            {
                static void Log<T>(T value) { }

                void M()
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Log(i);
                    }
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidBoxingInLoopsAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task EnumPassedToObjectParam_InLoop_Reports()
    {
        var source = """
            using System;

            enum Color { Red, Green, Blue }

            class C
            {
                static void Log(object value) { }

                void M()
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Log({|#0:(Color)i|});
                    }
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidBoxingInLoopsAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidBoxingInLoops)
            .WithLocation(0)
            .WithArguments("Color", "object");

        await CSharpAnalyzerVerifier<AvoidBoxingInLoopsAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }
}
