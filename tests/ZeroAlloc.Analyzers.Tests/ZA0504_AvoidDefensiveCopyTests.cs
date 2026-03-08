using Microsoft.CodeAnalysis.Testing;
using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0504_AvoidDefensiveCopyTests
{
    [Fact]
    public async Task InParamCallsNonReadonlyMethod_Reports()
    {
        var source = """
            struct MyStruct
            {
                public int Value;
                public int GetValue() => Value;
            }

            class C
            {
                void M(in MyStruct s)
                {
                    var v = {|#0:s.GetValue()|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidDefensiveCopyAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidDefensiveCopy)
            .WithLocation(0)
            .WithArguments("GetValue", "s");

        await CSharpAnalyzerVerifier<AvoidDefensiveCopyAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task InParamCallsReadonlyMethod_NoDiagnostic()
    {
        var source = """
            struct MyStruct
            {
                public int Value;
                public readonly int GetValue() => Value;
            }

            class C
            {
                void M(in MyStruct s)
                {
                    var v = s.GetValue();
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidDefensiveCopyAnalyzer>
            .VerifyNoDiagnosticAsync(source);
    }

    [Fact]
    public async Task ReadonlyFieldCallsNonReadonlyMethod_Reports()
    {
        var source = """
            struct MyStruct
            {
                public int Value;
                public int GetValue() => Value;
            }

            class C
            {
                readonly MyStruct _field;

                void M()
                {
                    var v = {|#0:_field.GetValue()|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidDefensiveCopyAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidDefensiveCopy)
            .WithLocation(0)
            .WithArguments("GetValue", "_field");

        await CSharpAnalyzerVerifier<AvoidDefensiveCopyAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task RegularParamCallsNonReadonlyMethod_NoDiagnostic()
    {
        var source = """
            struct MyStruct
            {
                public int Value;
                public int GetValue() => Value;
            }

            class C
            {
                void M(MyStruct s)
                {
                    var v = s.GetValue();
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidDefensiveCopyAnalyzer>
            .VerifyNoDiagnosticAsync(source);
    }

    [Fact]
    public async Task ReferenceTypeInParam_NoDiagnostic()
    {
        var source = """
            class MyClass
            {
                public int Value;
                public int GetValue() => Value;
            }

            class C
            {
                void M(in MyClass s)
                {
                    var v = s.GetValue();
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidDefensiveCopyAnalyzer>
            .VerifyNoDiagnosticAsync(source);
    }
}
