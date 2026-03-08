using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidSpanToStringBeforeParseAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.AvoidSpanToStringBeforeParse,
        "Avoid converting Span to string before Parse",
        "Avoid converting Span to string before calling {0}.Parse — use the ReadOnlySpan<char> overload directly",
        DiagnosticCategories.Strings,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationContext =>
        {
            if (!TfmHelper.TryGetTfm(compilationContext.Options, out var tfm) || !TfmHelper.IsNet6OrLater(tfm))
                return;

            compilationContext.RegisterSyntaxNodeAction(AnalyzeInvocation,
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        if (memberAccess.Name.Identifier.Text != "Parse")
            return;

        var args = invocation.ArgumentList.Arguments;
        if (args.Count < 1)
            return;

        // Check if first argument is a .ToString() call
        var firstArg = args[0].Expression;
        if (firstArg is not InvocationExpressionSyntax innerInvocation)
            return;

        if (innerInvocation.Expression is not MemberAccessExpressionSyntax innerMemberAccess)
            return;

        if (innerMemberAccess.Name.Identifier.Text != "ToString")
            return;

        if (innerInvocation.ArgumentList.Arguments.Count != 0)
            return;

        // Check if the receiver of .ToString() is ReadOnlySpan<char> or Span<char>
        var receiverType = context.SemanticModel.GetTypeInfo(innerMemberAccess.Expression, context.CancellationToken).Type;
        if (receiverType == null)
            return;

        if (receiverType is not INamedTypeSymbol namedType || !namedType.IsGenericType)
            return;

        if (namedType.Name != "ReadOnlySpan" && namedType.Name != "Span")
            return;

        if (namedType.TypeArguments.Length != 1 ||
            namedType.TypeArguments[0].SpecialType != SpecialType.System_Char)
            return;

        // Verify the outer Parse method resolves to a real method
        var parseMethod = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
        if (parseMethod?.ContainingType == null)
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(),
            parseMethod.ContainingType.Name));
    }
}
