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
        Assert.Null(err.SourceFilepath);
        Assert.Null(err.SourceLineNumber);
    }

    [Fact]
    public void Error_DefaultConstructor_WithNullMessageCaputersSourceLocation()
    {
        Error err = new(message: null!);
        Assert.Equal("Unknown error", err.Message);
        Assert.NotNull(err.SourceMemberName);
        Assert.NotEmpty(err.SourceMemberName);
        Assert.NotNull(err.SourceFilepath);
        Assert.NotEmpty(err.SourceFilepath);
        Assert.NotNull(err.SourceLineNumber);
        Assert.NotEqual(0, err.SourceLineNumber);
    }

    [Fact]
    public void Error_DefaultConstructor_WithEmptyMessageCaputersSourceLocation()
    {
        Error err = new(string.Empty);
        Assert.NotNull(err.SourceMemberName);
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
        Assert.Null(defaultError.SourceFilepath);
        Assert.Null(defaultError.SourceLineNumber);
        Assert.Null(emptyError.SourceMemberName);
        Assert.Null(emptyError.SourceFilepath);
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
    public void Error_DefaultInArray_IsErrorNone()
    {
        var errors = new Error[3];

        Assert.Equal(Error.None, errors[0]);
        Assert.Equal(Error.None, errors[1]);
        Assert.Equal(Error.None, errors[2]);
        Assert.DoesNotContain(Error.Empty, errors);
    }
}
