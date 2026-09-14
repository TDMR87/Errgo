using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace Errgo.Analyzer.Tests;

public class ErrgoAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_WhenErrorIsCheckedInNextStatement()
    {
        await ErrgoVerifier.VerifyAnalyzerAsync(/* lang=c#-test */
            """
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    Error GetError() => new Error("test");
            
                    void TestMethod()
                    {
                        var err = GetError();
                        if (err)
                        {
                            
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task Diagnostic_WhenErrorIsNotChecked()
    {
        await ErrgoVerifier.VerifyAnalyzerAsync(/* lang=c#-test */
            """
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    Error GetError() => new Error("test");

                    void TestMethod()
                    {
                        var err = GetError();
                        System.Console.WriteLine("Lorem ipsum");
                    }
                }
            }
            """, 
            expected: new DiagnosticResult(
                ErrgoAnalyzer.DiagnosticId, 
                DiagnosticSeverity.Warning)
            .WithSpan(
                startLine: 11, 
                startColumn: 17, 
                endLine: 11, 
                endColumn: 20)
            .WithArguments("err"));
    }

    [Fact]
    public async Task Diagnostic_WhenErrorIsNotChecked_Tuple()
    {
        await ErrgoVerifier.VerifyAnalyzerAsync(/* lang=c#-test */
            """
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    (string, Error) GetStringOrError() => (string.Empty, new Error("test"));

                    void TestMethod()
                    {
                        (var str, var err) = GetStringOrError();
                        System.Console.WriteLine("Lorem ipsum");
                    }
                }
            }
            """,
            expected: new DiagnosticResult(
                ErrgoAnalyzer.DiagnosticId, 
                DiagnosticSeverity.Warning)
            .WithSpan(
                startLine: 11,
                startColumn: 27,
                endLine: 11,
                endColumn: 30)
            .WithArguments("err"));
    }

    [Fact]
    public async Task Diagnostic_WhenErrorIsNotChecked_Tuple2()
    {
        await ErrgoVerifier.VerifyAnalyzerAsync(/* lang=c#-test */
            """
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    (string, Error) GetStringOrError() => (string.Empty, new Error("test"));

                    void TestMethod()
                    {
                        var (str, err) = GetStringOrError();
                        System.Console.WriteLine("Lorem ipsum");
                    }
                }
            }
            """,
            expected: new DiagnosticResult(
                ErrgoAnalyzer.DiagnosticId, 
                DiagnosticSeverity.Warning)
            .WithSpan(
                startLine: 11, 
                startColumn: 23, 
                endLine: 11, 
                endColumn: 26)
            .WithArguments("err"));
    }

    [Fact]
    public async Task Diagnostic_WhenErrorIsNotChecked_Tuple3()
    {
        await ErrgoVerifier.VerifyAnalyzerAsync(/* lang=c#-test */
            """
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    (string, Error) GetStringOrError() => (string.Empty, new Error("test"));

                    void TestMethod()
                    {
                        var (str, err) = GetStringOrError();
                        if (err)
                        {
                            // Handle error
                        }

                        (str, err) = GetStringOrError();
                        System.Console.WriteLine("Lorem ipsum");
                    }
                }
            }
            """,
            expected: new DiagnosticResult(
                ErrgoAnalyzer.DiagnosticId, 
                DiagnosticSeverity.Warning)
            .WithSpan(
                startLine: 17, 
                startColumn: 19, 
                endLine: 17, 
                endColumn: 22)
            .WithArguments("err"));
    }

    [Fact]
    public async Task Diagnostic_WhenErrorIsCheckedButNotInNextStatement()
    {
        await ErrgoVerifier.VerifyAnalyzerAsync(/* lang=c#-test */
            """
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    Error GetError() => new Error("test");

                    void TestMethod()
                    {
                        var err = GetError();
                        System.Console.WriteLine("Lorem ipsum");
                        if (err)
                        {
                            // Check is too late
                        }
                    }
                }
            }
            """,
            expected: new DiagnosticResult(
                ErrgoAnalyzer.DiagnosticId, 
                DiagnosticSeverity.Warning)
            .WithSpan(
                startLine: 11, 
                startColumn: 17, 
                endLine: 11, 
                endColumn: 20)
            .WithArguments("err"));
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsCheckedWithNegation()
    {
        await ErrgoVerifier.VerifyAnalyzerAsync(/* lang=c#-test */
            """
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    Error GetError() => new Error("test");

                    void TestMethod()
                    {
                        var err = GetError();
                        if (!err)
                        {
                            // Success path
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsCheckedWithComparison()
    {
        await ErrgoVerifier.VerifyAnalyzerAsync(/* lang=c#-test */
            """
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    Error GetError() => new Error("test");

                    void TestMethod()
                    {
                        var err = GetError();
                        if (err == Error.None)
                        {
                            // Success
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsCheckedWithNotEqual()
    {
        await ErrgoVerifier.VerifyAnalyzerAsync(/* lang=c#-test */
            """
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    Error GetError() => new Error("test");

                    void TestMethod()
                    {
                        var err = GetError();
                        if (err != Error.None)
                        {
                            // Error path
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsNotFromMethodCall()
    {
        await ErrgoVerifier.VerifyAnalyzerAsync(/* lang=c#-test */
            """
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    void TestMethod()
                    {
                        var err = new Error("manual error");
                        System.Console.WriteLine("Lorem ipsum");
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorNone()
    {
        await ErrgoVerifier.VerifyAnalyzerAsync(/* lang=c#-test */
            """
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    void TestMethod()
                    {
                        var err = Error.None;
                        System.Console.WriteLine("Lorem ipsum");
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorEmpty()
    {
        await ErrgoVerifier.VerifyAnalyzerAsync(/* lang=c#-test */
            """
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    void TestMethod()
                    {
                        var err = Error.Empty;
                        System.Console.WriteLine("Lorem ipsum");
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsLastStatementInVoidMethod()
    {
        await ErrgoVerifier.VerifyAnalyzerAsync(/* lang=c#-test */
            """
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    Error GetError() => new Error("test");

                    void TestMethod()
                    {
                        var err = GetError();
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task Diagnostic_WithMultipleErrors_AllUnchecked()
    {
        await ErrgoVerifier.VerifyAnalyzerAsync(/* lang=c#-test */
            """
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    Error GetError() => new Error("test");

                    void TestMethod()
                    {
                        var err1 = GetError();
                        System.Console.WriteLine("Lorem");
                        
                        var err2 = GetError();
                        System.Console.WriteLine("Ipsum");
                    }
                }
            }
            """,
            new DiagnosticResult(
                ErrgoAnalyzer.DiagnosticId, 
                DiagnosticSeverity.Warning)
                .WithSpan(
                    startLine: 11, 
                    startColumn: 17, 
                    endLine: 11, 
                    endColumn: 21)
                .WithArguments("err1"),
            new DiagnosticResult(
                ErrgoAnalyzer.DiagnosticId, 
                DiagnosticSeverity.Warning)
                .WithSpan(
                    startLine: 14, 
                    startColumn: 17, 
                    endLine: 14, 
                    endColumn: 21)
                .WithArguments("err2"));
    }

    [Fact]
    public async Task NoDiagnostic_WhenChecked_WithExplicitErrorType()
    {
        await ErrgoVerifier.VerifyAnalyzerAsync(/* lang=c#-test */
            """
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    Error GetError() => new Error("test");

                    void TestMethod()
                    {
                        Error err = GetError();
                        if (err)
                        {
                            // Handle error
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task NoDiagnostic_WhenVariableHasNoInitializer()
    {
        await ErrgoVerifier.VerifyAnalyzerAsync(/* lang=c#-test */
            """
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    void TestMethod()
                    {
                        Error err;
                        System.Console.WriteLine("Lorem ipsum");
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorInComplexCondition()
    {
        await ErrgoVerifier.VerifyAnalyzerAsync(/* lang=c#-test */
            """
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    Error GetError() => new Error("test");

                    void TestMethod()
                    {
                        var err = GetError();
                        if (err || true)
                        {
                            // Error is mentioned
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task Diagnostic_WhenAwaitedErrorIsNotChecked()
    {
        await ErrgoVerifier.VerifyAnalyzerAsync(/* lang=c#-test */
            """
            using Errgo;
            using System.Threading.Tasks;

            namespace TestNamespace
            {
                class TestClass
                {
                    async Task<Error> GetErrorAsync() => new Error("test");

                    async Task TestMethod()
                    {
                        var err = await GetErrorAsync();
                        System.Console.WriteLine("Lorem ipsum");
                    }
                }
            }
            """,
            expected: new DiagnosticResult(
                ErrgoAnalyzer.DiagnosticId, 
                DiagnosticSeverity.Warning)
            .WithSpan(
                startLine: 12,
                startColumn: 17,
                endLine: 12,
                endColumn: 20)
            .WithArguments("err"));
    }

    [Fact]
    public async Task Diagnostic_WhenAwaitedTupleErrorIsNotChecked()
    {
        await ErrgoVerifier.VerifyAnalyzerAsync(/* lang=c#-test */
            """
            using Errgo;
            using System.Threading.Tasks;

            namespace TestNamespace
            {
                class TestClass
                {
                    async Task<(string, Error)> GetDataAsync() => (string.Empty, new Error("test"));

                    async Task TestMethod()
                    {
                        var (data, err) = await GetDataAsync();
                        System.Console.WriteLine("Lorem ipsum");
                    }
                }
            }
            """,
            expected: new DiagnosticResult(
                ErrgoAnalyzer.DiagnosticId, 
                DiagnosticSeverity.Warning)
            .WithSpan(
                startLine: 12,
                startColumn: 24,
                endLine: 12,
                endColumn: 27)
            .WithArguments("err"));
    }
}