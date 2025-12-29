namespace Errgo.Tests;

public class ErrorPerformanceTests
{
    [Fact]
    public void Error_Is_VeryDeepChain_PerformsReasonably()
    {
        Error err = new Error("Level 0");
        for (int i = 1; i < 100; i++)
        {
            err = new Error($"Level {i}", err);
        }
        
        var target = new Error("Level 0");
        
        Assert.True(err.Is(target));
    }

    [Fact]
    public void Error_As_VeryDeepChain_FindsMatch()
    {
        Error err = new Error("Level 0");
        for (int i = 1; i < 100; i++)
        {
            err = new Error($"Level {i}", err);
        }
        
        var target = new Error("Level 50");
        
        Assert.True(err.As(target, out var match));
        Assert.Equal("Level 50", match.Message);
    }

    [Fact]
    public void Error_InnerErrors_VeryDeepChain_ReturnsAllErrors()
    {
        Error err = new Error("Level 0");
        for (int i = 1; i < 100; i++)
        {
            err = new Error($"Level {i}", err);
        }
        
        var innerErrors = err.InnerErrors;
        
        Assert.Equal(99, innerErrors.Count);
        Assert.Equal("Level 98", innerErrors[0].Message);
        Assert.Equal("Level 0", innerErrors[98].Message);
    }

    [Fact]
    public void Error_Stack_VeryDeepChain_GeneratesCompleteStack()
    {
        Error err = new Error("Level 0");
        for (int i = 1; i < 50; i++)
        {
            err = new Error($"Level {i}", err);
        }
        
        var stack = err.Stack;
        
        Assert.Contains("Level 0", stack);
        Assert.Contains("Level 49", stack);
        Assert.Contains("Level 25", stack);
    }

    [Fact]
    public void Error_Join_ManyErrors_MaintainsOrder()
    {
        var err = new Error("Original");
        
        for (int i = 0; i < 100; i++)
        {
            err.Join(new Error($"Error {i}"));
        }
        
        var innerErrors = err.InnerErrors;
        
        Assert.Equal(100, innerErrors.Count);
        Assert.Equal("Error 99", innerErrors[0].Message); // Most recent first
        Assert.Equal("Error 0", innerErrors[99].Message); // Oldest last
    }
}
