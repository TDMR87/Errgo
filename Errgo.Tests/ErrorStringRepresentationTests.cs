namespace Errgo.Tests;

public class ErrorStringRepresentationTests
{
    [Fact]
    public void Error_Details_MatchesToString()
    {
        var err = new Error("Test error");
        Assert.Equal(err.ToString(), err.MessageDetails);
    }

    [Fact]
    public void Error_ToString_WithNoSourceLocation_ReturnsMessageOnly()
    {
        var err = new Error("Test", "", "", 0);
        Assert.Equal("Test", err.ToString());
    }
}
