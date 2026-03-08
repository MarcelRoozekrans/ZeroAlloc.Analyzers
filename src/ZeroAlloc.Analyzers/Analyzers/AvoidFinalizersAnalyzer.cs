using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidFinalizersAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.AvoidFinalizers,
        "Avoid finalizers — use IDisposable pattern instead",
        "Class '{0}' has a finalizer which promotes objects to Gen1+ \u2014 use IDisposable pattern instead",
        DiagnosticCategories.ValueTypes,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeDestructor, SyntaxKind.DestructorDeclaration);
    }

    private static void AnalyzeDestructor(SyntaxNodeAnalysisContext context)
    {
        var destructor = (DestructorDeclarationSyntax)context.Node;

        // Report on ~ClassName() span (tilde through closing paren), excluding the body
        var start = destructor.TildeToken.SpanStart;
        var end = destructor.ParameterList.CloseParenToken.Span.End;
        var span = Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(start, end);
        var location = Location.Create(destructor.SyntaxTree, span);

        context.ReportDiagnostic(Diagnostic.Create(Rule, location, destructor.Identifier.Text));
    }
}
