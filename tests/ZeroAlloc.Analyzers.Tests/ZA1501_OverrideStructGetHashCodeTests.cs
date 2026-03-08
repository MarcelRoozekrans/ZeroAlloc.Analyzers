using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA1501_OverrideStructGetHashCodeTests
{
    [Fact]
    public async Task StructKeyWithoutGetHashCode_Reports()
    {
        var source = """
            struct MyStruct { public int X; }

            class C
            {
                void M()
                {
                    var d = {|#0:new System.Collections.Generic.Dictionary<MyStruct, int>()|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<OverrideStructGetHashCodeAnalyzer>
            .Diagnostic(DiagnosticIds.OverrideStructGetHashCode)
            .WithLocation(0)
            .WithArguments("MyStruct");

        await CSharpAnalyzerVerifier<OverrideStructGetHashCodeAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task StructKeyWithGetHashCode_NoDiagnostic()
    {
        var source = """
            struct MyStruct
            {
                public int X;
                public override int GetHashCode() => X;
                public override bool Equals(object obj) => obj is MyStruct s && s.X == X;
            }

            class C
            {
                void M()
                {
                    var d = new System.Collections.Generic.Dictionary<MyStruct, int>();
                }
            }
            """;

        await CSharpAnalyzerVerifier<OverrideStructGetHashCodeAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task ClassKey_NoDiagnostic()
    {
        var source = """
            class C
            {
                void M()
                {
                    var d = new System.Collections.Generic.Dictionary<string, int>();
                }
            }
            """;

        await CSharpAnalyzerVerifier<OverrideStructGetHashCodeAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task EnumKey_NoDiagnostic()
    {
        var source = """
            enum MyEnum { A, B, C }

            class C
            {
                void M()
                {
                    var d = new System.Collections.Generic.Dictionary<MyEnum, int>();
                }
            }
            """;

        await CSharpAnalyzerVerifier<OverrideStructGetHashCodeAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }
}
