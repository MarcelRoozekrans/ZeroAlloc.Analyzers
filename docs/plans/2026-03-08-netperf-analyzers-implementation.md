# ZeroAlloc Analyzers Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a standalone open-source Roslyn analyzer NuGet package that detects modern .NET performance anti-patterns with multi-TFM-aware code fixes.

**Architecture:** Single analyzer assembly (netstandard2.0) with runtime TFM detection via `CompilerVisibleProperty`. Ships as a NuGet package with bundled `.props` for TFM flow. Code fixes transform patterns in-place with proper `using` management.

**Tech Stack:** Roslyn Analyzers API (`Microsoft.CodeAnalysis.CSharp`), `Microsoft.CodeAnalysis.Testing` for verify-style tests, xUnit, NuGet packaging.

**Repository:** New standalone repo (e.g., `zeroalloc-analyzers`), NOT inside roslyn-codegraph-mcp.

---

## Task 1: Repository & Solution Scaffolding

**Files:**
- Create: `ZeroAlloc.Analyzers.sln`
- Create: `Directory.Build.props`
- Create: `.editorconfig`
- Create: `.gitignore`
- Create: `LICENSE`
- Create: `README.md`

**Step 1: Create the repository and solution**

```bash
mkdir zeroalloc-analyzers && cd zeroalloc-analyzers
git init
dotnet new sln -n ZeroAlloc.Analyzers
```

**Step 2: Create Directory.Build.props with shared settings**

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

**Step 3: Add .gitignore, LICENSE (MIT), and a minimal README**

Use `dotnet new gitignore` for .gitignore. Add MIT license file. README should contain: project name, one-line description, "work in progress" note.

**Step 4: Commit**

```bash
git add -A
git commit -m "chore: initial repository scaffolding"
```

---

## Task 2: Analyzer Project Setup

**Files:**
- Create: `src/ZeroAlloc.Analyzers/ZeroAlloc.Analyzers.csproj`
- Create: `src/ZeroAlloc.Analyzers/DiagnosticIds.cs`
- Create: `src/ZeroAlloc.Analyzers/DiagnosticCategories.cs`
- Create: `src/ZeroAlloc.Analyzers/TfmHelper.cs`

**Step 1: Create the analyzer project**

```bash
dotnet new classlib -n ZeroAlloc.Analyzers -o src/ZeroAlloc.Analyzers -f netstandard2.0
dotnet sln add src/ZeroAlloc.Analyzers
```

**Step 2: Configure the csproj for analyzer authoring**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <IsRoslynComponent>true</IsRoslynComponent>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.14.0" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Note: `EnforceExtendedAnalyzerRules` enables RS* diagnostics that catch common analyzer authoring mistakes. `IsRoslynComponent` marks this as a Roslyn analyzer for tooling.

**Step 3: Create DiagnosticIds.cs**

```csharp
namespace ZeroAlloc.Analyzers;

public static class DiagnosticIds
{
    public const string UseFrozenDictionary = "NP0001";
    public const string UseFrozenSet = "NP0002";
    public const string UseCollectionsMarshalAsSpan = "NP0003";
    public const string UseSearchValues = "NP0004";
    public const string AvoidStringConcatInLoop = "NP0005";
    public const string UseStackalloc = "NP0006";
    public const string UseArrayPool = "NP0007";
    public const string AvoidEnumHasFlag = "NP0008";
    public const string AvoidStringReplaceChain = "NP0009";
    public const string UseTryGetValue = "NP0010";
}
```

**Step 4: Create DiagnosticCategories.cs**

```csharp
namespace ZeroAlloc.Analyzers;

public static class DiagnosticCategories
{
    public const string Collections = "Performance.Collections";
    public const string Strings = "Performance.Strings";
    public const string Memory = "Performance.Memory";
}
```

**Step 5: Create TfmHelper.cs**

```csharp
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

internal static class TfmHelper
{
    public static bool TryGetTfm(AnalyzerOptions options, out string tfm)
    {
        if (options.AnalyzerConfigOptionsProvider
            .GlobalOptions
            .TryGetValue("build_property.TargetFramework", out var value)
            && !string.IsNullOrEmpty(value))
        {
            tfm = value;
            return true;
        }

        tfm = string.Empty;
        return false;
    }

    public static bool IsNetOrLater(string tfm, int majorVersion)
    {
        // Handles: net5.0, net6.0, net7.0, net8.0, net9.0, net10.0, etc.
        if (tfm.StartsWith("net", StringComparison.OrdinalIgnoreCase)
            && !tfm.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)
            && !tfm.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase))
        {
            var versionPart = tfm.Substring(3);
            var dotIndex = versionPart.IndexOf('.');
            if (dotIndex > 0)
                versionPart = versionPart.Substring(0, dotIndex);
            // Also strip any suffix like -windows
            var dashIndex = versionPart.IndexOf('-');
            if (dashIndex > 0)
                versionPart = versionPart.Substring(0, dashIndex);

            if (int.TryParse(versionPart, out var major))
                return major >= majorVersion;
        }

        // netcoreapp3.1 etc. — treat as < net5
        return false;
    }

    public static bool IsNet5OrLater(string tfm) => IsNetOrLater(tfm, 5);
    public static bool IsNet6OrLater(string tfm) => IsNetOrLater(tfm, 6);
    public static bool IsNet8OrLater(string tfm) => IsNetOrLater(tfm, 8);
}
```

