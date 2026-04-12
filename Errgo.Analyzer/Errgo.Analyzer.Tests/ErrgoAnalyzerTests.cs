/*
 * How to test a Roslyn analyzer:
 * https://www.meziantou.net/how-to-test-a-roslyn-analyzer.htm
*/

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;

namespace Errgo.Analyzer.Tests;

public class ErrgoAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_WhenErrorIsCheckedInNextStatement()
    {
        var test = CreateTest(/* lang=c#-test */"""
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

        await test.RunAsync();
    }

    [Fact]
    public async Task Diagnostic_WhenErrorIsNotChecked()
    {
        var test = CreateTest(/* lang=c#-test */"""
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
            """);

        test.ExpectedDiagnostics.Add(new DiagnosticResult("ERRGO001", DiagnosticSeverity.Warning)
            .WithSpan(
                startLine: 11, 
                startColumn: 17, 
                endLine: 11, 
                endColumn: 20)
            .WithArguments("err"));

        await test.RunAsync();
    }

    [Fact]
    public async Task Diagnostic_WhenErrorIsNotChecked_Tuple()
    {
        var test = CreateTest(/* lang=c#-test */"""
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
            """);

        test.ExpectedDiagnostics.Add(new DiagnosticResult("ERRGO001", DiagnosticSeverity.Warning)
            .WithSpan(
                startLine: 11,
                startColumn: 27,
                endLine: 11,
                endColumn: 30)
            .WithArguments("err"));

        await test.RunAsync();
    }

    [Fact]
    public async Task Diagnostic_WhenErrorIsNotChecked_Tuple2()
    {
        var test = CreateTest(/* lang=c#-test */"""
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
            """);

        test.ExpectedDiagnostics.Add(new DiagnosticResult("ERRGO001", DiagnosticSeverity.Warning)
            .WithSpan(
                startLine: 11,
                startColumn: 23,
                endLine: 11,
                endColumn: 26)
            .WithArguments("err"));

        await test.RunAsync();
    }

    [Fact]
    public async Task Diagnostic_WhenErrorIsNotChecked_Tuple3()
    {
        var test = CreateTest(/* lang=c#-test */"""
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
            """);

        test.ExpectedDiagnostics.Add(new DiagnosticResult("ERRGO001", DiagnosticSeverity.Warning)
            .WithSpan(
                startLine: 17,
                startColumn: 19,
                endLine: 17,
                endColumn: 22)
            .WithArguments("err"));

        await test.RunAsync();
    }

    [Fact]
    public async Task Diagnostic_WhenErrorIsCheckedButNotInNextStatement()
    {
        var test = CreateTest(/* lang=c#-test */"""
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
            """);

        test.ExpectedDiagnostics.Add(new DiagnosticResult("ERRGO001", DiagnosticSeverity.Warning)
            .WithSpan(
                startLine: 11, 
                startColumn: 17, 
                endLine: 11, 
                endColumn: 20)
            .WithArguments("err"));

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsCheckedWithNegation()
    {
        var test = CreateTest(/* lang=c#-test */"""
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

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsCheckedWithComparison()
    {
        var test = CreateTest(/* lang=c#-test */"""
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

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsCheckedWithNotEqual()
    {
        var test = CreateTest(/* lang=c#-test */"""
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

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsNotFromMethodCall()
    {
        var test = CreateTest(/* lang=c#-test */"""
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

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorNone()
    {
        var test = CreateTest(/* lang=c#-test */"""
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

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorEmpty()
    {
        var test = CreateTest(/* lang=c#-test */"""
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

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsLastStatementInVoidMethod()
    {
        var test = CreateTest(/* lang=c#-test */"""
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

        await test.RunAsync();
    }

    [Fact]
    public async Task Diagnostic_WithMultipleErrors_AllUnchecked()
    {
        var test = CreateTest(/* lang=c#-test */"""
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
            """);

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ERRGO001", DiagnosticSeverity.Warning)
                .WithSpan(
                    startLine: 11, 
                    startColumn: 17, 
                    endLine: 11, 
                    endColumn: 21)
                .WithArguments("err1"));

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ERRGO001", DiagnosticSeverity.Warning)
                .WithSpan(
                    startLine: 14, 
                    startColumn: 17, 
                    endLine: 14, 
                    endColumn: 21)
                .WithArguments("err2"));
        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenChecked_WithExplicitErrorType()
    {
        var test = CreateTest(/* lang=c#-test */"""
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

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenVariableHasNoInitializer()
    {
        var test = CreateTest(/* lang=c#-test */"""
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

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorInComplexCondition()
    {
        var test = CreateTest(/* lang=c#-test */"""
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

        await test.RunAsync();
    }

    [Fact]
    public async Task Diagnostic_WhenAwaitedErrorIsNotChecked()
    {
        var test = CreateTest(/* lang=c#-test */"""
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
            """);

        test.ExpectedDiagnostics.Add(new DiagnosticResult("ERRGO001", DiagnosticSeverity.Warning)
            .WithSpan(
                startLine: 12,
                startColumn: 17,
                endLine: 12,
                endColumn: 20)
            .WithArguments("err"));

        await test.RunAsync();
    }

    [Fact]
    public async Task Diagnostic_WhenAwaitedTupleErrorIsNotChecked()
    {
        var test = CreateTest(/* lang=c#-test */"""
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
            """);

        test.ExpectedDiagnostics.Add(new DiagnosticResult("ERRGO001", DiagnosticSeverity.Warning)
            .WithSpan(
                startLine: 12,
                startColumn: 24,
                endLine: 12,
                endColumn: 27)
            .WithArguments("err"));

        await test.RunAsync();
    }

    private static readonly ReferenceAssemblies ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

    private static CSharpAnalyzerTest<ErrgoAnalyzer, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier> CreateTest(string code)
    {
        var test = new CSharpAnalyzerTest<ErrgoAnalyzer, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies,
        };

        test.TestState.AdditionalReferences.Add(typeof(Error).Assembly);

        return test;
    }
}
