using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseCountPropertyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseCountProperty,
        "Use Count/Length property instead of LINQ Count()",
        "Use '.{0}' property instead of LINQ '.Count()' to avoid enumerator allocation",
        DiagnosticCategories.Linq,
        DiagnosticSeverity.Info,
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

        if (memberAccess.Name.Identifier.Text != "Count")
            return;

        // Must have no arguments (no predicate overload)
        if (invocation.ArgumentList.Arguments.Count != 0)
            return;

        // Verify the method is System.Linq.Enumerable.Count<T>
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        var containingType = methodSymbol.ContainingType;
        if (containingType?.Name != "Enumerable")
            return;

        if (!IsSystemLinqNamespace(containingType.ContainingNamespace))
            return;

        // Check receiver has Count or Length property
        var receiverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression).Type;
        if (receiverType == null)
            return;

        string? propertyName = FindCountOrLengthProperty(receiverType);
        if (propertyName == null)
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, memberAccess.Name.GetLocation(), propertyName));
    }

    private static string? FindCountOrLengthProperty(ITypeSymbol type)
    {
        // Arrays have Length as a special member
        if (type is IArrayTypeSymbol)
            return "Length";

        // Check direct members and base types
        var current = type;
        while (current != null)
        {
            var prop = current.GetMembers().OfType<IPropertySymbol>()
                .FirstOrDefault(p => (p.Name == "Count" || p.Name == "Length") && !p.IsIndexer);
            if (prop != null)
                return prop.Name;
            current = current.BaseType;
        }

        return null;
    }

    private static bool IsSystemLinqNamespace(INamespaceSymbol? ns)
    {
        return ns is { Name: "Linq" }
               && ns.ContainingNamespace?.Name == "System"
               && ns.ContainingNamespace.ContainingNamespace?.IsGlobalNamespace == true;
    }
}