**Step 6: Remove the auto-generated Class1.cs**

```bash
rm src/ZeroAlloc.Analyzers/Class1.cs
```

**Step 7: Verify it builds**

```bash
dotnet build src/ZeroAlloc.Analyzers
```

Expected: Build succeeded, 0 errors.

**Step 8: Commit**

```bash
git add src/ZeroAlloc.Analyzers/
git commit -m "feat: add analyzer project with TFM detection infrastructure"
```

---

## Task 3: Code Fixes Project Setup

**Files:**
- Create: `src/ZeroAlloc.Analyzers.CodeFixes/ZeroAlloc.Analyzers.CodeFixes.csproj`

**Step 1: Create the code fixes project**

```bash
dotnet new classlib -n ZeroAlloc.Analyzers.CodeFixes -o src/ZeroAlloc.Analyzers.CodeFixes -f netstandard2.0
dotnet sln add src/ZeroAlloc.Analyzers.CodeFixes
```

**Step 2: Configure the csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <IsRoslynComponent>true</IsRoslynComponent>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="4.14.0" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ZeroAlloc.Analyzers\ZeroAlloc.Analyzers.csproj" />
  </ItemGroup>
</Project>
```

**Step 3: Remove the auto-generated Class1.cs**

```bash
rm src/ZeroAlloc.Analyzers.CodeFixes/Class1.cs
```

**Step 4: Verify it builds**

```bash
dotnet build
```

**Step 5: Commit**

```bash
git add src/ZeroAlloc.Analyzers.CodeFixes/
git commit -m "feat: add code fixes project"
```

---

## Task 4: Test Project Setup

**Files:**
- Create: `tests/ZeroAlloc.Analyzers.Tests/ZeroAlloc.Analyzers.Tests.csproj`
- Create: `tests/ZeroAlloc.Analyzers.Tests/Verifiers/CSharpAnalyzerVerifier.cs`
- Create: `tests/ZeroAlloc.Analyzers.Tests/Verifiers/CSharpCodeFixVerifier.cs`
- Create: `tests/ZeroAlloc.Analyzers.Tests/TfmHelperTests.cs`

**Step 1: Create the test project**

```bash
dotnet new xunit -n ZeroAlloc.Analyzers.Tests -o tests/ZeroAlloc.Analyzers.Tests -f net8.0
dotnet sln add tests/ZeroAlloc.Analyzers.Tests
```

**Step 2: Add test dependencies to csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Analyzer.Testing" Version="1.1.2" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp.CodeFix.Testing" Version="1.1.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\ZeroAlloc.Analyzers\ZeroAlloc.Analyzers.csproj" />
    <ProjectReference Include="..\..\src\ZeroAlloc.Analyzers.CodeFixes\ZeroAlloc.Analyzers.CodeFixes.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
</Project>
```

**Step 3: Create CSharpAnalyzerVerifier.cs**

This helper reduces boilerplate in analyzer tests.

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace ZeroAlloc.Analyzers.Tests.Verifiers;

public static class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public static DiagnosticResult Diagnostic(string diagnosticId)
        => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);

    public static async Task VerifyAnalyzerAsync(
        string source,
        string targetFramework = "net8.0",
        params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };

        test.TestState.AnalyzerConfigFiles.Add(
            ("/.globalconfig", $"""
                is_global = true
                build_property.TargetFramework = {targetFramework}
                """));

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }

    public static async Task VerifyNoDiagnosticAsync(
        string source,
        string targetFramework = "net8.0")
    {
        await VerifyAnalyzerAsync(source, targetFramework);
    }
}
```

**Step 4: Create CSharpCodeFixVerifier.cs**

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace ZeroAlloc.Analyzers.Tests.Verifiers;

public static class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public static DiagnosticResult Diagnostic(string diagnosticId)
        => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);

    public static async Task VerifyCodeFixAsync(
        string source,
        string fixedSource,
        string diagnosticId,
        string targetFramework = "net8.0")
    {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };

        test.TestState.AnalyzerConfigFiles.Add(
            ("/.globalconfig", $"""
                is_global = true
                build_property.TargetFramework = {targetFramework}
                """));

        test.ExpectedDiagnostics.Add(Diagnostic(diagnosticId));
        await test.RunAsync();
    }
}
```

**Step 5: Create TfmHelperTests.cs**

