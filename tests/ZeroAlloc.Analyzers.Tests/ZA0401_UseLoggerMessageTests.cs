using Microsoft.CodeAnalysis.Testing;
using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0401_UseLoggerMessageTests
{
    private static readonly ReferenceAssemblies Net80WithLogging =
        ReferenceAssemblies.Net.Net80.AddPackages([
            new PackageIdentity("Microsoft.Extensions.Logging.Abstractions", "8.0.0")]);

    [Fact]
    public async Task LogInformation_Reports()
    {
        var source = """
            using Microsoft.Extensions.Logging;

            class C
            {
                void M(ILogger logger)
                {
                    logger.{|#0:LogInformation|}("Hello");
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseLoggerMessageAnalyzer>
            .Diagnostic(DiagnosticIds.UseLoggerMessage)
            .WithLocation(0)
            .WithArguments("LogInformation");

        await CSharpAnalyzerVerifier<UseLoggerMessageAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", Net80WithLogging, expected);
    }

    [Fact]
    public async Task LogWarning_Reports()
    {
        var source = """
            using Microsoft.Extensions.Logging;

            class C
            {
                void M(ILogger logger)
                {
                    logger.{|#0:LogWarning|}("Warning!");
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseLoggerMessageAnalyzer>
            .Diagnostic(DiagnosticIds.UseLoggerMessage)
            .WithLocation(0)
            .WithArguments("LogWarning");

        await CSharpAnalyzerVerifier<UseLoggerMessageAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", Net80WithLogging, expected);
    }

    [Fact]
    public async Task OnNet5_NoDiagnostic()
    {
        var source = """
            using Microsoft.Extensions.Logging;

            class C
            {
                void M(ILogger logger)
                {
                    logger.LogInformation("Hello");
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseLoggerMessageAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net5.0", Net80WithLogging);
    }

    [Fact]
    public async Task NonLoggerMethod_NoDiagnostic()
    {
        var source = """
            class MyLogger
            {
                public void LogInformation(string msg) { }
            }

            class C
            {
                void M()
                {
                    var logger = new MyLogger();
                    logger.LogInformation("Hello");
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseLoggerMessageAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }
}
