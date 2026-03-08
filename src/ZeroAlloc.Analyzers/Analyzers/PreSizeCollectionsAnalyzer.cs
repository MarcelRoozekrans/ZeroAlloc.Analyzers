using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreSizeCollectionsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.PreSizeCollections,
        "Pre-size collections when the capacity is known",
        "Consider pre-sizing '{0}' with the known capacity to avoid repeated reallocations",
        DiagnosticCategories.Collections,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation,
            SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var creation = (ObjectCreationExpressionSyntax)context.Node;

        // Must have zero arguments
        if (creation.ArgumentList == null || creation.ArgumentList.Arguments.Count != 0)
            return;

        // Must be assigned to a local variable: var x = new List<T>();
        if (creation.Parent is not EqualsValueClauseSyntax equalsValue)
            return;

        if (equalsValue.Parent is not VariableDeclaratorSyntax declarator)
            return;

        var variableName = declarator.Identifier.Text;

        // Check the type is List<T> from System.Collections.Generic
        var typeInfo = context.SemanticModel.GetTypeInfo(creation, context.CancellationToken);
        if (typeInfo.Type is not INamedTypeSymbol namedType)
            return;

        if (!namedType.IsGenericType)
            return;

        var originalDef = namedType.OriginalDefinition;
        if (originalDef.ContainingNamespace == null)
            return;

        if (!IsSystemCollectionsGenericNamespace(originalDef.ContainingNamespace))
            return;

        if (originalDef.Name != "List" || originalDef.Arity != 1)
            return;

        // Find the containing method or local function body
        var containingBody = FindContainingBody(creation);
        if (containingBody == null)
            return;

        // Look for variableName.AddRange(...) in the containing body
        var hasAddRange = containingBody
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(inv => IsAddRangeOnVariable(inv, variableName));

        if (!hasAddRange)
            return;

        var displayType = namedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var diagnostic = Diagnostic.Create(Rule, creation.GetLocation(), displayType);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsAddRangeOnVariable(InvocationExpressionSyntax invocation, string variableName)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        if (memberAccess.Name.Identifier.Text != "AddRange")
            return false;

        if (memberAccess.Expression is not IdentifierNameSyntax identifier)
            return false;

        return identifier.Identifier.Text == variableName;
    }

    private static SyntaxNode? FindContainingBody(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is MethodDeclarationSyntax method)
                return (SyntaxNode?)method.Body ?? method.ExpressionBody;

            if (current is LocalFunctionStatementSyntax localFunc)
                return (SyntaxNode?)localFunc.Body ?? localFunc.ExpressionBody;

            if (current is AccessorDeclarationSyntax accessor)
                return (SyntaxNode?)accessor.Body ?? accessor.ExpressionBody;

            if (current is ConstructorDeclarationSyntax ctor)
                return (SyntaxNode?)ctor.Body ?? ctor.ExpressionBody;

            current = current.Parent;
        }

        return null;
    }

    private static bool IsSystemCollectionsGenericNamespace(INamespaceSymbol ns)
    {
        return ns.Name == "Generic"
               && ns.ContainingNamespace?.Name == "Collections"
               && ns.ContainingNamespace.ContainingNamespace?.Name == "System"
               && ns.ContainingNamespace.ContainingNamespace.ContainingNamespace?.IsGlobalNamespace == true;
    }
}