```csharp
using ZeroAlloc.Analyzers;

namespace ZeroAlloc.Analyzers.Tests;

public class TfmHelperTests
{
    [Theory]
    [InlineData("net8.0", true)]
    [InlineData("net9.0", true)]
    [InlineData("net10.0", true)]
    [InlineData("net7.0", false)]
    [InlineData("net6.0", false)]
    [InlineData("netstandard2.0", false)]
    [InlineData("netcoreapp3.1", false)]
    public void IsNet8OrLater_ReturnsCorrectResult(string tfm, bool expected)
    {
        Assert.Equal(expected, TfmHelper.IsNet8OrLater(tfm));
    }

    [Theory]
    [InlineData("net5.0", true)]
    [InlineData("net6.0", true)]
    [InlineData("net8.0-windows", true)]
    [InlineData("netstandard2.0", false)]
    [InlineData("netcoreapp3.1", false)]
    public void IsNet5OrLater_ReturnsCorrectResult(string tfm, bool expected)
    {
        Assert.Equal(expected, TfmHelper.IsNet5OrLater(tfm));
    }
}
```

**Step 6: Remove the auto-generated test file and verify**

```bash
rm tests/ZeroAlloc.Analyzers.Tests/UnitTest1.cs
dotnet test
```

Expected: All TfmHelper tests pass.

**Step 7: Commit**

```bash
git add tests/
git commit -m "feat: add test project with verifier helpers and TfmHelper tests"
```

---

## Task 5: NP0010 — UseTryGetValue Analyzer (Starter Rule)

Start with the simplest rule to validate the full pipeline (analyzer + code fix + tests).

**Files:**
- Create: `src/ZeroAlloc.Analyzers/Analyzers/UseTryGetValueAnalyzer.cs`
- Create: `src/ZeroAlloc.Analyzers.CodeFixes/UseTryGetValueCodeFixProvider.cs`
- Create: `tests/ZeroAlloc.Analyzers.Tests/NP0010_UseTryGetValueTests.cs`

**Step 1: Write the failing test**

```csharp
using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class NP0010_UseTryGetValueTests
{
    [Fact]
    public async Task ContainsKey_FollowedByIndexer_Reports()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var dict = new Dictionary<string, int>();
                    if ({|#0:dict.ContainsKey("key")|})
                    {
                        var value = dict["key"];
                    }
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseTryGetValueAnalyzer>
            .Diagnostic(DiagnosticIds.UseTryGetValue)
            .WithLocation(0)
            .WithMessage("Use 'TryGetValue' instead of 'ContainsKey' followed by indexer access");

        await CSharpAnalyzerVerifier<UseTryGetValueAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task TryGetValue_AlreadyUsed_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var dict = new Dictionary<string, int>();
                    if (dict.TryGetValue("key", out var value))
                    {
                        _ = value;
                    }
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseTryGetValueAnalyzer>
            .VerifyNoDiagnosticAsync(source);
    }

    [Fact]
    public async Task ContainsKey_WithoutIndexer_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var dict = new Dictionary<string, int>();
                    if (dict.ContainsKey("key"))
                    {
                        System.Console.WriteLine("exists");
                    }
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseTryGetValueAnalyzer>
            .VerifyNoDiagnosticAsync(source);
    }
}
```

**Step 2: Run test to verify it fails**

```bash
dotnet test --filter "NP0010"
```

Expected: FAIL — `UseTryGetValueAnalyzer` not found.

**Step 3: Implement the analyzer**

```csharp
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseTryGetValueAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseTryGetValue,
        "Use TryGetValue instead of ContainsKey + indexer",
        "Use 'TryGetValue' instead of 'ContainsKey' followed by indexer access",
        DiagnosticCategories.Collections,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://github.com/your-org/zeroalloc-analyzers/blob/main/docs/NP0010.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
    }

    private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
    {
        var ifStatement = (IfStatementSyntax)context.Node;

        // Look for: if (dict.ContainsKey(key))
        if (ifStatement.Condition is not InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.Text: "ContainsKey"
                } memberAccess,
                ArgumentList.Arguments.Count: 1
            } containsKeyInvocation)
        {
            return;
        }

        // Verify it's on IDictionary/Dictionary
        var symbolInfo = context.SemanticModel.GetSymbolInfo(containsKeyInvocation);
        if (symbolInfo.Symbol is not IMethodSymbol method)
            return;

        var containingType = method.ContainingType;
        if (containingType == null)
            return;

        // Check if the type implements IDictionary<,> or is Dictionary<,>
        if (!IsDictionaryType(containingType))
            return;

        // Get the key argument
        var keyArg = containsKeyInvocation.ArgumentList.Arguments[0].Expression;

        // Look for dict[key] in the if body
        var dictExpr = memberAccess.Expression;
        if (HasIndexerAccessWithSameKey(ifStatement.Statement, dictExpr, keyArg, context.SemanticModel))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, containsKeyInvocation.GetLocation()));
        }
    }

    private static bool IsDictionaryType(INamedTypeSymbol type)
    {
        if (type.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.Dictionary<TKey, TValue>")
            return true;

        foreach (var iface in type.AllInterfaces)
        {
            if (iface.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IDictionary<TKey, TValue>")
                return true;
        }

        return false;
    }

    private static bool HasIndexerAccessWithSameKey(
        StatementSyntax body,
        ExpressionSyntax dictExpr,
        ExpressionSyntax keyExpr,
        SemanticModel model)
    {
        foreach (var node in body.DescendantNodes())
        {
            if (node is ElementAccessExpressionSyntax elementAccess
                && elementAccess.ArgumentList.Arguments.Count == 1)
            {
                var indexerDictExpr = elementAccess.Expression;
                var indexerKeyExpr = elementAccess.ArgumentList.Arguments[0].Expression;

                // Compare dictionary expression and key expression textually
                if (indexerDictExpr.ToString() == dictExpr.ToString()
                    && indexerKeyExpr.ToString() == keyExpr.ToString())
                {
                    return true;
                }
            }
        }

        return false;
    }
}
```

