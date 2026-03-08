using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0802_AvoidEnumToStringTests
{
    [Fact]
    public async Task EnumToString_Reports()
    {
        var source = """
            using System;

            enum Color { Red, Green, Blue }

            class C
            {
                string M(Color c) => c.{|#0:ToString|}();
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidEnumToStringAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidEnumToString)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<AvoidEnumToStringAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task NonEnumToString_NoDiagnostic()
    {
        var source = """
            class C
            {
                string M(int x) => x.ToString();
            }
            """;

        await CSharpAnalyzerVerifier<AvoidEnumToStringAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task EnumToStringWithFormat_NoDiagnostic()
    {
        var source = """
            using System;

            enum Color { Red, Green, Blue }

            class C
            {
                string M(Color c) => c.ToString("G");
            }
            """;

        await CSharpAnalyzerVerifier<AvoidEnumToStringAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }
}
