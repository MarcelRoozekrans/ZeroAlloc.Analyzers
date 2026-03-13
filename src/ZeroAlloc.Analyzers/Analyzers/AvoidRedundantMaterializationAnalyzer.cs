using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidRedundantMaterializationAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.AvoidRedundantMaterialization,
        "Avoid redundant ToList/ToArray on already-materialized collection",
        "'{0}' is already a {1} — '{2}' allocates a new collection unnecessarily; use the original directly",
        DiagnosticCategories.Collections,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.Text;
        if (methodName != "ToList" && methodName != "ToArray")
            return;

        // Verify it's System.Linq.Enumerable.ToList/ToArray
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol method)
            return;

        var containingType = method.ReducedFrom?.ContainingType ?? method.ContainingType;
        if (containingType?.Name != "Enumerable"
            || containingType.ContainingNamespace?.ToDisplayString() != "System.Linq")
            return;

        // Get receiver type
        var receiverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type;
        if (receiverType is null)
            return;

        // ToList on List<T> → redundant
        if (methodName == "ToList" && IsListT(receiverType))
        {
            var receiverName = memberAccess.Expression.ToString();
            var typeName = receiverType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            context.ReportDiagnostic(
                Diagnostic.Create(Rule, memberAccess.Name.GetLocation(),
                    receiverName, typeName, methodName));
        }

        // ToArray on T[] → redundant
        if (methodName == "ToArray" && receiverType is IArrayTypeSymbol { Rank: 1 })
        {
            var receiverName = memberAccess.Expression.ToString();
            var typeName = receiverType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            context.ReportDiagnostic(
                Diagnostic.Create(Rule, memberAccess.Name.GetLocation(),
                    receiverName, typeName, methodName));
        }
    }

    private static bool IsListT(ITypeSymbol type)
        => type is INamedTypeSymbol { IsGenericType: true } named
           && named.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.List<T>";
}