**Step 4: Run tests to verify they pass**

```bash
dotnet test --filter "NP0010"
```

Expected: 3 tests pass.

**Step 5: Implement the code fix**

```csharp
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZeroAlloc.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseTryGetValueCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.UseTryGetValue];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root == null) return;

        var diagnostic = context.Diagnostics[0];
        var node = root.FindNode(diagnostic.Location.SourceSpan);

        if (node is not InvocationExpressionSyntax invocation)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use TryGetValue",
                ct => ConvertToTryGetValueAsync(context.Document, invocation, ct),
                equivalenceKey: DiagnosticIds.UseTryGetValue),
            diagnostic);
    }

    private static async Task<Document> ConvertToTryGetValueAsync(
        Document document,
        InvocationExpressionSyntax containsKeyInvocation,
        CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct);
        if (root == null) return document;

        var memberAccess = (MemberAccessExpressionSyntax)containsKeyInvocation.Expression;
        var dictExpr = memberAccess.Expression;
        var keyArg = containsKeyInvocation.ArgumentList.Arguments[0];

        // Build: dict.TryGetValue(key, out var value)
        var tryGetValueExpr = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                dictExpr,
                SyntaxFactory.IdentifierName("TryGetValue")),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    keyArg,
                    SyntaxFactory.Argument(
                        null,
                        SyntaxFactory.Token(SyntaxKind.OutKeyword),
                        SyntaxFactory.DeclarationExpression(
                            SyntaxFactory.IdentifierName("var"),
                            SyntaxFactory.SingleVariableDesignation(
                                SyntaxFactory.Identifier("value"))))
                })));

        // Replace the ContainsKey call with TryGetValue
        var newRoot = root.ReplaceNode(containsKeyInvocation, tryGetValueExpr);

        // Replace dict["key"] with value in the if body
        // (simplified — a production version would handle multiple occurrences)
        var ifStatement = containsKeyInvocation.FirstAncestorOrSelf<IfStatementSyntax>();
        if (ifStatement != null)
        {
            // Re-find the if statement in the new tree
            var updatedIf = newRoot.FindNode(ifStatement.Span) as IfStatementSyntax;
            if (updatedIf?.Statement != null)
            {
                var rewriter = new IndexerToValueRewriter(dictExpr.ToString(), keyArg.ToString());
                var newBody = (StatementSyntax)rewriter.Visit(updatedIf.Statement);
                newRoot = newRoot.ReplaceNode(updatedIf.Statement, newBody);
            }
        }

        return document.WithSyntaxRoot(newRoot);
    }

    private sealed class IndexerToValueRewriter(string dictText, string keyText) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            if (node.Expression.ToString() == dictText
                && node.ArgumentList.Arguments.Count == 1
                && node.ArgumentList.Arguments[0].ToString() == keyText)
            {
                return SyntaxFactory.IdentifierName("value")
                    .WithTriviaFrom(node);
            }

            return base.VisitElementAccessExpression(node);
        }
    }
}
```

**Step 6: Add a code fix test**

Add to `NP0010_UseTryGetValueTests.cs`:

```csharp
[Fact]
public async Task ContainsKey_FollowedByIndexer_FixesToTryGetValue()
{
    var source = """
        using System.Collections.Generic;

        class C
        {
            void M()
            {
                var dict = new Dictionary<string, int>();
                if ({|#0:dict.ContainsKey("key")|})
                {
                    var v = dict["key"];
                }
            }
        }
        """;

    var fixedSource = """
        using System.Collections.Generic;

        class C
        {
            void M()
            {
                var dict = new Dictionary<string, int>();
                if (dict.TryGetValue("key", out var value))
                {
                    var v = value;
                }
            }
        }
        """;

    await CSharpCodeFixVerifier<UseTryGetValueAnalyzer, UseTryGetValueCodeFixProvider>
        .VerifyCodeFixAsync(source, fixedSource, DiagnosticIds.UseTryGetValue);
}
```

**Step 7: Run all tests**

```bash
dotnet test
```

Expected: All pass.

**Step 8: Commit**

```bash
git add -A
git commit -m "feat: add NP0010 UseTryGetValue analyzer and code fix"
```

---

## Task 6: NP0001 — UseFrozenDictionary Analyzer (TFM-Gated)

**Files:**
- Create: `src/ZeroAlloc.Analyzers/Analyzers/UseFrozenDictionaryAnalyzer.cs`
- Create: `src/ZeroAlloc.Analyzers.CodeFixes/UseFrozenDictionaryCodeFixProvider.cs`
- Create: `tests/ZeroAlloc.Analyzers.Tests/NP0001_UseFrozenDictionaryTests.cs`

**Step 1: Write the failing tests**

