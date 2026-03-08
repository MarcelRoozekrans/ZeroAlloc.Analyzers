using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseLoggerMessageAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseLoggerMessage,
        "Use LoggerMessage source generator instead of ILogger.Log* methods",
        "Use [LoggerMessage] source generator instead of '{0}' to avoid boxing and string interpolation allocations",
        DiagnosticCategories.Logging,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    private static readonly ImmutableHashSet<string> LogMethodNames = ImmutableHashSet.Create(
        "Log",
        "LogTrace",
        "LogDebug",
        "LogInformation",
        "LogWarning",
        "LogError",
        "LogCritical");

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            if (!TfmHelper.TryGetTfm(compilationContext.Options, out var tfm) || !TfmHelper.IsNet6OrLater(tfm))
                return;

            compilationContext.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // We only care about member access: logger.LogInformation(...)
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.Text;
        if (!LogMethodNames.Contains(methodName))
            return;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        if (!IsLoggerMethod(methodSymbol))
            return;

        // Skip if the containing class already has any method with [LoggerMessage]
        if (ContainingClassHasLoggerMessageAttribute(context))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, memberAccess.Name.GetLocation(), methodName));
    }

    private static bool IsLoggerMethod(IMethodSymbol methodSymbol)
    {
        // For extension methods, check the ReducedFrom (original definition)
        var containingType = methodSymbol.ReducedFrom?.ContainingType ?? methodSymbol.ContainingType;
        if (containingType is null)
            return false;

        var namespaceName = containingType.ContainingNamespace?.ToDisplayString();
        if (namespaceName != "Microsoft.Extensions.Logging")
            return false;

        var typeName = containingType.Name;
        return typeName == "ILogger" || typeName == "LoggerExtensions";
    }

    private static bool ContainingClassHasLoggerMessageAttribute(SyntaxNodeAnalysisContext context)
    {
        var classDecl = context.Node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (classDecl is null)
            return false;

        foreach (var member in classDecl.Members)
        {
            if (member is not MethodDeclarationSyntax method)
                continue;

            foreach (var attrList in method.AttributeLists)
            {
                foreach (var attr in attrList.Attributes)
                {
                    var attrName = attr.Name.ToString();
                    if (attrName == "LoggerMessage" || attrName == "LoggerMessageAttribute")
                        return true;
                }
            }
        }

        return false;
    }
}
