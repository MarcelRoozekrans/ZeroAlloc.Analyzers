using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseIndexerOverLinqFirstAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseIndexerOverLinqFirst,
        "Use indexer instead of LINQ First()/Last()",
        "Use indexer access instead of LINQ '.{0}()' to avoid enumerator allocation",
        DiagnosticCategories.Linq,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    private static readonly ImmutableHashSet<string> TargetMethods = ImmutableHashSet.Create("First", "Last");

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
        if (!TargetMethods.Contains(methodName))
            return;

        // Must have no arguments (no predicate overload)
        if (invocation.ArgumentList.Arguments.Count != 0)
            return;

        // Verify the method is System.Linq.Enumerable.First/Last<T>
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        var containingType = methodSymbol.ContainingType;
        if (containingType?.Name != "Enumerable")
            return;

        if (!IsSystemLinqNamespace(containingType.ContainingNamespace))
            return;

        // Check receiver type implements IList<T> or is an array
        var receiverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression).Type;
        if (receiverType == null)
            return;

        if (!IsIndexableType(receiverType, context.Compilation))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, memberAccess.Name.GetLocation(), methodName));
    }

    private static bool IsIndexableType(ITypeSymbol type, Compilation compilation)
    {
        // Arrays are indexable
        if (type is IArrayTypeSymbol)
            return true;

        // Check if the type implements IList<T>
        var ilistOfT = compilation.GetTypeByMetadataName("System.Collections.Generic.IList`1");
        if (ilistOfT == null)
            return false;

        // Check if the type itself is IList<T>
        if (type.OriginalDefinition.Equals(ilistOfT, SymbolEqualityComparer.Default))
            return true;

        // Check if it implements IList<T>
        return type.AllInterfaces.Any(i =>
            i.OriginalDefinition.Equals(ilistOfT, SymbolEqualityComparer.Default));
    }

    private static bool IsSystemLinqNamespace(INamespaceSymbol? ns)
    {
        return ns is { Name: "Linq" }
               && ns.ContainingNamespace?.Name == "System"
               && ns.ContainingNamespace.ContainingNamespace?.IsGlobalNamespace == true;
    }
}
