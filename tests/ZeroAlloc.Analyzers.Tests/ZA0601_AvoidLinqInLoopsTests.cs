using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0601_AvoidLinqInLoopsTests
{
    [Fact]
    public async Task ToListInForLoop_Reports()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(List<int> items)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var copy = items.{|#0:ToList|}();
                    }
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidLinqInLoopsAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidLinqInLoops)
            .WithLocation(0)
            .WithArguments("ToList");

        await CSharpAnalyzerVerifier<AvoidLinqInLoopsAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task WhereInForeachLoop_Reports()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(List<int> items)
                {
                    foreach (var item in items)
                    {
                        var filtered = items.{|#0:Where|}(x => x > 0);
                    }
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidLinqInLoopsAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidLinqInLoops)
            .WithLocation(0)
            .WithArguments("Where");

        await CSharpAnalyzerVerifier<AvoidLinqInLoopsAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task LinqOutsideLoop_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(List<int> items)
                {
                    var copy = items.ToList();
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidLinqInLoopsAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task NonLinqMethodInLoop_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                void M(List<int> items)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        items.Add(i);
                    }
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidLinqInLoopsAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }
}
