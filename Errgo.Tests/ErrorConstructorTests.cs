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
        Assert.Empty(err.SourceMemberName);
    }

    [Fact]
    public void Error_DefaultConstructor_WithNullMessageCaputersSourceLocation()
    {
        Error err = new(null!);
        Assert.NotEmpty(err.SourceMemberName);
    }

    [Fact]
    public void Error_DefaultConstructor_WithEmptyMessageCaputersSourceLocation()
    {
        Error err = new(string.Empty);
        Assert.NotEmpty(err.SourceMemberName);
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
        Assert.Equal(string.Empty, emptyError.Message);

        // Neither of them capture source location
        Assert.Empty(defaultError.SourceMemberName);
        Assert.Empty(defaultError.SourceFilePath);
        Assert.Equal(0, defaultError.SourceLineNumber);
        Assert.Empty(emptyError.SourceMemberName);
        Assert.Empty(emptyError.SourceFilePath);
        Assert.Equal(0, emptyError.SourceLineNumber);
    }

    [Fact]
    public void Error_Constructor_WithErrorNoneAsInner_NoChain()
    {
        var err = new Error("Outer", Error.None);
        Assert.Equal("Outer", err.Message);
        Assert.Empty(err.InnerErrors);
    }
}
