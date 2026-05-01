namespace Errgo.Tests;

public class ErrorMessageDetailsTests
{
    [Fact]
    public void Error_DefaultTruthyValue_ShouldExposeVisibleMessageDetails()
    {
        var error = default(Error);

        Assert.True(error);
        Assert.False(string.IsNullOrWhiteSpace(error.MessageDetails));
    }
}
