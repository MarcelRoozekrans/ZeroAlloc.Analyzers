# Contributing to ZeroAlloc Analyzers

## Conventional Commits

This project uses [conventional commits](https://www.conventionalcommits.org/). All commit messages must follow this format:

```
<type>(<scope>): <description>

[optional body]
```

### Types

| Type | Description |
|------|-------------|
| `feat` | New analyzer rule or feature |
| `fix` | Bug fix (false positive/negative) |
| `perf` | Performance improvement to an analyzer |
| `docs` | Documentation changes |
| `test` | Adding or updating tests |
| `refactor` | Code refactoring (no behavior change) |
| `ci` | CI/CD pipeline changes |
| `chore` | Maintenance tasks |

### Examples

```
feat(ZA0107): add pre-size collections analyzer
fix(ZA0603): handle arrays in Count() detection
docs(ZA1401): add rule documentation for static lambda
test(ZA0901): add test for nested class hierarchies
ci: add NuGet publish workflow
```

## Adding a New Rule

1. **Choose an ID** in the appropriate category range (see `DiagnosticIds.cs`)
2. **Create the analyzer** in `src/ZeroAlloc.Analyzers/Analyzers/`
3. **Add the ID** to `DiagnosticIds.cs`
4. **Add the category** to `DiagnosticCategories.cs` (if new)
5. **Register in** `AnalyzerReleases.Unshipped.md`
6. **Write tests** in `tests/ZeroAlloc.Analyzers.Tests/`
7. **Write docs** in `docs/rules/ZA####.md`
8. **Update** `README.md` rule table

### Analyzer Conventions

- Target `netstandard2.0` (no C# 8+ index/range syntax)
- Use `TfmHelper` for TFM-gated rules
- Use `RegisterCompilationStartAction` for TFM checks (avoids RS1030)
- Severity: `Warning` for likely bugs, `Info` for suggestions
- Always set `isEnabledByDefault: true` unless the rule is opt-in

### Testing

```bash
dotnet test
```

Tests use `Microsoft.CodeAnalysis.Testing` with the `{|#0:...|}`/`WithLocation(0)` markup pattern.

## Releasing

Releases are triggered by pushing a git tag:

```bash
git tag v1.0.0
git push origin v1.0.0
```

This triggers the release workflow which builds, tests, publishes to NuGet, and creates a GitHub Release with auto-generated notes from conventional commits.
