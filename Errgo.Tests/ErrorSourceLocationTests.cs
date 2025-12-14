namespace Errgo.Tests;

public class ErrorSourceLocationTests
{
    private static (object?, Error) GetDatabaseError() => (null, new Error("Database connection failed"));
    private static (object?, Error) GetErrorWithoutMessage() => (null, new Error(message: null));

    [Fact]
    public void Error_CapturesSourceLocation()
    {
        var (_, err) = GetDatabaseError();

        // Error properties contain the full details
        Assert.Equal("Database connection failed", err.Message);
        Assert.Equal("GetDatabaseError", err.SourceMemberName);
        Assert.Contains("ErrorSourceLocationTests.cs", err.SourceFilePath);
        Assert.True(err.SourceLineNumber > 0);
        
        // Stack includes location info
        Assert.Contains("Database connection failed", err.Stack);
        Assert.Contains("GetDatabaseError", err.Stack);
        Assert.Contains("ErrorSourceLocationTests.cs", err.Stack);
    }

    [Fact]
    public void Error_WithoutMessage_ShowsSourceLocationInStack()
    {
        var (_, err) = GetErrorWithoutMessage();
        
        // Even without a message, source location should be visible
        Assert.Equal("Unknown error", err.Message);
        Assert.Equal("GetErrorWithoutMessage", err.SourceMemberName);
        Assert.Contains("ErrorSourceLocationTests.cs", err.SourceFilePath);
        Assert.True(err.SourceLineNumber > 0);
        
        var stack = err.Stack;
        Assert.Contains("Unknown error at GetErrorWithoutMessage", stack);
        Assert.Contains("ErrorSourceLocationTests.cs", stack);
    }

    [Fact]
    public void Error_WithoutMessage_InChain_ShowsSourceLocationInStack()
    {
        var (val, innerErr) = GetErrorWithoutMessage();
        var err = new Error("Operation failed", innerErr);
        
        // Outer error with message should be shown normally
        Assert.Contains("Operation failed", err.Stack);
        
        // Inner error without message should still show its source location
        Assert.Contains("Unknown error at GetErrorWithoutMessage", err.Stack);
        Assert.Contains("ErrorSourceLocationTests.cs", err.Stack);
    }
}
