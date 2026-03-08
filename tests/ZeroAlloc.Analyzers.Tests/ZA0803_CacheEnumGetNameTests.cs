using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0803_CacheEnumGetNameTests
{
    [Fact]
    public async Task EnumGetNameInLoop_Reports()
    {
        var source = """
            using System;

            class C
            {
                enum Color { Red, Green, Blue }

                void M()
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var name = {|#0:Enum.GetName(typeof(Color), i)|};
                    }
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<CacheEnumGetNameAnalyzer>
            .Diagnostic(DiagnosticIds.CacheEnumGetName)
            .WithLocation(0)
            .WithArguments("Enum.GetName");

        await CSharpAnalyzerVerifier<CacheEnumGetNameAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task EnumGetValuesInLoop_Reports()
    {
        var source = """
            using System;

            class C
            {
                enum Color { Red, Green, Blue }

                void M()
                {
                    while (true)
                    {
                        var values = {|#0:Enum.GetValues(typeof(Color))|};
                        break;
                    }
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<CacheEnumGetNameAnalyzer>
            .Diagnostic(DiagnosticIds.CacheEnumGetName)
            .WithLocation(0)
            .WithArguments("Enum.GetValues");

        await CSharpAnalyzerVerifier<CacheEnumGetNameAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task EnumGetNameOutsideLoop_NoDiagnostic()
    {
        var source = """
            using System;

            class C
            {
                enum Color { Red, Green, Blue }

                void M()
                {
                    var name = Enum.GetName(typeof(Color), 0);
                }
            }
            """;

        await CSharpAnalyzerVerifier<CacheEnumGetNameAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task EnumGetValuesOutsideLoop_NoDiagnostic()
    {
        var source = """
            using System;

            class C
            {
                enum Color { Red, Green, Blue }

                void M()
                {
                    var values = Enum.GetValues(typeof(Color));
                }
            }
            """;

        await CSharpAnalyzerVerifier<CacheEnumGetNameAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }
}
