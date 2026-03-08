using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DisposeCancellationTokenSourceAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.DisposeCancellationTokenSource,
        "Dispose CancellationTokenSource",
        "CancellationTokenSource should be disposed \u2014 use a 'using' statement to avoid resource leaks",
        DiagnosticCategories.Async,
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

        if (typeInfo.Type == null)
            return;

        if (typeInfo.Type.Name != "CancellationTokenSource")
            return;

        if (typeInfo.Type.ContainingNamespace == null
            || typeInfo.Type.ContainingNamespace.ToDisplayString() != "System.Threading")
            return;

        // Walk up to determine if inside a using statement or using declaration
        var parent = objectCreation.Parent;
        while (parent != null)
        {
            if (parent is UsingStatementSyntax)
                return; // OK — using statement

            if (parent is LocalDeclarationStatementSyntax localDecl
                && localDecl.UsingKeyword.IsKind(SyntaxKind.UsingKeyword))
                return; // OK — using declaration

            if (parent is EqualsValueClauseSyntax
                || parent is VariableDeclaratorSyntax
                || parent is VariableDeclarationSyntax)
            {
                parent = parent.Parent;
                continue;
            }

            break;
        }

        // Skip field-level assignments (class manages lifetime)
        if (parent is FieldDeclarationSyntax)
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, objectCreation.GetLocation()));
    }
}