```csharp
using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class NP0001_UseFrozenDictionaryTests
{
    [Fact]
    public async Task ReadonlyDictField_InitializedInConstructor_Reports()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                private readonly Dictionary<string, int> {|#0:_lookup|};

                C()
                {
                    _lookup = new Dictionary<string, int>
                    {
                        ["a"] = 1,
                        ["b"] = 2
                    };
                }

                int Get(string key) => _lookup[key];
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseFrozenDictionaryAnalyzer>
            .Diagnostic(DiagnosticIds.UseFrozenDictionary)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<UseFrozenDictionaryAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task ReadonlyDictField_OnNet6_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                private readonly Dictionary<string, int> _lookup = new()
                {
                    ["a"] = 1
                };

                int Get(string key) => _lookup[key];
            }
            """;

        // FrozenDictionary requires net8.0+
        await CSharpAnalyzerVerifier<UseFrozenDictionaryAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net6.0");
    }

    [Fact]
    public async Task MutableDict_WithAddCalls_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                private readonly Dictionary<string, int> _lookup = new();

                void AddItem(string key, int value) => _lookup.Add(key, value);
            }
            """;

        await CSharpAnalyzerVerifier<UseFrozenDictionaryAnalyzer>
            .VerifyNoDiagnosticAsync(source);
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test --filter "NP0001"
```

**Step 3: Implement the analyzer**

The analyzer checks for:
1. `readonly` fields of type `Dictionary<,>` or `HashSet<>`
2. No mutation calls (`Add`, `Remove`, `Clear`, `[]=`) outside constructor
3. TFM is net8.0+

```csharp
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
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
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

        // Must target net8.0+
        if (!TfmHelper.TryGetTfm(context.Options, out var tfm) || !TfmHelper.IsNet8OrLater(tfm))
            return;

        // Check if the field is mutated outside constructors
        var containingType = field.ContainingType;
        foreach (var syntaxRef in containingType.DeclaringSyntaxReferences)
        {
            var typeSyntax = syntaxRef.GetSyntax(context.CancellationToken);
            var model = context.Compilation.GetSemanticModel(typeSyntax.SyntaxTree);

            foreach (var invocation in typeSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess
                    && MutatingMethods.Contains(memberAccess.Name.Identifier.Text))
                {
                    var targetSymbol = model.GetSymbolInfo(memberAccess.Expression).Symbol;
                    if (SymbolEqualityComparer.Default.Equals(targetSymbol, field))
                    {
                        // Check if we're inside a constructor
                        var enclosingMethod = invocation.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
                        if (enclosingMethod == null)
                            return; // Mutated outside constructor — not a candidate
                    }
                }
            }

            // Also check for indexer assignment: dict["key"] = value (outside constructor)
            foreach (var assignment in typeSyntax.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                if (assignment.Left is ElementAccessExpressionSyntax elementAccess)
                {
                    var targetSymbol = model.GetSymbolInfo(elementAccess.Expression).Symbol;
                    if (SymbolEqualityComparer.Default.Equals(targetSymbol, field))
                    {
                        var enclosingCtor = assignment.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
                        if (enclosingCtor == null)
                            return; // Mutated outside constructor
                    }
                }
            }
        }

        // Report on the field declaration
        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            field.Locations[0],
            field.Name));
    }
}
```

**Step 4: Run tests**

```bash
dotnet test --filter "NP0001"
```

