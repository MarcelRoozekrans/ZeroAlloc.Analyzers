using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseFrozenDictionaryAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseFrozenDictionary,
        "Use FrozenDictionary for read-only dictionary",
        "Dictionary '{0}' is never mutated after initialization — consider using FrozenDictionary",
        DiagnosticCategories.Collections,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    private static readonly ImmutableHashSet<string> MutatingMethods = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "Add", "Remove", "Clear", "TryAdd");

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Check TFM once per compilation
            if (!TfmHelper.TryGetTfm(compilationContext.Options, out var tfm) || !TfmHelper.IsNet8OrLater(tfm))
                return;

            compilationContext.RegisterSymbolAction(ctx => AnalyzeField(ctx), SymbolKind.Field);
        });
    }

    private static void AnalyzeField(SymbolAnalysisContext context)
    {
        var field = (IFieldSymbol)context.Symbol;

        // Must be readonly
        if (!field.IsReadOnly) return;

        // Must be Dictionary<,>
        if (field.Type is not INamedTypeSymbol namedType) return;
        if (namedType.OriginalDefinition.ToDisplayString() != "System.Collections.Generic.Dictionary<TKey, TValue>")
            return;

        // Check if the field is mutated outside constructors using syntax-only analysis
        var containingType = field.ContainingType;
        foreach (var syntaxRef in containingType.DeclaringSyntaxReferences)
        {
            var typeSyntax = syntaxRef.GetSyntax(context.CancellationToken);

            // Check for mutating method calls on a member with the same name
            foreach (var invocation in typeSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess
                    && MutatingMethods.Contains(memberAccess.Name.Identifier.Text))
                {
                    // Check if target matches the field name syntactically
                    if (IsFieldReference(memberAccess.Expression, field.Name))
                    {
                        var enclosingCtor = invocation.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
                        if (enclosingCtor == null)
                            return; // Mutated outside constructor
                    }
                }
            }

            // Check for indexer assignment: dict["key"] = value (outside constructor)
            foreach (var assignment in typeSyntax.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                if (assignment.Left is ElementAccessExpressionSyntax elementAccess
                    && IsFieldReference(elementAccess.Expression, field.Name))
                {
                    var enclosingCtor = assignment.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
                    // In constructors, indexer init like _lookup["a"] = 1 is fine (part of initialization)
                    if (enclosingCtor == null)
                        return; // Mutated outside constructor
                }
            }
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            field.Locations[0],
            field.Name));
    }

    private static bool IsFieldReference(ExpressionSyntax expression, string fieldName)
    {
        return expression switch
        {
            IdentifierNameSyntax id => id.Identifier.Text == fieldName,
            MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: IdentifierNameSyntax name }
                => name.Identifier.Text == fieldName,
            _ => false
        };
    }
}
