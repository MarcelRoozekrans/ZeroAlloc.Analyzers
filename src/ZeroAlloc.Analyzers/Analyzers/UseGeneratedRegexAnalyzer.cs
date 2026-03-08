using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseGeneratedRegexAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseGeneratedRegex,
        "Use GeneratedRegex for compile-time regex",
        "Use [GeneratedRegex] source generator instead of 'new Regex()' to avoid runtime compilation overhead",
        DiagnosticCategories.Regex,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            if (!TfmHelper.TryGetTfm(compilationContext.Options, out var tfm)
                || !TfmHelper.IsNetOrLater(tfm, 7))
            {
                return;
            }

            compilationContext.RegisterSyntaxNodeAction(AnalyzeObjectCreation,
                SyntaxKind.ObjectCreationExpression);
        });
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

        // Must have arguments: new Regex(pattern) or new Regex(pattern, options)
        var args = objectCreation.ArgumentList?.Arguments;
        if (args == null || (args.Value.Count != 1 && args.Value.Count != 2))
            return;

        // Verify the constructor belongs to System.Text.RegularExpressions.Regex
        var symbolInfo = context.SemanticModel.GetSymbolInfo(objectCreation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol constructor)
            return;

        var containingType = constructor.ContainingType;
        if (containingType.Name != "Regex"
            || containingType.ContainingNamespace?.ToDisplayString() != "System.Text.RegularExpressions")
        {
            return;
        }

        // Only flag if the first argument (pattern) is a compile-time constant
        var patternArg = args.Value[0].Expression;
        var constantValue = context.SemanticModel.GetConstantValue(patternArg, context.CancellationToken);
        if (!constantValue.HasValue || constantValue.Value is not string)
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, objectCreation.GetLocation()));
    }
}