Expected: All pass.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: add NP0001 UseFrozenDictionary analyzer (net8.0+ gated)"
```

---

## Task 7: NP0003 — UseCollectionsMarshalAsSpan Analyzer (TFM-Gated)

**Files:**
- Create: `src/ZeroAlloc.Analyzers/Analyzers/UseCollectionsMarshalAsSpanAnalyzer.cs`
- Create: `tests/ZeroAlloc.Analyzers.Tests/NP0003_UseCollectionsMarshalAsSpanTests.cs`

**Step 1: Write the failing tests**

```csharp
using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class NP0003_UseCollectionsMarshalAsSpanTests
{
    [Fact]
    public async Task ForeachOverList_Reports()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var list = new List<int> { 1, 2, 3 };
                    {|#0:foreach|} (var item in list)
                    {
                        _ = item;
                    }
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<UseCollectionsMarshalAsSpanAnalyzer>
            .Diagnostic(DiagnosticIds.UseCollectionsMarshalAsSpan)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<UseCollectionsMarshalAsSpanAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task ForeachOverList_OnNet48_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var list = new List<int> { 1, 2, 3 };
                    foreach (var item in list)
                    {
                        _ = item;
                    }
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseCollectionsMarshalAsSpanAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net48");
    }

    [Fact]
    public async Task ForeachOverArray_NoDiagnostic()
    {
        var source = """
            class C
            {
                void M()
                {
                    var arr = new int[] { 1, 2, 3 };
                    foreach (var item in arr)
                    {
                        _ = item;
                    }
                }
            }
            """;

        await CSharpAnalyzerVerifier<UseCollectionsMarshalAsSpanAnalyzer>
            .VerifyNoDiagnosticAsync(source);
    }

    [Fact]
    public async Task ForeachOverList_WithAwait_NoDiagnostic()
    {
        var source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class C
            {
                async Task M()
                {
                    var list = new List<int> { 1, 2, 3 };
                    foreach (var item in list)
                    {
                        await Task.Delay(1);
                    }
                }
            }
            """;

        // Span can't cross await boundaries (CS4007)
        await CSharpAnalyzerVerifier<UseCollectionsMarshalAsSpanAnalyzer>
            .VerifyNoDiagnosticAsync(source);
    }
}
```

**Step 2: Implement the analyzer**

```csharp
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseCollectionsMarshalAsSpanAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseCollectionsMarshalAsSpan,
        "Use CollectionsMarshal.AsSpan for List<T> iteration",
        "Use 'CollectionsMarshal.AsSpan()' to iterate List<T> without enumerator allocation",
        DiagnosticCategories.Collections,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeForEach, SyntaxKind.ForEachStatement);
    }

    private static void AnalyzeForEach(SyntaxNodeAnalysisContext context)
    {
        var forEach = (ForEachStatementSyntax)context.Node;

        // Check TFM: CollectionsMarshal.AsSpan requires net5.0+
        if (!TfmHelper.TryGetTfm(context.Options, out var tfm) || !TfmHelper.IsNet5OrLater(tfm))
            return;

        // Get the type of the collection being iterated
        var typeInfo = context.SemanticModel.GetTypeInfo(forEach.Expression);
        if (typeInfo.Type is not INamedTypeSymbol namedType)
            return;

        // Must be List<T>
        if (namedType.OriginalDefinition.ToDisplayString() != "System.Collections.Generic.List<T>")
            return;

        // Check for await expressions in the loop body — Span can't cross await boundaries
        if (forEach.Statement.DescendantNodes().OfType<AwaitExpressionSyntax>().Any())
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, forEach.ForEachKeyword.GetLocation()));
    }
}
```

**Step 3: Run tests**

```bash
dotnet test --filter "NP0003"
```

Expected: All pass.

**Step 4: Commit**

```bash
git add -A
git commit -m "feat: add NP0003 UseCollectionsMarshalAsSpan analyzer (net5.0+ gated)"
```

---

## Task 8: NP0005 — AvoidStringConcatInLoop Analyzer

**Files:**
- Create: `src/ZeroAlloc.Analyzers/Analyzers/AvoidStringConcatInLoopAnalyzer.cs`
- Create: `tests/ZeroAlloc.Analyzers.Tests/NP0005_AvoidStringConcatInLoopTests.cs`

**Step 1: Write the failing tests**

```csharp
using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class NP0005_AvoidStringConcatInLoopTests
{
    [Fact]
    public async Task StringConcatInForLoop_Reports()
    {
        var source = """
            class C
            {
                string M()
                {
                    var result = "";
                    for (int i = 0; i < 10; i++)
                    {
                        {|#0:result += i.ToString()|};
                    }
                    return result;
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidStringConcatInLoopAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidStringConcatInLoop)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<AvoidStringConcatInLoopAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task StringConcatInForeachLoop_Reports()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                string M(List<string> items)
                {
                    var result = "";
                    foreach (var item in items)
                    {
                        {|#0:result += item|};
                    }
                    return result;
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidStringConcatInLoopAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidStringConcatInLoop)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<AvoidStringConcatInLoopAnalyzer>
            .VerifyAnalyzerAsync(source, "net8.0", expected);
    }

    [Fact]
    public async Task StringConcatOutsideLoop_NoDiagnostic()
    {
        var source = """
            class C
            {
                string M()
                {
                    var result = "hello" + " " + "world";
                    return result;
                }
            }
            """;

        await CSharpAnalyzerVerifier<AvoidStringConcatInLoopAnalyzer>
            .VerifyNoDiagnosticAsync(source);
    }
}
```

**Step 2: Implement the analyzer**

```csharp
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidStringConcatInLoopAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.AvoidStringConcatInLoop,
        "Avoid string concatenation in loops",
        "String concatenation in a loop causes repeated allocations — use StringBuilder",
        DiagnosticCategories.Strings,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAssignment,
            SyntaxKind.AddAssignmentExpression);
    }

    private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
    {
        var assignment = (AssignmentExpressionSyntax)context.Node;

        // Check if left side is string type
        var typeInfo = context.SemanticModel.GetTypeInfo(assignment.Left);
        if (typeInfo.Type?.SpecialType != SpecialType.System_String)
            return;

        // Check if inside a loop
        if (!IsInsideLoop(assignment))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, assignment.GetLocation()));
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
```

**Step 3: Run tests**

```bash
dotnet test --filter "NP0005"
```

Expected: All pass.

**Step 4: Commit**

```bash
git add -A
git commit -m "feat: add NP0005 AvoidStringConcatInLoop analyzer"
```

---

## Task 9: NP0008 — AvoidEnumHasFlag Analyzer

**Files:**
- Create: `src/ZeroAlloc.Analyzers/Analyzers/AvoidEnumHasFlagAnalyzer.cs`
- Create: `tests/ZeroAlloc.Analyzers.Tests/NP0008_AvoidEnumHasFlagTests.cs`

**Step 1: Write the failing tests**

```csharp
using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class NP0008_AvoidEnumHasFlagTests
{
    [Fact]
    public async Task EnumHasFlag_Reports()
    {
        var source = """
            using System;

            [Flags]
            enum Options { None = 0, A = 1, B = 2 }

            class C
            {
                bool M(Options o) => {|#0:o.HasFlag(Options.A)|};
            }
            """;

        var expected = CSharpAnalyzerVerifier<AvoidEnumHasFlagAnalyzer>
            .Diagnostic(DiagnosticIds.AvoidEnumHasFlag)
            .WithLocation(0);

        // Only report on < net7.0 (net7.0+ HasFlag is JIT-intrinsic, no boxing)
        await CSharpAnalyzerVerifier<AvoidEnumHasFlagAnalyzer>
            .VerifyAnalyzerAsync(source, "net6.0", expected);
    }

    [Fact]
    public async Task EnumHasFlag_OnNet7_NoDiagnostic()
    {
        var source = """
            using System;

            [Flags]
            enum Options { None = 0, A = 1, B = 2 }

            class C
            {
                bool M(Options o) => o.HasFlag(Options.A);
            }
            """;

        // net7.0+ JIT inlines HasFlag — no boxing
        await CSharpAnalyzerVerifier<AvoidEnumHasFlagAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net7.0");
    }

    [Fact]
    public async Task BitwiseCheck_NoDiagnostic()
    {
        var source = """
            using System;

            [Flags]
            enum Options { None = 0, A = 1, B = 2 }

            class C
            {
                bool M(Options o) => (o & Options.A) != 0;
            }
            """;

        await CSharpAnalyzerVerifier<AvoidEnumHasFlagAnalyzer>
            .VerifyNoDiagnosticAsync(source, "net6.0");
    }
}
```

**Step 2: Implement the analyzer**

```csharp
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroAlloc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidEnumHasFlagAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.AvoidEnumHasFlag,
        "Avoid Enum.HasFlag — use bitwise check",
        "Enum.HasFlag boxes the argument on older runtimes — use '(value & flag) != 0' instead",
        DiagnosticCategories.Collections,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax
            {
                Name.Identifier.Text: "HasFlag"
            })
        {
            return;
        }

        // Verify it's Enum.HasFlag
        var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol;
        if (symbol is not IMethodSymbol method
            || method.ContainingType.SpecialType != SpecialType.System_Enum)
        {
            return;
        }

        // On net7.0+, HasFlag is JIT-intrinsic — no boxing, no diagnostic
        if (TfmHelper.TryGetTfm(context.Options, out var tfm) && TfmHelper.IsNetOrLater(tfm, 7))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
    }
}
```

**Step 3: Run tests**

```bash
dotnet test --filter "NP0008"
```

Expected: All pass.

**Step 4: Commit**

```bash
git add -A
git commit -m "feat: add NP0008 AvoidEnumHasFlag analyzer (suppressed on net7.0+)"
```

---

## Task 10: Remaining Analyzers (NP0002, NP0004, NP0006, NP0007, NP0009)

Follow the same TDD pattern for each remaining rule. Each gets its own analyzer class, test class, and (where applicable) code fix provider. Key implementation notes:

**NP0002 (UseFrozenSet):** Mirror NP0001 but detect `HashSet<T>`. Same TFM gate (net8.0+), same mutation detection.

**NP0004 (UseSearchValues):** Detect `new[] { "a", "b" }.Contains(x)` or long `if (x == "a" || x == "b" || ...)` chains (threshold: 4+ comparisons). Suggest `SearchValues<char>` for char patterns, `FrozenSet<string>` for string patterns. Gate: net8.0+.

**NP0006 (UseStackalloc):** Detect `new byte[n]` / `new char[n]` where `n` is a constant <= 256 and the array doesn't escape the method. Suggest `stackalloc byte[n]` or `Span<byte> span = stackalloc byte[n]`. No TFM gate (works on all supported TFMs with C# 7.2+).

**NP0007 (UseArrayPool):** Detect `new byte[n]` where `n` is a variable or constant > 256. Suggest `ArrayPool<byte>.Shared.Rent(n)`. No TFM gate.

**NP0009 (AvoidStringReplaceChain):** Detect 3+ chained `.Replace()` calls on a string. Suggest `StringBuilder`. No TFM gate.

Each follows the same commit pattern: tests first, implementation, verify, commit.

---

## Task 11: NuGet Package Project

**Files:**
- Create: `src/ZeroAlloc.Analyzers.Package/ZeroAlloc.Analyzers.Package.csproj`
- Create: `src/ZeroAlloc.Analyzers.Package/buildTransitive/ZeroAlloc.Analyzers.props`

**Step 1: Create the packaging project**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <IsPackable>true</IsPackable>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <SuppressDependenciesWhenPacking>true</SuppressDependenciesWhenPacking>
    <DevelopmentDependency>true</DevelopmentDependency>

    <PackageId>ZeroAlloc.Analyzers</PackageId>
    <Title>ZeroAlloc Analyzers</Title>
    <Description>Roslyn analyzers for modern .NET performance patterns with multi-TFM awareness</Description>
    <Authors>Marcel Roozekrans</Authors>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageTags>analyzers;roslyn;performance;dotnet;frozen-dictionary;stackalloc;array-pool</PackageTags>
    <RepositoryUrl>https://github.com/MarcelRoozekrans/zeroalloc-analyzers</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <NoPackageAnalysis>true</NoPackageAnalysis>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\ZeroAlloc.Analyzers\ZeroAlloc.Analyzers.csproj" />
    <ProjectReference Include="..\ZeroAlloc.Analyzers.CodeFixes\ZeroAlloc.Analyzers.CodeFixes.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="\" />
  </ItemGroup>

  <!-- Pack analyzer DLLs into analyzers/dotnet/cs/ -->
  <Target Name="_AddAnalyzersToOutput" AfterTargets="Build">
    <ItemGroup>
      <_AnalyzerFile Include="$(OutputPath)\ZeroAlloc.Analyzers.dll" />
      <_AnalyzerFile Include="$(OutputPath)\ZeroAlloc.Analyzers.CodeFixes.dll" />
    </ItemGroup>
  </Target>

  <ItemGroup>
    <None Include="$(OutputPath)\ZeroAlloc.Analyzers.dll"
          Pack="true"
          PackagePath="analyzers/dotnet/cs"
          Visible="false" />
    <None Include="$(OutputPath)\ZeroAlloc.Analyzers.CodeFixes.dll"
          Pack="true"
          PackagePath="analyzers/dotnet/cs"
          Visible="false" />
  </ItemGroup>

  <!-- Include .props for CompilerVisibleProperty -->
  <ItemGroup>
    <None Include="buildTransitive\ZeroAlloc.Analyzers.props"
          Pack="true"
          PackagePath="buildTransitive\" />
    <None Include="buildTransitive\ZeroAlloc.Analyzers.props"
          Pack="true"
          PackagePath="build\" />
  </ItemGroup>
</Project>
```

