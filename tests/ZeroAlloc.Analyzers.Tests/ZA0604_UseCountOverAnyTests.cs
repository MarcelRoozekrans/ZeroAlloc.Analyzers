using Microsoft.CodeAnalysis.Testing;
using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0604_UseCountOverAnyTests
{
    [Fact]
    public async Task ListAny_Reports()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(List<int> myList)
                {
                    var b = myList.{|#0:Any|}();
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseCountOverAnyAnalyzer>
            .Diagnostic(DiagnosticIds.UseCountOverAny)
            .WithLocation(0)
            .WithArguments("Count");

        await CSharpAnalyzerVerifier<UseCountOverAnyAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task AnyWithPredicate_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(List<int> myList)
                {
                    var b = myList.Any(x => x > 0);
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseCountOverAnyAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task IEnumerableAny_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(IEnumerable<int> items)
                {
                    var b = items.Any();
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseCountOverAnyAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }
}
