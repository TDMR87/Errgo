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
    public void Error_ToString_WithMemberNameFilePathAndLineNumber_ReturnsFullDetails()
    {
        var err = new Error("Failed", memberName: "DoWork", filePath: @"C:\\src\\Errgo\\Error.cs", lineNumber: 42);
        Assert.Equal("Failed at DoWork in Error.cs:line 42", err.ToString());
    }

    [Fact]
    public void Error_ToString_WithMemberNameAndFilePath_ReturnsMemberAndFile()
    {
        var err = new Error("Failed", memberName: "DoWork", filePath: @"C:\\src\\Errgo\\Error.cs", lineNumber: 0);
        Assert.Equal("Failed at DoWork in Error.cs", err.ToString());
    }

    [Fact]
    public void Error_ToString_WithMemberNameOnly_ReturnsMemberOnly()
    {
        var err = new Error("Failed", memberName: "DoWork", filePath: null, lineNumber: null);
        Assert.Equal("Failed at DoWork", err.ToString());
    }

    [Fact]
    public void Error_ToString_WithFilePathAndLineNumberButNoMemberName_ShouldStillShowAvailableSourceLocation()
    {
        var err = new Error("Failed", memberName: null, filePath: @"C:\\src\\Errgo\\Error.cs", lineNumber: 42);
        Assert.Contains("Failed", err.ToString());
        Assert.Contains("Error.cs", err.ToString());
        Assert.Contains("42", err.ToString());
    }

    [Fact]
    public void Error_ToString_WithFilePathOnly_ReturnsFileOnly()
    {
        var err = new Error("Failed", memberName: null, filePath: @"C:\\src\\Errgo\\Error.cs", lineNumber: null);
        Assert.Equal("Failed in Error.cs", err.ToString());
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
        var err = new Error("Failed", 
            memberName: "DoWork", 
            filePath: @"C:\\src\\Errgo\\Nested\\Error.cs", 
            lineNumber: 42);

        Assert.Contains("Error.cs", err.ToString());
        Assert.DoesNotContain("Nested", err.ToString());
        Assert.DoesNotContain(@"C:\src\Errgo", err.ToString());
    }

    [Fact]
    public void Error_ToString_InterpolationUsesErrorMessage()
    {
        var err = new Error("Something went wrong");
        Assert.Contains("Something went wrong", $"{err}");
    }
}
