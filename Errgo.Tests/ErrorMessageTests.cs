namespace Errgo.Tests;

public class ErrorMessageTests
{
    [Fact]
    public void Error_Message_NullMessageReturnsUnknownError()
    {
        var inner = new Error(message: null);
        var middle = new Error("Middle error", inner);
        var outer = new Error("Outer error", middle);

        Assert.Equal(2, outer.InnerErrors.Count);
        Assert.Equal("Outer error", outer.Message);
        Assert.Equal("Middle error", outer.InnerErrors.ElementAt(0).Message);
        Assert.Equal("Unknown error", outer.InnerErrors.ElementAt(1).Message);
    }

    [Fact]
    public void Error_Message_VeryLongString_NoTruncation()
    {
        var longMessage = new string('x', 10000);
        var err = new Error(longMessage);

        Assert.Equal(longMessage, err.Message);
        Assert.Contains(longMessage, err.Stack);
    }

    [Fact]
    public void Error_Message_WithNewlines_PreservesFormatting()
    {
        var message = "Line1\nLine2\rLine3\r\nLine4";
        var err = new Error(message);
        Assert.Equal(message, err.Message);
    }

    [Fact]
    public void Error_Message_WithUnicode_PreservesCharacters()
    {
        var message = "Error: ?? ?? émojis";
        var err = new Error(message);
        Assert.Equal(message, err.Message);
    }

    [Fact]
    public void Error_Message_EmptyString_NotEqualToNull()
    {
        var errEmpty = new Error("");
        var errNull = new Error(message: null);
        Assert.NotEqual(errEmpty, errNull);
    }

    [Fact]
    public void Error_DefaultTruthyValue_ShouldExposeVisibleMessage()
    {
        var error = default(Error);
        Assert.True(error);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }
}
