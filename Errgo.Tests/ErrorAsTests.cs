namespace Errgo.Tests;

public class ErrorAsTests
{
    public static readonly Error NotFound = new("Item not found");

    private static (object?, Error) GetDatabaseError() => (null, new Error("Database connection failed"));

    [Fact]
    public void Error_As_FindsErrorInChain()
    {
        var inner = NotFound;
        var middle = new Error("Middle error", inner);
        var outer = new Error("Outer error", middle);

        Assert.True(outer.As(NotFound, out var match));
        Assert.Equal(NotFound.Message, match.Message);
        
        Assert.True(middle.As(NotFound, out match));
        Assert.Equal(NotFound.Message, match.Message);
        
        Assert.True(inner.As(NotFound, out match));
        Assert.Equal(NotFound.Message, match.Message);
    }

    [Fact]
    public void Error_As_DoesNotFindErrorInChain()
    {
        var differentError = new Error("Different error");
        var err = new Error("Some error", NotFound);

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
        Assert.Equal("GetDatabaseError", match.Member);
        Assert.Contains("ErrorAsTests.cs", match.Filepath);
        Assert.True(match.LineNum > 0);
    }

    [Fact]
    public void Error_As_WithErrorNoneReturnsFalse()
    {
        Assert.False(Error.None.As(NotFound, out var match));
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
        outer.Join(sentinel);
        Assert.Empty(outer.Message);
        Assert.True(outer.As(sentinel, out var match));
        Assert.Equal(sentinel, match);
    }
}
