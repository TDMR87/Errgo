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

        // Add reference to Errgo assembly
        test.TestState.AdditionalReferences.Add(typeof(Error).Assembly);
        
        return test;
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsCheckedInNextStatement()
    {
        var code = @"
using Errgo;

namespace TestNamespace
{
    class TestClass
    {
        Error GetError() => new Error(""test"");

        void TestMethod()
        {
            var err = GetError();
            if (err)
            {
                // Handle error
            }
        }
    }
}";

        var test = CreateTest(code);
        await test.RunAsync();
    }

    [Fact]
    public async Task Diagnostic_WhenErrorIsNotChecked()
    {
        var code = @"
using Errgo;

namespace TestNamespace
{
    class TestClass
    {
        Error GetError() => new Error(""test"");

        void TestMethod()
        {
            var err = GetError();
            System.Console.WriteLine(""Next statement"");
        }
    }
}";

        var test = CreateTest(code);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ERRGO001", DiagnosticSeverity.Warning)
                .WithSpan(12, 17, 12, 33)
                .WithArguments("err"));
        await test.RunAsync();
    }

    [Fact]
    public async Task Diagnostic_WhenErrorIsCheckedButNotInNextStatement()
    {
        var code = @"
using Errgo;

namespace TestNamespace
{
    class TestClass
    {
        Error GetError() => new Error(""test"");

        void TestMethod()
        {
            var err = GetError();
            System.Console.WriteLine(""Some other code"");
            if (err)
            {
                // Check is too late
            }
        }
    }
}";

        var test = CreateTest(code);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ERRGO001", DiagnosticSeverity.Warning)
                .WithSpan(12, 17, 12, 33)
                .WithArguments("err"));
        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsCheckedWithNegation()
    {
        var code = @"
using Errgo;

namespace TestNamespace
{
    class TestClass
    {
        Error GetError() => new Error(""test"");

        void TestMethod()
        {
            var err = GetError();
            if (!err)
            {
                // Success path
            }
        }
    }
}";

        var test = CreateTest(code);
        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsCheckedWithComparison()
    {
        var code = @"
using Errgo;

namespace TestNamespace
{
    class TestClass
    {
        Error GetError() => new Error(""test"");

        void TestMethod()
        {
            var err = GetError();
            if (err == Error.None)
            {
                // Success
            }
        }
    }
}";

        var test = CreateTest(code);
        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsCheckedWithNotEqual()
    {
        var code = @"
using Errgo;

namespace TestNamespace
{
    class TestClass
    {
        Error GetError() => new Error(""test"");

        void TestMethod()
        {
            var err = GetError();
            if (err != Error.None)
            {
                // Error path
            }
        }
    }
}";

        var test = CreateTest(code);
        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsNotFromMethodCall()
    {
        var code = @"
using Errgo;

namespace TestNamespace
{
    class TestClass
    {
        void TestMethod()
        {
            var err = new Error(""manual error"");
            System.Console.WriteLine(""Next statement"");
        }
    }
}";

        var test = CreateTest(code);
        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsErrorNone()
    {
        var code = @"
using Errgo;

namespace TestNamespace
{
    class TestClass
    {
        void TestMethod()
        {
            var err = Error.None;
            System.Console.WriteLine(""Next statement"");
        }
    }
}";

        var test = CreateTest(code);
        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorIsLastStatement()
    {
        var code = @"
using Errgo;

namespace TestNamespace
{
    class TestClass
    {
        Error GetError() => new Error(""test"");

        void TestMethod()
        {
            var err = GetError();
        }
    }
}";

        var test = CreateTest(code);
        await test.RunAsync();
    }

    [Fact]
    public async Task Diagnostic_WithMultipleErrors_AllUnchecked()
    {
        var code = @"
using Errgo;

namespace TestNamespace
{
    class TestClass
    {
        Error GetError() => new Error(""test"");

        void TestMethod()
        {
            var err1 = GetError();
            System.Console.WriteLine(""Next"");
            
            var err2 = GetError();
            System.Console.WriteLine(""Next"");
        }
    }
}";

        var test = CreateTest(code);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ERRGO001", DiagnosticSeverity.Warning)
                .WithSpan(12, 17, 12, 34)
                .WithArguments("err1"));
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ERRGO001", DiagnosticSeverity.Warning)
                .WithSpan(15, 17, 15, 34)
                .WithArguments("err2"));
        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WithExplicitErrorType()
    {
        var code = @"
using Errgo;

namespace TestNamespace
{
    class TestClass
    {
        Error GetError() => new Error(""test"");

        void TestMethod()
        {
            Error err = GetError();
            if (err)
            {
                // Handle error
            }
        }
    }
}";

        var test = CreateTest(code);
        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenVariableHasNoInitializer()
    {
        var code = @"
using Errgo;

namespace TestNamespace
{
    class TestClass
    {
        void TestMethod()
        {
            Error err;
            System.Console.WriteLine(""Next"");
        }
    }
}";

        var test = CreateTest(code);
        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenNotErrorType()
    {
        var code = @"
namespace TestNamespace
{
    class TestClass
    {
        string GetString() => ""test"";

        void TestMethod()
        {
            var str = GetString();
            System.Console.WriteLine(""Next"");
        }
    }
}";

        var test = CreateTest(code);
        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorInComplexCondition()
    {
        var code = @"
using Errgo;

namespace TestNamespace
{
    class TestClass
    {
        Error GetError() => new Error(""test"");

        void TestMethod()
        {
            var err = GetError();
            if (err || true)
            {
                // Error is mentioned
            }
        }
    }
}";

        var test = CreateTest(code);
        await test.RunAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenErrorInParenthesizedCondition()
    {
        var code = @"
using Errgo;

namespace TestNamespace
{
    class TestClass
    {
        Error GetError() => new Error(""test"");

        void TestMethod()
        {
            var err = GetError();
            if ((err))
            {
                // Error is checked
            }
        }
    }
}";

        var test = CreateTest(code);
        await test.RunAsync();
    }
}
