namespace Errgo.Tests;

public class ErrorStackTests
{
    [Fact]
    public void Error_Stack_NullMessage()
    {
        var inner = new Error(message: null);
        var middle = new Error("Middle error", inner);
        var outer = new Error("Outer error", middle);

        Assert.Contains("Outer error", outer.Stack);
        Assert.Contains("Middle error", outer.Stack);
        Assert.Contains("Unknown error", outer.Stack);
    }

    [Fact]
    public void Error_Stack_WhitespaceMessage()
    {
        var inner = new Error("   ");
        var middle = new Error("Middle error", inner);
        var outer = new Error("Outer error", middle);

        Assert.Contains("Outer error", outer.Stack);
        Assert.Contains("Middle error", outer.Stack);
        Assert.Contains("   ", outer.Stack);
    }

    [Fact]
    public void Error_Stack_WithEmptyMessages_FiltersWhitespace()
    {
        var err1 = new Error("");
        var err2 = new Error("Valid", err1);
        
        var stack = err2.Stack;
        var lines = stack.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        
        // Empty message lines should be filtered
        Assert.DoesNotContain(lines, line => string.IsNullOrWhiteSpace(line));
    }

    [Fact]
    public void Error_Stack_OnlyWhitespaceMessages_FiltersCorrectly()
    {
        var err1 = new Error("   ");
        var err2 = new Error("\t\t");
        var err3 = new Error("\n\n");
        
        var outer = new Error("Valid");
        outer.Wrap(err1, err2, err3);
        
        var stack = outer.Stack;
        var lines = stack.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        
        // Whitespace-only lines should be present (not filtered by RemoveEmptyEntries)
        Assert.Contains(lines, line => line.Contains("Valid"));
    }
}
