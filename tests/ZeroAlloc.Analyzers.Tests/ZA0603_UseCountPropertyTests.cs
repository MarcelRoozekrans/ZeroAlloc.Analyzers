using Microsoft.CodeAnalysis.Testing;
using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0603_UseCountPropertyTests
{
    [Fact]
    public async Task ListCount_Reports()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(List<int> myList)
                {
                    var c = myList.{|#0:Count|}();
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseCountPropertyAnalyzer>
            .Diagnostic(DiagnosticIds.UseCountProperty)
            .WithLocation(0)
            .WithArguments("Count");

        await CSharpAnalyzerVerifier<UseCountPropertyAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task ArrayCount_Reports()
    {
        var source = """
            using System.Linq;

            class C
            {
                void M(int[] myArray)
                {
                    var c = myArray.{|#0:Count|}();
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseCountPropertyAnalyzer>
            .Diagnostic(DiagnosticIds.UseCountProperty)
            .WithLocation(0)
            .WithArguments("Length");

        await CSharpAnalyzerVerifier<UseCountPropertyAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task CountWithPredicate_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(List<int> myList)
                {
                    var c = myList.Count(x => x > 0);
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseCountPropertyAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task IEnumerableCount_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(IEnumerable<int> items)
                {
                    var c = items.Count();
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseCountPropertyAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }
}
