namespace Errgo.Tests;

public class ErrorToStringTests
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
        var err = new Error("message", "", "", 0);
        Assert.Equal("message", err.ToString());
    }

    [Fact]
    public void Error_ToString_WithNullMessage_ReturnsDefaultErrorMessageAndSourceLocationInfo()
    {
        var filename = nameof(ErrorToStringTests) + ".cs";
        var err = new Error(memberName: "DoWork", lineNumber: 42);
        Assert.Equal($"Unknown error at DoWork in {filename}:line 42", err.ToString());
    }

    [Fact]
    public void Error_ToString_WithMemberNameOnly_ReturnsMemberOnly()
    {
        var err = new Error("Failed", memberName: "DoWork", filePath: null, lineNumber: null);
        Assert.Equal("Failed at DoWork", err.ToString());
    }

    [Fact]
    public void Error_ToString_WithFilePathOnly_ReturnsFileOnly()
    {
        var filename = nameof(ErrorToStringTests) + ".cs";
        var err = new Error("Failed", memberName: null, lineNumber: null);
        Assert.StartsWith($"Failed in {filename}", err.ToString());
    }

    [Fact]
    public void Error_ToString_WithLineNumberOnly_ReturnsLineOnly()
    {
        var err = new Error("Failed", memberName: null, filePath: null, lineNumber: 42);
        Assert.Equal("Failed:line 42", err.ToString());
    }

    [Fact]
    public void Error_ToString_ForErrorNone_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, Error.None.ToString());
    }

    [Fact]
    public void Error_ToString_WithWhitespaceMemberNameAndFilePath_TreatsThemAsMissing()
    {
        var err = new Error("Failed", memberName: "   ", filePath: " ", lineNumber: 42);
        Assert.Equal("Failed:line 42", err.ToString());
    }

    [Fact]
    public void Error_ToString_OnlyUsesFileNameFromFilePath()
    {
        var filename = nameof(ErrorToStringTests) + ".cs";
        var err = new Error("Failed", memberName: "DoWork");
        Assert.StartsWith($"Failed at DoWork in {filename}", err.ToString());
    }

    [Fact]
    public void Error_ToString_InterpolationUsesErrorMessage()
    {
        var err = new Error("Something went wrong");
        Assert.Contains("Something went wrong", $"{err}");
    }
}
