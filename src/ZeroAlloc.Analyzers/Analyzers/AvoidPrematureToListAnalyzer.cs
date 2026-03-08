using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidPrematureToListAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.AvoidPrematureToList,
        "Avoid premature ToList/ToArray before LINQ",
        "'{0}' materializes the collection before '{1}' — apply '{1}' first to avoid unnecessary allocation",
        DiagnosticCategories.Collections,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    // LINQ methods that filter or project (reduce) data — calling ToList before these wastes memory
    private static readonly ImmutableHashSet<string> FilteringLinqMethods = ImmutableHashSet.Create(
        "Where", "Select", "SelectMany", "First", "FirstOrDefault",
        "Last", "LastOrDefault", "Single", "SingleOrDefault",
        "Take", "Skip", "Distinct", "Any", "All", "Count",
        "OrderBy", "OrderByDescending", "GroupBy");

    // Materialization methods that trigger allocation
    private static readonly ImmutableHashSet<string> MaterializationMethods = ImmutableHashSet.Create(
        "ToList", "ToArray", "ToDictionary", "ToHashSet", "ToLookup");

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Pattern: something.ToList().Where(...)
        // The outer call is Where/Select/etc., the inner receiver is .ToList()
        if (invocation.Expression is not MemberAccessExpressionSyntax outerAccess)
            return;

        var outerMethodName = outerAccess.Name.Identifier.Text;
        if (!FilteringLinqMethods.Contains(outerMethodName))
            return;

        // Check if the receiver is itself an invocation: xxx.ToList()
        if (outerAccess.Expression is not InvocationExpressionSyntax innerInvocation)
            return;

        if (innerInvocation.Expression is not MemberAccessExpressionSyntax innerAccess)
            return;

        var innerMethodName = innerAccess.Name.Identifier.Text;
        if (!MaterializationMethods.Contains(innerMethodName))
            return;

        // Verify the inner method is a LINQ materialization (System.Linq.Enumerable)
        var innerSymbol = context.SemanticModel.GetSymbolInfo(innerInvocation, context.CancellationToken);
        if (innerSymbol.Symbol is not IMethodSymbol innerMethod)
            return;

        var containingType = innerMethod.ReducedFrom?.ContainingType ?? innerMethod.ContainingType;
        if (containingType?.Name != "Enumerable"
            || containingType.ContainingNamespace?.ToDisplayString() != "System.Linq")
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(Rule, innerAccess.Name.GetLocation(),
                innerMethodName, outerMethodName));
    }
}
