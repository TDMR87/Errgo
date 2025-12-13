namespace Errgo.Tests;

public class ErrorNoneTests
{
    [Fact]
    public void Error_None_AreEqual()
    {
        Error err = Error.None;
        Assert.Equal(err, Error.None);
        Assert.True(err == Error.None);
        Assert.True(err.Equals(Error.None));
    }

    [Fact]
    public void Error_None_ReturnsFalse()
    {
        Assert.False(Error.None);
    }

    [Fact]
    public void Error_None_StackReturnsEmptyString()
    {
        Assert.Equal(string.Empty, Error.None.Stack);
    }

    [Fact]
    public void Error_None_InnerErrorsReturnsEmptyList()
    {
        var errors = Error.None.InnerErrors;
        Assert.Empty(errors);
    }

    [Fact]
    public void Error_Default_IsSameAsErrorNone()
    {
        Error defaultErr = default;
        
        Assert.Equal(Error.None, defaultErr);
        Assert.False(defaultErr);
    }
}
