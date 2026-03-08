# ZeroAlloc Analyzers — Design Document

**Date:** 2026-03-08
**Status:** Approved

## Overview

A standalone open-source NuGet Roslyn analyzer package focused on modern .NET performance patterns that existing analyzers (Meziantou, Roslynator, ErrorProne.NET, NetFabric.Hyperlinq) miss. Multi-framework targeting suggests fixes based on the consumer's target framework.

## Architecture: Single Assembly + Runtime TFM Detection

One analyzer DLL ships in the NuGet package under `analyzers/dotnet/cs/`. Each diagnostic checks the consuming project's `TargetFramework` via `CompilerVisibleProperty` and conditionally reports diagnostics. This is the standard pattern used by Meziantou and Roslynator.

## Project Structure

```
src/
  ZeroAlloc.Analyzers/           # Analyzer assembly (netstandard2.0)
  ZeroAlloc.Analyzers.CodeFixes/ # Code fix providers (netstandard2.0)
  ZeroAlloc.Analyzers.Package/   # NuGet packaging project
  ZeroAlloc.Analyzers.Tests/     # Unit tests (net8.0)
```

- Analyzers target **netstandard2.0** (Roslyn host requirement).
- Tests target net8.0 and use `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing`.
- The `.Package` project produces the NuGet with the correct `analyzers/dotnet/cs/` layout.

## TFM Detection

A bundled `.props` file flows the target framework into analyzer options:

```xml
<CompilerVisibleProperty Include="TargetFramework" />
```

Each analyzer reads it via:

```csharp
context.Options.AnalyzerConfigOptionsProvider
    .GlobalOptions.TryGetValue("build_property.TargetFramework", out var tfm);
```

A shared `TfmHelper` class parses the TFM string and exposes `IsNet8OrLater`, `IsNet6OrLater`, `IsNet5OrLater`, etc.

## Diagnostic Rules (Initial Set)

| ID | Category | Pattern | Min TFM | Severity |
|---|---|---|---|---|
| **NP0001** | Collections | `new Dictionary<K,V>` populated once, never mutated -> `FrozenDictionary` | net8.0 | Info |
| **NP0002** | Collections | `new HashSet<T>` populated once, never mutated -> `FrozenSet` | net8.0 | Info |
| **NP0003** | Collections | `foreach` over `List<T>` -> `CollectionsMarshal.AsSpan()` | net5.0 | Info |
| **NP0004** | Strings | Array `.Contains(s)` or chained `\|\|` comparisons -> `SearchValues<char>` / `FrozenSet<string>` | net8.0 | Info |
| **NP0005** | Strings | String concatenation in loops -> `string.Create` / `StringBuilder` | any | Warning |
| **NP0006** | Memory | Small `new byte[n]` (n <= 256) -> `stackalloc` | any | Info |
| **NP0007** | Memory | Large `new byte[n]` in method scope -> `ArrayPool<T>` | any | Info |
| **NP0008** | Collections | `Enum.HasFlag` with boxing -> bitwise `&` check | any | Info |
| **NP0009** | Strings | `string.Replace` chain -> `StringBuilder` / `string.Create` | any | Info |
| **NP0010** | Collections | `dict.ContainsKey` + `dict[key]` -> `TryGetValue` | any | Warning |

### Detection Heuristics for NP0001/NP0002

"Populated once, never mutated" detection:
- Field/property assigned in constructor or field initializer only
- No calls to `Add`, `Remove`, `Clear`, indexer-set after initialization
- `readonly` field is a strong positive signal

## Code Fix Strategy

Every diagnostic gets a code fix provider that:
- Adds the required `using` statement
- Transforms the code (e.g., wraps initialization with `.ToFrozenDictionary()`)
- For TFM-gated rules, the fix is only offered when the TFM supports it (same `TfmHelper` check)
- Uses `DocumentEditor` for multi-edit transforms and `SyntaxGenerator` for cross-version compatibility

## Testing Approach

- Use `Microsoft.CodeAnalysis.Testing` (`CSharpAnalyzerTest` / `CSharpCodeFixTest`)
- Each rule gets tests for: positive detection, negative (no false positive), code fix verification
- TFM-specific tests verify rules are suppressed on older TFMs by setting `build_property.TargetFramework` in `AnalyzerConfigOptions`

## Packaging

The `.Package` project:
- `IsPackable=true`, `IncludeBuildOutput=false`, `SuppressDependenciesWhenPacking=true`
- Embeds analyzer + code fix DLLs in `analyzers/dotnet/cs/`
- Includes the `.props` file in `buildTransitive/` for `CompilerVisibleProperty`
- No runtime dependencies — everything is self-contained

## Non-Goals

- Duplicating rules from Meziantou, Roslynator, ErrorProne.NET, or NetFabric.Hyperlinq
- Runtime/IL-level analysis (source analysis only)
- Per-TFM analyzer assemblies (single assembly handles all TFMs)
