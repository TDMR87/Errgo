namespace Errgo.Tests;

public class ErrorEmptyTests
{
    [Fact]
    public void Error_Empty_IsAnError()
    {
        var err = Error.Empty;
        Assert.True(err);
        Assert.NotEqual(Error.None, err);
    }

    [Fact]
    public void Error_Empty_HasEmptyMessage()
    {
        var err = Error.Empty;
        Assert.Empty(err.Message);
    }
}
