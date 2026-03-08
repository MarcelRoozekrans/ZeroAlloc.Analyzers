using ZeroAlloc.Analyzers.Tests.Verifiers;

namespace ZeroAlloc.Analyzers.Tests;

public class ZA0101_UseFrozenDictionaryTests
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
            .WithLocation(0)
            .WithArguments("_lookup");

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