**Step 2: Create the .props file**

```xml
<Project>
  <ItemGroup>
    <CompilerVisibleProperty Include="TargetFramework" />
  </ItemGroup>
</Project>
```

**Step 3: Build the package**

```bash
dotnet pack src/ZeroAlloc.Analyzers.Package -c Release
```

Expected: `ZeroAlloc.Analyzers.1.0.0.nupkg` created in `bin/Release`.

**Step 4: Verify package contents**

```bash
dotnet nuget verify src/ZeroAlloc.Analyzers.Package/bin/Release/ZeroAlloc.Analyzers.*.nupkg || true
# Or inspect with:
unzip -l src/ZeroAlloc.Analyzers.Package/bin/Release/ZeroAlloc.Analyzers.*.nupkg
```

Should contain:
- `analyzers/dotnet/cs/ZeroAlloc.Analyzers.dll`
- `analyzers/dotnet/cs/ZeroAlloc.Analyzers.CodeFixes.dll`
- `buildTransitive/ZeroAlloc.Analyzers.props`
- `build/ZeroAlloc.Analyzers.props`

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: add NuGet packaging project with TFM props"
```

---

## Task 12: Integration Test — Dogfood on roslyn-codegraph-mcp

**Step 1: Add the local package to roslyn-codegraph-mcp**

In roslyn-codegraph-mcp's `NuGet.config` or `Directory.Build.props`, add a local feed pointing to the package output directory, and add the `ZeroAlloc.Analyzers` package reference.

**Step 2: Build roslyn-codegraph-mcp and check for NP* diagnostics**

```bash
cd ../roslyn-codegraph-mcp
dotnet build 2>&1 | grep "NP0"
```

Verify that diagnostics are reported for known patterns (e.g., `CollectionsMarshal.AsSpan` suggestions already fixed should not trigger, but any remaining patterns should).

**Step 3: Fix any false positives found during dogfooding**

Go back to zeroalloc-analyzers repo and fix.

**Step 4: Commit fixes**

```bash
git add -A
git commit -m "fix: address false positives from dogfooding"
```

---

## Task 13: Documentation & CI

**Step 1: Write rule documentation**

Create `docs/rules/NP0001.md` through `docs/rules/NP0010.md` with:
- Rule description
- Violation example
- Fix example
- When to suppress
- Minimum TFM

**Step 2: Update README.md**

Add rule table, installation instructions, and TFM compatibility matrix.

**Step 3: Add GitHub Actions CI**

Create `.github/workflows/ci.yml`:
- Build on ubuntu-latest
- Run tests
- Pack NuGet
- Upload artifact

**Step 4: Commit**

```bash
git add -A
git commit -m "docs: add rule documentation, README, and CI workflow"
```

---

Plan complete and saved to `docs/plans/2026-03-08-zeroalloc-analyzers-implementation.md`.

**Two execution options:**

**1. Subagent-Driven (this session)** — I dispatch fresh subagent per task, review between tasks, fast iteration

**2. Parallel Session (separate)** — Open new session in the new repo with executing-plans, batch execution with checkpoints

Which approach?