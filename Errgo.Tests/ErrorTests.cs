using Errgo;

namespace Errgo.Tests;

public class ErrorTests
{
    [Fact]
    public void Errors_WithIdenticalMemberValues_AreEqual()
    {
        Error err1 = new("error");
        Error err2 = new("error");
        Assert.Equal(err1, err2);
        Assert.True(err1 == err2);
        Assert.True(err1.Equals(err2));
    }

    [Fact]
    public void Errors_None_AreEqual()
    {
        Error err = Error.None;
        Assert.Equal(err, Error.None);
        Assert.True(err == Error.None);
        Assert.True(err.Equals(Error.None));
    }

    [Fact]
    public void Error_WithoutMessage_And_ErrorNone_AreNotEqual()
    {
        Assert.NotEqual(new Error(), Error.None);
        Assert.False(new Error() == Error.None);
        Assert.False(new Error().Equals(Error.None));
    }

    [Fact]
    public void Error_From_WithExpression()
    {
        Error err1 = new("error");
        // Can't use with expression on Message anymore since it's readonly
        // Create a new error instead
        Error err2 = new("different error");
        Assert.True(err2);
        Assert.NotEqual(err1, err2);
    }

    [Fact]
    public void Error_From_String()
    {
        Error err = new("error");
        Assert.True(err);
    }

    [Fact]
    public void Error_DefaultConstructor_ReturnsTrue()
    {
        Assert.True(new Error());
    }

    [Fact]
    public void Error_None_ReturnsFalse()
    {
        Assert.False(Error.None);
    }

    [Fact]
    public void Error_Sentinel_ValueEquality()
    {
        (object?, Error) GetItemById(int id)
        {
            return (default, SentinelErrors.NotFound);
        }

        var (item, err) = GetItemById(123);
        
        Assert.Equal(SentinelErrors.NotFound, err);
        Assert.True(err == SentinelErrors.NotFound);
        Assert.True(err.Equals(SentinelErrors.NotFound));
    }

    [Fact]
    public void Error_Chaining()
    {
        var err = new Error($"Some error happened", SentinelErrors.NotFound);
        Assert.True(err);
    }

    [Fact]
    public void Error_Chaining_MessagesAreChained()
    {
        var itemId = 123;

        static (object?, Error) GetItemById(int id)
        {
            return (null, new($"Failed to get item with id {id}", SentinelErrors.NotFound));
        }

        var (_, err) = GetItemById(itemId);
        Assert.Equal($"Failed to get item with id {itemId}. Item not found", err.FullMessage);
    }

