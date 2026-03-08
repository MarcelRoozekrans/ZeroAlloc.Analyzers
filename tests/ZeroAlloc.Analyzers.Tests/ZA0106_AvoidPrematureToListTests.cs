using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0106_AvoidPrematureToListTests
{
    [Fact]
    public async Task ToListThenWhere_Reports()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(IEnumerable<int> items)
                {
                    var result = items.{|#0:ToList|}().Where(x => x > 0);
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidPrematureToListAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidPrematureToList)
            .WithLocation(0)
            .WithArguments("ToList", "Where");

        await CSharpAnalyzerVerifier<AvoidPrematureToListAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task ToArrayThenSelect_Reports()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(IEnumerable<int> items)
                {
                    var result = items.{|#0:ToArray|}().Select(x => x * 2);
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidPrematureToListAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidPrematureToList)
            .WithLocation(0)
            .WithArguments("ToArray", "Select");

        await CSharpAnalyzerVerifier<AvoidPrematureToListAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task ToListThenFirst_Reports()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(IEnumerable<int> items)
                {
                    var result = items.{|#0:ToList|}().First();
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidPrematureToListAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidPrematureToList)
            .WithLocation(0)
            .WithArguments("ToList", "First");

        await CSharpAnalyzerVerifier<AvoidPrematureToListAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task WhereThenToList_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(IEnumerable<int> items)
                {
                    var result = items.Where(x => x > 0).ToList();
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidPrematureToListAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task ToListAlone_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(IEnumerable<int> items)
                {
                    var result = items.ToList();
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidPrematureToListAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }
}
