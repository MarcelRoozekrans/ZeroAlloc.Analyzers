using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0107_PreSizeCollectionsTests
{
    [Fact]
    public async Task ListWithoutCapacityFollowedByAddRange_Reports()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                void M(int[] source)
                {
                    var list = {|#0:new List<int>()|};
                    list.AddRange(source);
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<PreSizeCollectionsAnalyzer>
            .Diagnostic(DiagnosticIds.PreSizeCollections)
            .WithLocation(0)
            .WithArguments("List<int>");

        await CSharpAnalyzerVerifier<PreSizeCollectionsAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task ListWithCapacity_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                void M(int[] source)
                {
                    var list = new List<int>(source.Length);
                    list.AddRange(source);
                }
            }
            """;

        await CSharpAnalyzerVerifier<PreSizeCollectionsAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task ListWithoutAddRange_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var list = new List<int>();
                    list.Add(1);
                }
            }
            """;

        await CSharpAnalyzerVerifier<PreSizeCollectionsAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task ListAssignedFromMethod_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var list = GetList();
                    list.AddRange(new[] { 1, 2, 3 });
                }

                List<int> GetList() => new List<int>();
            }
            """;

        await CSharpAnalyzerVerifier<PreSizeCollectionsAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }
}
