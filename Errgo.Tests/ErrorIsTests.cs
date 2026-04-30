namespace Errgo.Tests;

public class ErrorIsTests
{
    public static readonly Error NotFound = new("Item not found");
    public static readonly Error DatabaseError = new("Database error");

    [Fact]
    public void Error_Is_ReturnsFalseWhenErrorNone()
    {
        var err = new Error("Some error");
        Assert.False(err.Is(Error.None));
    }

    [Fact]
    public void Error_Is_DefaultError_ShouldStillMatchDefaultError()
    {
        var err = new Error("error");
        var joined = Error.Join(err, default(Error), new Error("next error"));

        Assert.True(joined.Is(default(Error)));
    }

    [Fact]
    public void Error_Is_ReturnsTrueWhenErrorInChain()
    {
        var inner = NotFound;
        var middle = new Error("Middle error", inner);
        var outer = new Error("Outer error", middle);

        Assert.True(outer.Is(NotFound));
        Assert.True(middle.Is(NotFound));
        Assert.True(inner.Is(NotFound));
    }

    [Fact]
    public void Error_Is_ReturnsFalseWhenErrorNotInChain()
    {
        var differentError = new Error("Different error");
        var err = new Error("Some error", NotFound);
        Assert.False(err.Is(differentError));
    }

    [Fact]
    public void Error_Is_WithAnyTargetReturnsFalse()
    {
        Assert.False(Error.None.Is(NotFound));
        Assert.False(Error.None.Is(new Error("Some error")));
    }

    [Fact]
    public void Error_Is_ErrorNoneReturnsTrue()
    {
        var err = Error.None;
        Assert.True(err.Is(Error.None));
        Assert.Equal(err, Error.None);
    }

    [Fact]
    public void Error_Is_CaseSensitive()
    {
        var err1 = new Error("Error Message");
        var err2 = new Error("error message");

        Assert.False(err1.Is(err2));
    }

    [Fact]
    public void Error_Is_EmptyMessageErrors_Match()
    {
        var err1 = new Error("");
        var err2 = new Error("");

        Assert.True(err1.Is(err2));
    }

    [Fact]
    public void Error_Is_WithDuplicateErrorsInChain_StillFinds()
    {
        var err1 = new Error("Duplicate");
        var err2 = new Error("Duplicate");
        var chain = new Error("Outer", err1);
        chain = Error.Join(chain, err2);

        Assert.True(chain.Is(new Error("Duplicate")));
    }

    [Fact]
    public void Error_As_WithDuplicates_ReturnsFirst()
    {
        var err1 = new Error("Duplicate");
        var err2 = new Error("Duplicate");
        var chain = new Error("Outer");
        chain = Error.Join(chain, err1);
        chain = Error.Join(chain, err2);

        Assert.True(chain.As(new Error("Duplicate"), out var match));
        // Should find the first occurrence in traversal order.
        Assert.Equal("Duplicate", match.Message);
    }

    [Fact]
    public void Error_Is_SearchesFullDepth()
    {
        var deepest = NotFound;
        var middle1 = new Error("Middle 1", deepest);
        var middle2 = new Error("Middle 2", middle1);
        var outer = new Error("Outer", middle2);

        Assert.True(outer.Is(NotFound));
    }

    [Fact]
    public void Error_As_SearchesFullDepth()
    {
        var deepest = NotFound;
        var middle1 = new Error("Middle 1", deepest);
        var middle2 = new Error("Middle 2", middle1);
        var outer = new Error("Outer", middle2);

        Assert.True(outer.As(NotFound, out var match));
        Assert.Equal(NotFound.Message, match.Message);
    }

    [Fact]
    public void Error_Is_WithPartialMessageMatch_ReturnsFalse()
    {
        var err = new Error("Item not found in database");

        Assert.False(err.Is(NotFound)); // Exact match only
    }

    [Fact]
    public void Error_As_WithPartialMessageMatch_ReturnsFalse()
    {
        var err = new Error("Item not found in database");

        Assert.False(err.As(NotFound, out var match));
        Assert.Equal(Error.None, match);
    }

    [Fact]
    public void Error_Is_OnItself_ReturnsTrue()
    {
        var err = new Error("Test error");

        Assert.True(err.Is(err));
        Assert.True(err.Is(new Error("Test error")));
    }

    [Fact]
    public void Error_As_OnItself_FindsMatch()
    {
        var err = new Error("Test error");

        Assert.True(err.As(err, out var match));
        Assert.Equal(err, match);
    }

    [Fact]
    public void Error_Is_MultipleDifferentErrors_FindsCorrectOne()
    {
        var err1 = new Error("Error 1");
        var err2 = new Error("Error 2");
        var err3 = new Error("Error 3");

        var joiner = new Error("Joiner");
        joiner = Error.Join(joiner, err1, err2, err3);

        Assert.True(joiner.Is(new Error("Error 2")));
        Assert.True(joiner.Is(new Error("Error 1")));
        Assert.True(joiner.Is(new Error("Error 3")));
        Assert.False(joiner.Is(new Error("Error 4")));
    }

    [Fact]
    public void Error_As_MultipleDifferentErrors_FindsCorrectOne()
    {
        var err1 = new Error("Error 1");
        var err2 = new Error("Error 2");
        var err3 = new Error("Error 3");

        var joiner = new Error("Joiner");
        joiner = Error.Join(joiner, err1, err2, err3);

        Assert.True(joiner.As(new Error("Error 2"), out var match));
        Assert.Equal("Error 2", match.Message);
    }

    [Fact]
    public void Error_Is_MixedConstructorAndJoin_SearchesBoth()
    {
        var constructorErr = new Error("Constructor Error");
        var joiner = new Error("Joiner", constructorErr);

        var joinErr = new Error("Join Error");
        joiner = Error.Join(joiner, joinErr);

        Assert.True(joiner.Is(new Error("Constructor Error")));
        Assert.True(joiner.Is(new Error("Join Error")));
    }

    [Fact]
    public void Error_Is_EmptyMessageWithInnerErrors_SearchesInnerErrors()
    {
        var sentinel = Error.Sentinel("Database error");
        var outer = Error.Empty;
        outer = Error.Join(outer, sentinel);
        Assert.Empty(outer.Message);
        Assert.True(outer.Is(sentinel)); // Should find in inner errors even though outer message is empty
    }
}

