namespace Errgo.Tests;

public class ErrorConversionTests
{
    [Fact]
    public void Error_ImplicitBoolConversion()
    {
        var err = new Error("Test");
        Assert.True(err);
    }

    [Fact]
    public void Error_ImplicitBoolConversion_ErrorNone_ReturnsFalse()
    {
        var errorNone = Error.None;
        Assert.False(errorNone);
    }

    [Fact]
    public void Error_ImplicitBoolConversion_ErrorEmpty_ReturnsTrue()
    {
        var errorEmpty = Error.Empty;
        Assert.True(errorEmpty);
    }
}
