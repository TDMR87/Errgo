namespace Errgo.Tests;

public class ErrorAsTests
{
    public static readonly Error NotFoundError = Error.Sentinel("Item not found");
    public static readonly Error TimeoutError = Error.Sentinel("Database connection timed out");

    private static (object?, Error) GetDatabaseError()
    {
        return (null, new Error("Database error", TimeoutError));
    }

    [Fact]
    public void Error_As_FindsErrorInChain()
    {
        var inner = NotFoundError;
        var middle = new Error("Middle error", inner);
        var outer = new Error("Outer error", middle);

        Assert.True(outer.As(NotFoundError, out var match));
        Assert.Equal(NotFoundError, match);
        
        Assert.True(middle.As(NotFoundError, out match));
        Assert.Equal(NotFoundError, match);

        Assert.True(inner.As(NotFoundError, out match));
        Assert.Equal(NotFoundError, match);
    }

    [Fact]
    public void Error_As_DoesNotFindErrorInChain()
    {
        var differentError = new Error("Different error");
        var err = new Error("Some error", NotFoundError);

        Assert.False(err.As(differentError, out var match));
        Assert.Equal(Error.None, match);
    }

    [Fact]
    public void Error_As_ReturnsErrorWithSourceLocation()
    {
        var (_, err) = GetDatabaseError();
        
        Assert.True(err.As(TimeoutError, out var match));
        Assert.Equal(TimeoutError, match);
        Assert.Equal("GetDatabaseError", err.SourceMemberName);
        Assert.Contains("ErrorAsTests.cs", err.SourceFilepath);
        Assert.True(err.SourceLineNumber > 0);
    }

    [Fact]
    public void Error_As_WithErrorNoneReturnsFalse()
    {
        Assert.False(Error.None.As(NotFoundError, out var match));
        Assert.Equal(Error.None, match);
        
        Assert.False(Error.None.As(new Error("Some error"), out match));
        Assert.Equal(Error.None, match);
    }

    [Fact]
    public void Error_As_EmptyMessage_FindsMatch()
    {
        var err = new Error("Outer", new Error(""));
        
        Assert.True(err.As(new Error(""), out var match));
        Assert.Equal("", match.Message);
    }

    [Fact]
    public void Error_As_EmptyMessageWithInnerErrors_SearchesInnerErrors()
    {
        var sentinel = Error.Sentinel("Database error");
        var outer = Error.Empty;
        outer = Error.Join(outer, sentinel);
        Assert.Empty(outer.Message);
        Assert.True(outer.As(sentinel, out var match));
        Assert.Equal(sentinel, match);
    }
}

