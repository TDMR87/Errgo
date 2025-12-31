using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;

namespace Errgo.Analyzer.Tests;

public class ErrgoAnalyzerTests
{
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

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsCheckedInNextStatement()
    {
        var test = CreateTest("""
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
        var test = CreateTest("""
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    Error GetError() => new Error("test");

                    void TestMethod()
                    {
                        var err = GetError();
                        System.Console.WriteLine("Next statement");
                    }
                }
            }
            """);

        test.ExpectedDiagnostics.Add(new DiagnosticResult("ERRGO001", DiagnosticSeverity.Warning)
            .WithSpan(
                startLine: 11, 
                startColumn: 17, 
                endLine: 11, 
                endColumn: 33)
            .WithArguments("err"));

        await test.RunAsync();
    }

    [Fact]
    public async Task Diagnostic_WhenErrorIsCheckedButNotInNextStatement()
    {
        var test = CreateTest("""
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    Error GetError() => new Error("test");

                    void TestMethod()
                    {
                        var err = GetError();
                        System.Console.WriteLine("Some other code");
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
                endColumn: 33)
            .WithArguments("err"));

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsCheckedWithNegation()
    {
        var test = CreateTest("""
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
        var test = CreateTest("""
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
        var test = CreateTest("""
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
        var test = CreateTest("""
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    void TestMethod()
                    {
                        var err = new Error("manual error");
                        System.Console.WriteLine("Next statement");
                    }
                }
            }
            """);

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsErrorNone()
    {
        var test = CreateTest("""
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    void TestMethod()
                    {
                        var err = Error.None;
                        System.Console.WriteLine("Next statement");
                    }
                }
            }
            """);

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsLastStatement()
    {
        var test = CreateTest("""
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
        var test = CreateTest("""
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
                    endColumn: 34)
                .WithArguments("err1"));

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ERRGO001", DiagnosticSeverity.Warning)
                .WithSpan(
                    startLine: 14, 
                    startColumn: 17, 
                    endLine: 14, 
                    endColumn: 34)
                .WithArguments("err2"));
        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WithExplicitErrorType()
    {
        var test = CreateTest("""
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
        var test = CreateTest("""
            using Errgo;

            namespace TestNamespace
            {
                class TestClass
                {
                    void TestMethod()
                    {
                        Error err;
                        System.Console.WriteLine("Next");
                    }
                }
            }
            """);

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenNotErrorType()
    {
        var test = CreateTest("""
            namespace TestNamespace
            {
                class TestClass
                {
                    string GetString() => "test";

                    void TestMethod()
                    {
                        var str = GetString();
                        System.Console.WriteLine("Next");
                    }
                }
            }
            """);

        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorInComplexCondition()
    {
        var test = CreateTest("""
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
}
