using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ElideAsyncAwaitAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.ElideAsyncAwait,
        "Elide async/await for simple tail calls",
        "Async method only performs a single await on return \u2014 consider returning the Task directly to avoid state machine allocation",
        DiagnosticCategories.Async,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;

        if (!method.Modifiers.Any(SyntaxKind.AsyncKeyword))
            return;

        // Expression-bodied: async Task<int> M() => await Something();
        if (method.ExpressionBody != null)
        {
            if (method.ExpressionBody.Expression is AwaitExpressionSyntax)
            {
                ReportOnAsyncKeyword(context, method);
            }

            return;
        }

        if (method.Body == null)
            return;

        // Check for exactly one statement (excluding local function declarations) that is return-await
        var statements = method.Body.Statements;
        if (statements.Count != 1)
            return;

        if (statements[0] is ReturnStatementSyntax returnStmt
            && returnStmt.Expression is AwaitExpressionSyntax)
        {
            ReportOnAsyncKeyword(context, method);
        }
    }

    private static void ReportOnAsyncKeyword(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method)
    {
        foreach (var modifier in method.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.AsyncKeyword))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, modifier.GetLocation()));
                return;
            }
        }
    }
}
