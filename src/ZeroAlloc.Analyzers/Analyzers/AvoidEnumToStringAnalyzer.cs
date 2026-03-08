using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidEnumToStringAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.AvoidEnumToString,
        "Avoid Enum.ToString() — allocates a string",
        "Enum.ToString() allocates on every call — consider using a cached lookup or nameof()",
        DiagnosticCategories.Enums,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation,
            SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        if (memberAccess.Name.Identifier.Text != "ToString")
            return;

        if (invocation.ArgumentList.Arguments.Count != 0)
            return;

        var type = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type;

        if (type is null || type.TypeKind != TypeKind.Enum)
            return;

        var diagnostic = Diagnostic.Create(Rule, memberAccess.Name.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }
}
