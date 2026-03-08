using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OverrideStructGetHashCodeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.OverrideStructGetHashCode,
        "Override GetHashCode on structs used as dictionary/set keys",
        "Struct '{0}' used as dictionary/set key does not override GetHashCode() — default uses reflection and causes boxing",
        DiagnosticCategories.ValueTypes,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
        var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation);

        if (!(typeInfo.Type is INamedTypeSymbol namedType))
            return;

        if (!namedType.IsGenericType)
            return;

        var originalDefinition = namedType.OriginalDefinition.ToDisplayString();

        bool isDictionary = originalDefinition == "System.Collections.Generic.Dictionary<TKey, TValue>";
        bool isHashSet = originalDefinition == "System.Collections.Generic.HashSet<T>";

        if (!isDictionary && !isHashSet)
            return;

        // Get the key type (first type arg for Dictionary, only type arg for HashSet)
        var keyType = namedType.TypeArguments[0];

        if (!keyType.IsValueType || keyType.TypeKind == TypeKind.Enum)
            return;

        // Check if it overrides GetHashCode
        bool overridesGetHashCode = keyType.GetMembers("GetHashCode")
            .OfType<IMethodSymbol>()
            .Any(m => m.IsOverride && m.Parameters.Length == 0);

        if (!overridesGetHashCode)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, objectCreation.GetLocation(), keyType.Name));
        }
    }
}
