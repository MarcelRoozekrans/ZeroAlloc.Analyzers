using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseCompositeFormatAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseCompositeFormat,
        "Use CompositeFormat.Parse to cache format strings",
        "Consider using CompositeFormat.Parse() to cache the format string for better performance",
        DiagnosticCategories.Strings,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationContext =>
        {
            if (!TfmHelper.TryGetTfm(compilationContext.Options, out var tfm) || !TfmHelper.IsNet8OrLater(tfm))
                return;

            compilationContext.RegisterSyntaxNodeAction(AnalyzeInvocation,
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        if (memberAccess.Name.Identifier.Text != "Format")
            return;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);

        if (symbolInfo.Symbol is not IMethodSymbol method)
            return;

        if (method.ContainingType.SpecialType != SpecialType.System_String)
            return;

        var args = invocation.ArgumentList.Arguments;
        if (args.Count < 2)
            return;

        var firstArg = args[0].Expression;

        // Check if the format string is a compile-time constant
        var constantValue = context.SemanticModel.GetConstantValue(firstArg, context.CancellationToken);
        if (constantValue.HasValue && constantValue.Value is string)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
            return;
        }

        // Check if the format string references a static readonly field
        var argSymbolInfo = context.SemanticModel.GetSymbolInfo(firstArg, context.CancellationToken);
        if (argSymbolInfo.Symbol is IFieldSymbol field &&
            field.IsStatic &&
            field.IsReadOnly &&
            field.Type.SpecialType == SpecialType.System_String)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
        }
    }
}