    [Fact]
    public void Error_None_FullMessage_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, Error.None.FullMessage);
    }

    [Fact]
    public void Error_None_Messages_ReturnsEmptyList()
    {
        var errors = Error.None.InnerErrors;
        Assert.Empty(errors);
    }

    [Fact]
    public void Error_FullMessage_SkipsEmptyMessages()
    {
        var inner = new Error("");
        var middle = new Error("Middle error", inner);
        var outer = new Error("Outer error", middle);

        Assert.Equal("Outer error. Middle error. Unspecified error", outer.FullMessage);
    }

    [Fact]
    public void Error_FullMessage_SkipsWhitespaceMessages()
    {
        var inner = new Error("   ");
        var middle = new Error("Middle error", inner);
        var outer = new Error("Outer error", middle);

        Assert.Equal("Outer error. Middle error. Unspecified error", outer.FullMessage);
    }

    [Fact]
    public void Error_Messages_SkipsEmptyMessages()
    {
        var inner = new Error("");
        var middle = new Error("Middle error", inner);
        var outer = new Error("Outer error", middle);

        Assert.Equal(2, outer.InnerErrors.Count);
        Assert.Equal("Outer error", outer.Message);
        Assert.Equal("Middle error", outer.InnerErrors[0].Message);
        Assert.Equal("Unspecified error", outer.InnerErrors[1].Message);
    }

    [Fact]
    public void Error_Is_WithNoneTarget_ReturnsFalse()
    {
        var err = new Error("Some error");
        Assert.False(err.Is(Error.None));
    }

    [Fact]
    public void Error_Is_FindsErrorInChain()
    {
        var inner = SentinelErrors.NotFound;
        var middle = new Error("Middle error", inner);
        var outer = new Error("Outer error", middle);

        Assert.True(outer.Is(SentinelErrors.NotFound));
        Assert.True(middle.Is(SentinelErrors.NotFound));
        Assert.True(inner.Is(SentinelErrors.NotFound));
    }

    [Fact]
    public void Error_Is_DoesNotFindErrorNotInChain()
    {
        var differentError = new Error("Different error");
        var err = new Error("Some error", SentinelErrors.NotFound);

        Assert.False(err.Is(differentError));
    }

    [Fact]
    public void Error_None_Is_WithAnyTarget_ReturnsFalse()
    {
        Assert.False(Error.None.Is(SentinelErrors.NotFound));
        Assert.False(Error.None.Is(new Error("Some error")));
    }

    [Fact]
    public void Error_None_Is_ErrorNone_ReturnsTrue()
    {
        var err = Error.None;
        Assert.True(err.Is(Error.None));
        Assert.Equal(err, Error.None);
    }

    [Fact]
    public void Error_As_FindsErrorInChain()
    {
        var inner = SentinelErrors.NotFound;
        var middle = new Error("Middle error", inner);
        var outer = new Error("Outer error", middle);

        Assert.True(outer.As(SentinelErrors.NotFound, out var match));
        Assert.Equal(SentinelErrors.NotFound.Message, match.Message);
        
        Assert.True(middle.As(SentinelErrors.NotFound, out match));
        Assert.Equal(SentinelErrors.NotFound.Message, match.Message);
        
        Assert.True(inner.As(SentinelErrors.NotFound, out match));
        Assert.Equal(SentinelErrors.NotFound.Message, match.Message);
    }

    [Fact]
    public void Error_As_DoesNotFindErrorNotInChain()
    {
        var differentError = new Error("Different error");
        var err = new Error("Some error", SentinelErrors.NotFound);

        Assert.False(err.As(differentError, out var match));
        Assert.Equal(Error.None, match);
    }

    [Fact]
    public void Error_As_ReturnsErrorWithSourceLocation()
    {
        var (_, err) = GetDatabaseError();
        
        // Create a sentinel error without source location for matching
        var dbErrorSentinel = new Error("Database connection failed");
        
        // Use As to find the actual error in the chain with source location
        Assert.True(err.As(dbErrorSentinel, out var match));
        Assert.Equal("Database connection failed", match.Message);
        Assert.Equal("GetDatabaseError", match.MemberName);
        Assert.Contains("ErrorTests.cs", match.FilePath);
        Assert.True(match.LineNumber > 0);
    }

    [Fact]
    public void Error_None_As_WithAnyTarget_ReturnsFalse()
    {
        Assert.False(Error.None.As(SentinelErrors.NotFound, out var match));
        Assert.Equal(Error.None, match);
        
        Assert.False(Error.None.As(new Error("Some error"), out match));
        Assert.Equal(Error.None, match);
    }

    private (object?, Error) GetDatabaseError() => (null, new Error("Database connection failed"));

    [Fact]
    public void Error_CapturesSourceLocation()
    {
        var (val, err) = DoStuff();
        
        // Error properties contain the full details
        Assert.Equal("Database connection failed", err.Message);
        Assert.Equal("DoStuff", err.MemberName);
        Assert.Contains("ErrorTests.cs", err.FilePath);
        Assert.True(err.LineNumber > 0);
        
        // Stack includes location info
        Assert.Contains("Database connection failed", err.Stack);
        Assert.Contains("DoStuff", err.Stack);
        Assert.Contains("ErrorTests.cs", err.Stack);
    }

    private (string Value, Error err) DoStuff() => (string.Empty, new Error("Database connection failed"));

    [Fact]
    public void Error_CanChainErrors()
    {
        Func<(int, Error)> func1 = () => (default, new Error("Error 1"));
        Func<(string?, Error)> func2 = () => (default, new Error("Error 2"));
        Func<(bool?, Error)> func3 = () => (null, new Error("Error 3"));

        var errWrapper = Error.Empty;

        var (val, err) = func1();
        if (err) errWrapper.Wrap(new Error("Func1 failed", err)); // No reassignment!

        (var str, err) = func2();
        if (err) errWrapper.Wrap(new Error("Func2 failed", err)); // No reassignment!

        (var flag, err) = func3();
        if (err) errWrapper.Wrap(new Error("Func3 failed", err)); // No reassignment!

        // Verify the error chain is built correctly
        Assert.True(errWrapper);
        
        var fullMessage = errWrapper.FullMessage;
        Assert.Contains("Func1 failed", fullMessage);
        Assert.Contains("Func2 failed", fullMessage);
        Assert.Contains("Func3 failed", fullMessage);
        Assert.Contains("Error 1", fullMessage);
        Assert.Contains("Error 2", fullMessage);
        Assert.Contains("Error 3", fullMessage);
    }

    [Fact]
    public void Error_WithoutMessage_ShowsSourceLocationInStack()
    {
        var (_, err) = GetErrorWithoutMessage();
        
        // Even without a message, source location should be visible
        Assert.Equal("Unspecified error", err.Message);
        Assert.Equal("GetErrorWithoutMessage", err.MemberName);
        Assert.Contains("ErrorTests.cs", err.FilePath);
        Assert.True(err.LineNumber > 0);
        
        var stack = err.Stack;
        Assert.Contains("Unspecified error at GetErrorWithoutMessage", stack);
        Assert.Contains("ErrorTests.cs", stack);
        Assert.Contains("line", stack);
    }

    [Fact]
    public void Error_WithoutMessage_InChain_ShowsSourceLocationInStack()
    {
        var (val, innerErr) = GetErrorWithoutMessage();
        var err = new Error("Operation failed", innerErr);
        
        // Outer error with message should be shown normally
        Assert.Contains("Operation failed", err.Stack);
        
        // Inner error without message should still show its source location
        Assert.Contains("Unspecified error at GetErrorWithoutMessage", err.Stack);
        Assert.Contains("ErrorTests.cs", err.Stack);
    }

    private (object?, Error) GetErrorWithoutMessage() => (null, new Error(""));
}
