namespace Errgo.Tests;

public class ErrorConstructorTests
{
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
    public void Error_DefaultConstructor_DoesNotCaptureSourceLocation()
    {
        var err = new Error();
        Assert.Null(err.Member);
        Assert.Null(err.Filepath);
        Assert.Null(err.LineNum);
    }

    [Fact]
    public void Error_DefaultConstructor_WithNullMessageCaputersSourceLocation()
    {
        Error err = new(message: null!);
        Assert.Equal("Unknown error", err.Message);
        Assert.NotNull(err.Member);
        Assert.NotEmpty(err.Member);
        Assert.NotNull(err.Filepath);
        Assert.NotEmpty(err.Filepath);
        Assert.NotNull(err.LineNum);
        Assert.NotEqual(0, err.LineNum);
    }

    [Fact]
    public void Error_DefaultConstructor_WithEmptyMessageCaputersSourceLocation()
    {
        Error err = new(string.Empty);
        Assert.NotNull(err.Member);
    }

    [Fact]
    public void Error_DefaultConstructor_And_ErrorEmpty_AreNotEqual()
    {
        var defaultError = new Error();
        var emptyError = Error.Empty;

        Assert.NotEqual(defaultError, emptyError);
        Assert.False(defaultError == emptyError);
        Assert.False(defaultError.Equals(emptyError));

        // They have different messages
        Assert.Equal("Unknown error", defaultError.Message);
        Assert.Empty(emptyError.Message);

        // Neither of them capture source location
        Assert.Null(defaultError.Member);
        Assert.Null(defaultError.Filepath);
        Assert.Null(defaultError.LineNum);
        Assert.Null(emptyError.Member);
        Assert.Null(emptyError.Filepath);
        Assert.Null(emptyError.LineNum);
    }

    [Fact]
    public void Error_Constructor_WrappingErrorNone_HasNoInnerErrors()
    {
        var err = new Error("Outer", Error.None);
        Assert.Equal("Outer", err.Message);
        Assert.Empty(err.InnerErrors);
    }
}
