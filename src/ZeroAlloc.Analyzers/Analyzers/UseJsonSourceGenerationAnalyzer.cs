using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseJsonSourceGenerationAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseJsonSourceGeneration,
        "Use JSON source generation instead of reflection-based serialization",
        "Use JSON source generation instead of reflection-based JsonSerializer.{0} for better performance",
        DiagnosticCategories.Serialization,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            if (!TfmHelper.TryGetTfm(compilationContext.Options, out var tfm)
                || !TfmHelper.IsNet7OrLater(tfm))
            {
                return;
            }

            compilationContext.RegisterSyntaxNodeAction(AnalyzeInvocation,
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (!(symbolInfo.Symbol is IMethodSymbol method))
            return;

        var containingType = method.ContainingType;
        if (containingType.Name != "JsonSerializer"
            || containingType.ContainingNamespace == null
            || containingType.ContainingNamespace.ToDisplayString() != "System.Text.Json")
        {
            return;
        }

        var methodName = method.Name;
        if (methodName != "Serialize" && methodName != "Deserialize"
            && methodName != "SerializeAsync" && methodName != "DeserializeAsync"
            && methodName != "SerializeToUtf8Bytes" && methodName != "SerializeToElement"
            && methodName != "SerializeToDocument" && methodName != "SerializeToNode")
        {
            return;
        }

        // Check if any parameter accepts JsonTypeInfo<T> or JsonSerializerContext
        foreach (var param in method.Parameters)
        {
            var paramTypeName = param.Type.Name;
            if (paramTypeName == "JsonTypeInfo" || paramTypeName == "JsonSerializerContext")
                return; // Already using source gen

            // Also check for JsonTypeInfo<T> which is a generic type
            if (param.Type is INamedTypeSymbol namedType && namedType.IsGenericType
                && namedType.ConstructedFrom.Name == "JsonTypeInfo")
            {
                return;
            }
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), methodName));
    }
}
