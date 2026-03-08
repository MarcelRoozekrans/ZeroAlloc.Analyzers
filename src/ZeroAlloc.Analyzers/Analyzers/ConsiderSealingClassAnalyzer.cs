using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConsiderSealingClassAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.ConsiderSealingClass,
        "Consider sealing class to enable JIT devirtualization",
        "Class '{0}' can be sealed to enable JIT devirtualization",
        DiagnosticCategories.Sealing,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var baseTypes = new ConcurrentDictionary<INamedTypeSymbol, byte>(SymbolEqualityComparer.Default);

            compilationContext.RegisterSymbolAction(symbolContext =>
            {
                var type = (INamedTypeSymbol)symbolContext.Symbol;
                if (type.TypeKind != TypeKind.Class)
                    return;

                // Mark its base type as "has derived types"
                if (type.BaseType != null && type.BaseType.SpecialType != SpecialType.System_Object)
                    baseTypes.TryAdd(type.BaseType, 0);
            }, SymbolKind.NamedType);

            compilationContext.RegisterCompilationEndAction(endContext =>
            {
                foreach (var type in GetAllTypes(endContext.Compilation.Assembly.GlobalNamespace))
                {
                    if (type.TypeKind != TypeKind.Class)
                        continue;
                    if (type.IsImplicitlyDeclared)
                        continue;
                    if (type.IsSealed || type.IsAbstract || type.IsStatic)
                        continue;
                    if (type.IsRecord)
                        continue;
                    if (baseTypes.ContainsKey(type))
                        continue;

                    var location = type.Locations.FirstOrDefault();
                    if (location != null)
                        endContext.ReportDiagnostic(Diagnostic.Create(Rule, location, type.Name));
                }
            });
        });
    }

    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
    {
        foreach (var type in ns.GetTypeMembers())
            yield return type;

        foreach (var nested in ns.GetNamespaceMembers())
            foreach (var type in GetAllTypes(nested))
                yield return type;
    }
}
