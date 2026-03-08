using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0901_ConsiderSealingClassTests
{
    [Fact]
    public async Task NonSealedClassWithoutDerivedTypes_Reports()
    {
        var source = """
            class {|#0:MyService|}
            {
                public void DoWork() { }
            }
            """;

        var expected = CSharpAnalyzerVerifier<ConsiderSealingClassAnalyzer>
            .Diagnostic(DiagnosticIds.ConsiderSealingClass)
            .WithLocation(0)
            .WithArguments("MyService");

        await CSharpAnalyzerVerifier<ConsiderSealingClassAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task SealedClass_NoDiagnostic()
    {
        var source = """
            sealed class MyService
            {
                public void DoWork() { }
            }
            """;

        await CSharpAnalyzerVerifier<ConsiderSealingClassAnalyzer>
            .VerifyNoDiagnosticAsync(source);
    }

    [Fact]
    public async Task AbstractClass_NoDiagnostic()
    {
        var source = """
            abstract class MyService
            {
                public abstract void DoWork();
            }
            """;

        await CSharpAnalyzerVerifier<ConsiderSealingClassAnalyzer>
            .VerifyNoDiagnosticAsync(source);
    }

    [Fact]
    public async Task ClassWithDerivedType_NoDiagnostic()
    {
        var source = """
            class Base { }
            class {|#0:Derived|} : Base { }
            """;

        // Base should NOT be flagged (it has a derived type).
        // Derived SHOULD be flagged (leaf class, not sealed).
        var expected = CSharpAnalyzerVerifier<ConsiderSealingClassAnalyzer>
            .Diagnostic(DiagnosticIds.ConsiderSealingClass)
            .WithLocation(0)
            .WithArguments("Derived");

        await CSharpAnalyzerVerifier<ConsiderSealingClassAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task StaticClass_NoDiagnostic()
    {
        var source = """
            static class Helper
            {
                public static void DoWork() { }
            }
            """;

        await CSharpAnalyzerVerifier<ConsiderSealingClassAnalyzer>
            .VerifyNoDiagnosticAsync(source);
    }
}
