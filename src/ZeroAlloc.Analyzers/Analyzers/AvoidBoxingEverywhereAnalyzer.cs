using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

/// <summary>
/// Strict opt-in rule that flags all boxing conversions, not just those in loops.
/// Disabled by default — enable via .editorconfig: dotnet_diagnostic.ZA0503.severity = info
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidBoxingEverywhereAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.AvoidBoxingEverywhere,
        "Avoid boxing value types",
        "Value type '{0}' is boxed to '{1}' — consider using a generic overload to avoid the allocation",
        DiagnosticCategories.Boxing,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    private static readonly SymbolDisplayFormat ShortFormat = SymbolDisplayFormat.MinimallyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol method)
            return;

        var arguments = invocation.ArgumentList.Arguments;
        var parameters = method.Parameters;

        for (int i = 0; i < arguments.Count && i < parameters.Length; i++)
        {
            var param = parameters[i];

            var effectiveParam = param;
            if (i >= parameters.Length - 1 && parameters[parameters.Length - 1].IsParams)
            {
                effectiveParam = parameters[parameters.Length - 1];
                if (effectiveParam.Type is IArrayTypeSymbol arrayType
                    && arrayType.ElementType.SpecialType == SpecialType.System_Object)
                {
                    CheckArgForBoxing(context, arguments[i].Expression, arrayType.ElementType);
                    continue;
                }
            }

            if (IsBoxingTarget(effectiveParam.Type))
            {
                CheckArgForBoxing(context, arguments[i].Expression, effectiveParam.Type);
            }
        }
    }

    private static void CheckArgForBoxing(
        SyntaxNodeAnalysisContext context,
        ExpressionSyntax argExpression,
        ITypeSymbol paramType)
    {
        var typeInfo = context.SemanticModel.GetTypeInfo(argExpression, context.CancellationToken);
        if (typeInfo.Type != null && typeInfo.Type.IsValueType
            && !typeInfo.Type.IsReferenceType)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rule, argExpression.GetLocation(),
                    typeInfo.Type.ToDisplayString(ShortFormat), paramType.ToDisplayString(ShortFormat)));
        }
    }

    private static bool IsBoxingTarget(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_Object)
            return true;

        if (type.TypeKind == TypeKind.Interface)
            return true;

        return false;
    }
}
