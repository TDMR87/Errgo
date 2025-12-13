namespace Errgo.Tests;

public class ErrorWrapTests
{
    [Fact]
    public void Error_Wrap_CanChainErrors()
    {
        Func<(int, Error)> func1 = () => (default, new Error("Error 1"));
        Func<(string?, Error)> func2 = () => (default, new Error("Error 2"));
        Func<(bool?, Error)> func3 = () => (null, new Error("Error 3"));

        var errWrapper = Error.Empty;

        var (val, err) = func1();
        if (err) errWrapper.Wrap(new Error("Func1 failed", err));

        (var str, err) = func2();
        if (err) errWrapper.Wrap(new Error("Func2 failed", err));

        (var flag, err) = func3();
        if (err) errWrapper.Wrap(new Error("Func3 failed", err));

        // Verify the error
        Assert.True(errWrapper);

        var stack = errWrapper.Stack;
        Assert.Contains("Func1 failed", stack);
        Assert.Contains("Func2 failed", stack);
        Assert.Contains("Func3 failed", stack);
        Assert.Contains("Error 1", stack);
        Assert.Contains("Error 2", stack);
        Assert.Contains("Error 3", stack);
    }

    [Fact]
    public void Error_Wrap_WithNullArray_DoesNotThrow()
    {
        var err = new Error("Original");
        err.Wrap(null!);
        Assert.Equal("Original", err.Message);
        Assert.Empty(err.InnerErrors);
    }

    [Fact]
    public void Error_Wrap_WithEmptyArray_DoesNotThrow()
    {
        var err = new Error("Original");
        err.Wrap(Array.Empty<Error>());
        Assert.Equal("Original", err.Message);
        Assert.Empty(err.InnerErrors);
    }

    [Fact]
    public void Error_Wrap_WithErrorNoneInArray_IgnoresErrorNone()
    {
        var err = new Error("Original");
        var validError = new Error("Valid");
        err.Wrap(Error.None, validError, Error.None);

        Assert.Contains("Valid", err.Stack);
        Assert.Single(err.InnerErrors);
    }

    [Fact]
    public void Error_Wrap_OnErrorNone_DoesNothing()
    {
        var err = Error.None;
        err.Wrap(new Error("Should not appear"));
        Assert.False(err);
    }

    [Fact]
    public void Error_Wrap_MultipleTimesPrepends()
    {
        var err = new Error("Original");
        err.Wrap(new Error("First wrap"));
        err.Wrap(new Error("Second wrap"));

        var stack = err.Stack;
        var lines = stack.Split(Environment.NewLine);

        // Original is the outer error (appears first in Stack)
        Assert.Contains("Original", lines[0]);
        Assert.Contains("Second wrap", lines[1]);
        Assert.Contains("First wrap", lines[2]);
    }

    [Fact]
    public void Error_Wrap_SameErrorTwice_AddsBothInstances()
    {
        var err = new Error("Original");
        var inner = new Error("Inner");

        err.Wrap(inner);
        err.Wrap(inner); // Wrap same error again

        Assert.Equal(2, err.InnerErrors.Count);
        Assert.Equal("Inner", err.InnerErrors[0].Message);
        Assert.Equal("Inner", err.InnerErrors[1].Message);
    }

    [Fact]
    public void Error_Wrap_MixedConstructor_CorrectOrder()
    {
        var err1 = new Error("Error 1");
        var err2 = new Error("Error 2", err1);

        err2.Wrap(new Error("Error 3"));
        err2.Wrap(new Error("Error 4"));

        var innerErrors = err2.InnerErrors;

        // Wrap prepends, constructor appends
        // Expected order: Error 4, Error 3, Error 1
        Assert.Equal(3, innerErrors.Count);
        Assert.Equal("Error 4", innerErrors[0].Message);
        Assert.Equal("Error 3", innerErrors[1].Message);
        Assert.Equal("Error 1", innerErrors[2].Message);
    }

    [Fact]
    public void Error_Wrap_SingleError_NotInArray()
    {
        var err = new Error("Original");
        var inner = new Error("Inner");

        err.Wrap(inner); // Not using array syntax

        Assert.Single(err.InnerErrors);
        Assert.Equal("Inner", err.InnerErrors[0].Message);
    }

    [Fact]
    public void Error_Wrap_ChainedErrors_FlattenedInInnerErrors()
    {
        var err1 = new Error("Error 1");
        var err2 = new Error("Error 2", err1);
        var err3 = new Error("Error 3");

        var wrapper = new Error("Wrapper");
        wrapper.Wrap(err3, err2); // err2 has its own chain

        var innerErrors = wrapper.InnerErrors;

        // Should flatten: err3, err2, err1
        Assert.Equal(3, innerErrors.Count);
        Assert.Equal("Error 3", innerErrors[0].Message);
        Assert.Equal("Error 2", innerErrors[1].Message);
        Assert.Equal("Error 1", innerErrors[2].Message);
    }

    [Fact]
    public void Error_Wrap_AfterConstructorChain_CombinesBoth()
    {
        var constructorInner = new Error("Constructor Inner");
        var err = new Error("Outer", constructorInner);

        var wrapInner = new Error("Wrap Inner");
        err.Wrap(wrapInner);

        Assert.Equal(2, err.InnerErrors.Count);
        Assert.Equal("Wrap Inner", err.InnerErrors[0].Message);
        Assert.Equal("Constructor Inner", err.InnerErrors[1].Message);
    }
}
