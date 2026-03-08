using Microsoft.CodeAnalysis.Testing;
using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0605_UseIndexerOverLinqFirstTests
{
    [Fact]
    public async Task ListFirst_Reports()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(List<int> myList)
                {
                    var f = myList.{|#0:First|}();
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseIndexerOverLinqFirstAnalyzer>
            .Diagnostic(DiagnosticIds.UseIndexerOverLinqFirst)
            .WithLocation(0)
            .WithArguments("First");

        await CSharpAnalyzerVerifier<UseIndexerOverLinqFirstAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task ArrayLast_Reports()
    {
        var source = """
            using System.Linq;

            class C
            {
                void M(int[] myArray)
                {
                    var l = myArray.{|#0:Last|}();
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseIndexerOverLinqFirstAnalyzer>
            .Diagnostic(DiagnosticIds.UseIndexerOverLinqFirst)
            .WithLocation(0)
            .WithArguments("Last");

        await CSharpAnalyzerVerifier<UseIndexerOverLinqFirstAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task FirstWithPredicate_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(List<int> myList)
                {
                    var f = myList.First(x => x > 0);
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseIndexerOverLinqFirstAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task IEnumerableFirst_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(IEnumerable<int> items)
                {
                    var f = items.First();
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseIndexerOverLinqFirstAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }
}
