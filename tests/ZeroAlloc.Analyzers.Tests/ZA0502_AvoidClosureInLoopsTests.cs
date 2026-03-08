using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0502_AvoidClosureInLoopsTests
{
    [Fact]
    public async Task LambdaCapturingLoopVariable_Reports()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(List<int> items)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var result = items.Where(x {|#0:=>|} x > i);
                    }
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidClosureInLoopsAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidClosureInLoops)
            .WithLocation(0)
            .WithArguments("i");

        await CSharpAnalyzerVerifier<AvoidClosureInLoopsAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task LambdaCapturingOuterVariable_InLoop_Reports()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(List<int> items)
                {
                    int threshold = 5;
                    for (int i = 0; i < 10; i++)
                    {
                        var result = items.Where(x {|#0:=>|} x > threshold);
                    }
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidClosureInLoopsAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidClosureInLoops)
            .WithLocation(0)
            .WithArguments("threshold");

        await CSharpAnalyzerVerifier<AvoidClosureInLoopsAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task LambdaWithNoCapture_InLoop_NoDiagnostic()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(List<int> items)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var result = items.Where(x => x > 0);
                    }
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidClosureInLoopsAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }

    [Fact]
    public async Task LambdaCapturingVariable_OutsideLoop_NoDiagnostic()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(List<int> items)
                {
                    int threshold = 5;
                    var result = items.Where(x => x > threshold);
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidClosureInLoopsAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net8.0");
    }
}
