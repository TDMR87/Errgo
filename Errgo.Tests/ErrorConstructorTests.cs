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
        Assert.Null(err.SourceMemberName);
        Assert.Null(err.SourceFilePath);
        Assert.Null(err.SourceLineNumber);
    }

    [Fact]
    public void Error_DefaultConstructor_WithNullMessageCapturesSourceLocation()
    {
        Error err = new(message: null!);
        Assert.Equal("Unknown error", err.Message);
        Assert.NotNull(err.SourceMemberName);
        Assert.NotEmpty(err.SourceMemberName);
        Assert.NotNull(err.SourceFilePath);
        Assert.NotEmpty(err.SourceFilePath);
        Assert.NotNull(err.SourceLineNumber);
        Assert.NotEqual(0, err.SourceLineNumber);
    }

    [Fact]
    public void Error_DefaultConstructor_WithEmptyMessageCapturesSourceLocationInfo()
    {
        Error err = new(string.Empty);
        Assert.NotNull(err.SourceMemberName);
        Assert.NotNull(err.SourceFilePath);
        Assert.NotNull(err.SourceLineNumber);
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
        Assert.Null(defaultError.SourceMemberName);
        Assert.Null(defaultError.SourceFilePath);
        Assert.Null(defaultError.SourceLineNumber);
        Assert.Null(emptyError.SourceMemberName);
        Assert.Null(emptyError.SourceFilePath);
        Assert.Null(emptyError.SourceLineNumber);
    }

    [Fact]
    public void Error_Constructor_WrappingErrorNone_HasNoInnerErrors()
    {
        var err = new Error("Outer", Error.None);
        Assert.Equal("Outer", err.Message);
        Assert.Empty(err.InnerErrors);
    }

    [Fact]
    public void Error_Constructor_WrappingError()
    {
        var err = new Error("Outer", new Error("Inner"));
        Assert.Equal("Outer", err.Message);
        Assert.Equal("Inner", err.InnerErrors[0].Message);
    }

    [Fact]
    public void Error_DefaultInArray_IsNot_ErrorNone()
    {
        var errorArray = new Error[3];

        Assert.Equal(default, errorArray[0]);
        Assert.Equal(default, errorArray[1]);
        Assert.Equal(default, errorArray[2]);
        Assert.DoesNotContain(Error.None, errorArray);
    }

    [Fact]
    public void Error_Default()
    {
        var error = default(Error);
        Assert.True(error);
    }
}
