using ZeroAlloc.Analyzers.CodeFixes;
using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0108_AvoidRedundantMaterializationTests
{
    [Fact]
    public async Task ToList_OnList_Reports()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    List<int> items = new List<int> { 1, 2, 3 };
                    var copy = items.{|#0:ToList|}();
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidRedundantMaterializationAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidRedundantMaterialization)
            .WithLocation(0)
            .WithArguments("items", "List<int>", "ToList");

        await CSharpAnalyzerVerifier<AvoidRedundantMaterializationAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task ToArray_OnArray_Reports()
    {
        var source = """
            using System.Linq;

            class C
            {
                void M()
                {
                    int[] items = new int[] { 1, 2, 3 };
                    var copy = items.{|#0:ToArray|}();
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidRedundantMaterializationAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidRedundantMaterialization)
            .WithLocation(0)
            .WithArguments("items", "int[]", "ToArray");

        await CSharpAnalyzerVerifier<AvoidRedundantMaterializationAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task ToList_OnIEnumerable_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(IEnumerable<int> items)
                {
                    var list = items.ToList();
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidRedundantMaterializationAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task ToArray_OnIEnumerable_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(IEnumerable<int> items)
                {
                    var arr = items.ToArray();
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidRedundantMaterializationAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task ToArray_OnList_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    List<int> items = new List<int> { 1, 2, 3 };
                    var arr = items.ToArray();
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidRedundantMaterializationAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task ToList_OnArray_NoDiagnostic()
    {
        var source = """
            using System.Linq;

            class C
            {
                void M()
                {
                    int[] items = new int[] { 1, 2, 3 };
                    var list = items.ToList();
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidRedundantMaterializationAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    // Code fix tests

    [Fact]
    public async Task ToList_OnList_CodeFix_RemovesCall()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    List<int> items = new List<int> { 1, 2, 3 };
                    var copy = items.{|#0:ToList|}();
                }
            }
            """;

        var fixedSource = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    List<int> items = new List<int> { 1, 2, 3 };
                    var copy = items;
                }
            }
            """;

        var expected = CSharpCodeFixVerifier<AvoidRedundantMaterializationAnalyzer, AvoidRedundantMaterializationCodeFixProvider>
            .Diagnostic(DiagnosticIds.AvoidRedundantMaterialization)
            .WithLocation(0)
            .WithArguments("items", "List<int>", "ToList");

        await CSharpCodeFixVerifier<AvoidRedundantMaterializationAnalyzer, AvoidRedundantMaterializationCodeFixProvider>
            .VerifyCodeFixAsync(source, fixedSource, expected);
    }

    [Fact]
    public async Task ToArray_OnArray_CodeFix_RemovesCall()
    {
        var source = """
            using System.Linq;

            class C
            {
                void M()
                {
                    int[] items = new int[] { 1, 2, 3 };
                    var copy = items.{|#0:ToArray|}();
                }
            }
            """;

        var fixedSource = """
            using System.Linq;

            class C
            {
                void M()
                {
                    int[] items = new int[] { 1, 2, 3 };
                    var copy = items;
                }
            }
            """;

        var expected = CSharpCodeFixVerifier<AvoidRedundantMaterializationAnalyzer, AvoidRedundantMaterializationCodeFixProvider>
            .Diagnostic(DiagnosticIds.AvoidRedundantMaterialization)
            .WithLocation(0)
            .WithArguments("items", "int[]", "ToArray");

        await CSharpCodeFixVerifier<AvoidRedundantMaterializationAnalyzer, AvoidRedundantMaterializationCodeFixProvider>
            .VerifyCodeFixAsync(source, fixedSource, expected);
    }
}
