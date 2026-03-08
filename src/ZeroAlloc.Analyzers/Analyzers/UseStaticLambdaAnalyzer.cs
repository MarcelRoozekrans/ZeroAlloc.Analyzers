using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseStaticLambdaAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseStaticLambda,
        "Use static lambda to prevent accidental closures",
        "Lambda does not capture any variables — add 'static' modifier to prevent accidental closures",
        DiagnosticCategories.Delegates,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            if (!TfmHelper.TryGetTfm(compilationContext.Options, out var tfm) || !TfmHelper.IsNet5OrLater(tfm))
                return;

            compilationContext.RegisterSyntaxNodeAction(
                AnalyzeLambda,
                SyntaxKind.SimpleLambdaExpression,
                SyntaxKind.ParenthesizedLambdaExpression);
        });
    }

    private static void AnalyzeLambda(SyntaxNodeAnalysisContext context)
    {
        var lambda = (LambdaExpressionSyntax)context.Node;

        // Skip if already marked static
        if (lambda.Modifiers.Any(SyntaxKind.StaticKeyword))
            return;

        // The body can be an expression or a block
        var body = lambda.Body;
        if (body == null)
            return;

        var dataFlow = context.SemanticModel.AnalyzeDataFlow(body);
        if (dataFlow == null || !dataFlow.Succeeded)
            return;

        // If Captured is empty, the lambda does not close over any variables
        if (dataFlow.Captured.IsEmpty)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, lambda.GetLocation()));
        }
    }
}
