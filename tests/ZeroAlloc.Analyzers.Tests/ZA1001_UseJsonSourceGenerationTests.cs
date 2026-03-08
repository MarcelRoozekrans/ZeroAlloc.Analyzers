using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA1001_UseJsonSourceGenerationTests
{
    [Fact]
    public async Task JsonSerializeWithoutContext_Reports()
    {
        var source = """
            using System.Text.Json;

            class C
            {
                void M()
                {
                    var json = {|#0:JsonSerializer.Serialize(new { Name = "test" })|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseJsonSourceGenerationAnalyzer>
            .Diagnostic(DiagnosticIds.UseJsonSourceGeneration)
            .WithLocation(0)
            .WithArguments("Serialize");

        await CSharpAnalyzerVerifier<UseJsonSourceGenerationAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task JsonDeserializeWithoutContext_Reports()
    {
        var source = """
            using System.Text.Json;

            class C
            {
                void M(string json)
                {
                    var obj = {|#0:JsonSerializer.Deserialize<int>(json)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseJsonSourceGenerationAnalyzer>
            .Diagnostic(DiagnosticIds.UseJsonSourceGeneration)
            .WithLocation(0)
            .WithArguments("Deserialize");

        await CSharpAnalyzerVerifier<UseJsonSourceGenerationAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task JsonSerialize_PreNet7_NoDiagnostic()
    {
        var source = """
            using System.Text.Json;

            class C
            {
                void M()
                {
                    var json = JsonSerializer.Serialize(new { Name = "test" });
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseJsonSourceGenerationAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net6.0");
    }

    [Fact]
    public async Task NonJsonSerializerCall_NoDiagnostic()
    {
        var source = """
            class C
            {
                void M()
                {
                    var s = string.Join(",", new[] { "a", "b" });
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseJsonSourceGenerationAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }
}
