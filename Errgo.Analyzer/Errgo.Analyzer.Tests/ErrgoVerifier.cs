using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Errgo.Analyzer.Tests;

public static partial class ErrgoVerifier
{
    /*
     * Used for verifying that the given source code produces the specified diagnostics.
     */
    public static Task VerifyAnalyzerAsync(string testCode, params DiagnosticResult[] expected)
    {
        var test = new AnalyzerTest
        {
            TestCode = testCode
        };

        test.TestState.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    /*
     * Used for verifying that the given source code produces and applies a code fix for the specified diagnostic.
     */
    public static Task VerifyCodeFixAsync(string testCode, string fixedCode, params DiagnosticResult[] expected)
    {
        var test = new CodeFixTest
        {
            TestCode = testCode,
            FixedCode = fixedCode,
        };

        test.TestState.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    internal sealed class AnalyzerTest : CSharpAnalyzerTest<ErrgoAnalyzer, DefaultVerifier>
    {
        public AnalyzerTest()
        {
            this.TestState.AdditionalReferences.Add(typeof(Error).Assembly);
        }
    }

    internal sealed class CodeFixTest : CSharpCodeFixTest<ErrgoAnalyzer, ErrgoAnalyzerCodeFixProvider, DefaultVerifier>
    {
        public CodeFixTest()
        {
            this.TestState.AdditionalReferences.Add(typeof(Error).Assembly);
        }
    }
}