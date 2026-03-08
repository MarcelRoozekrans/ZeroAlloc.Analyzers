using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidParamsInLoopsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.AvoidParamsInLoops,
        "Avoid params calls in loops",
        "params call to '{0}' inside a loop allocates an array on every iteration",
        DiagnosticCategories.Linq,
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

        // Check if inside a loop first (cheap syntactic check)
        if (!IsInsideLoop(invocation))
            return;

        // Resolve the method symbol
        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
            return;

        // Check if the method has a params parameter
        if (method.Parameters.Length == 0)
            return;

        var lastParam = method.Parameters[method.Parameters.Length - 1];
        if (!lastParam.IsParams)
            return;

        // Check that the call site is passing individual arguments (implicit array allocation)
        // rather than an explicit pre-allocated array.
        // If there are more arguments than non-params parameters, the compiler creates an implicit array.
        var argumentCount = invocation.ArgumentList.Arguments.Count;
        var nonParamsParameterCount = method.Parameters.Length - 1;

        if (argumentCount >= method.Parameters.Length)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), method.Name));
        }
    }

    private static bool IsInsideLoop(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is ForStatementSyntax
                or ForEachStatementSyntax
                or WhileStatementSyntax
                or DoStatementSyntax)
            {
                return true;
            }

            // Stop at method/lambda boundaries
            if (current is MethodDeclarationSyntax
                or LocalFunctionStatementSyntax
                or LambdaExpressionSyntax)
            {
                return false;
            }

            current = current.Parent;
        }

        return false;
    }
}
