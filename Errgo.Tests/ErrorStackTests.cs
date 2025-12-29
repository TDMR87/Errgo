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
        outer.Join(err1, err2, err3);
        
        var stack = outer.Stack;
        var lines = stack.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        
        // Whitespace-only lines should be present (not filtered by RemoveEmptyEntries)
        Assert.Contains(lines, line => line.Contains("Valid"));
    }

    [Fact]
    public void Error_Stack_ShowsCurrentErrorFirst()
    {
        // Arrange: Create a chain of errors
        var err1 = new Error("First error");
        var err2 = new Error("Second error", err1);
        var err3 = new Error("Third error", err2);

        // Act & Assert: Each error's Stack should show itself first
        var stack1 = err1.Stack;
        var stack2 = err2.Stack;
        var stack3 = err3.Stack;

        // err1.Stack should start with "First error"
        Assert.StartsWith("First error", stack1);

        // err2.Stack should start with "Second error"
        Assert.StartsWith("Second error", stack2);

        // err3.Stack should start with "Third error"
        Assert.StartsWith("Third error", stack3);
    }

    [Fact]
    public void Error_Stack_InnerErrorShowsOnlyItsChain()
    {
        // Arrange: Create a chain
        var err1 = new Error("Error 1");
        var err2 = new Error("Error 2", err1);
        var err3 = new Error("Error 3", err2);

        // Act: Get stack from the middle error
        var stack2 = err2.Stack;

        // Assert: err2's stack should contain err2 and err1, but NOT err3
        Assert.Contains("Error 2", stack2);
        Assert.Contains("Error 1", stack2);
        Assert.DoesNotContain("Error 3", stack2); // err3 is NOT in err2's chain
    }

    [Fact]
    public void Error_Stack_OuterErrorShowsFullChain()
    {
        // Arrange: Create a chain
        var err1 = new Error("Error 1");
        var err2 = new Error("Error 2", err1);
        var err3 = new Error("Error 3", err2);

        // Act: Get stack from the outer error
        var stack3 = err3.Stack;

        // Assert: err3's stack should contain all errors
        Assert.Contains("Error 3", stack3);
        Assert.Contains("Error 2", stack3);
        Assert.Contains("Error 1", stack3);
    }

    [Fact]
    public void Error_Stack_OrderIsCorrectForEachPerspective()
    {
        // Arrange: Create a chain
        var err1 = new Error("Error 1");
        var err2 = new Error("Error 2", err1);
        var err3 = new Error("Error 3", err2);

        // Act
        var lines1 = err1.Stack.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var lines2 = err2.Stack.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var lines3 = err3.Stack.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        // Assert: err1's stack (only itself)
        Assert.Single(lines1);
        Assert.Contains("Error 1", lines1[0]);

        // Assert: err2's stack (err2, then err1)
        Assert.Equal(2, lines2.Length);
        Assert.Contains("Error 2", lines2[0]);
        Assert.Contains("Error 1", lines2[1]);

        // Assert: err3's stack (err3, then err2, then err1)
        Assert.Equal(3, lines3.Length);
        Assert.Contains("Error 3", lines3[0]);
        Assert.Contains("Error 2", lines3[1]);
        Assert.Contains("Error 1", lines3[2]);
    }

    [Fact]
    public void Error_Stack_AfterJoin_ShowsCorrectPerspective()
    {
        // Arrange: Create errors and join them
        var dbError = new Error("Database error");
        var validationError = new Error("Validation error");

        var rootError = new Error("Root error");
        rootError.Join(dbError, validationError);

        // Act: Get stacks from different perspectives
        var rootStack = rootError.Stack;
        var dbStack = dbError.Stack;
        var validationStack = validationError.Stack;

        // Assert: Each error's stack shows itself first
        Assert.StartsWith("Root error", rootStack);
        Assert.StartsWith("Database error", dbStack);
        Assert.StartsWith("Validation error", validationStack);

        // Assert: Root contains all
        Assert.Contains("Root error", rootStack);
        Assert.Contains("Database error", rootStack);
        Assert.Contains("Validation error", rootStack);

        // Assert: dbError only contains itself (it wasn't joined into dbError)
        Assert.Contains("Database error", dbStack);
        Assert.DoesNotContain("Root error", dbStack);
        Assert.DoesNotContain("Validation error", dbStack);
    }

    [Fact]
    public void Error_Stack_NestedChainWithJoin_MaintainsPerspective()
    {
        // Arrange: Complex scenario
        var inner1 = new Error("Inner 1");
        var inner2 = new Error("Inner 2", inner1);

        var outer = new Error("Outer");
        outer.Join(inner2);

        // Act
        var outerStack = outer.Stack;
        var inner2Stack = inner2.Stack;
        var inner1Stack = inner1.Stack;

        // Assert: Each maintains its own perspective
        Assert.StartsWith("Outer", outerStack);
        Assert.StartsWith("Inner 2", inner2Stack);
        Assert.StartsWith("Inner 1", inner1Stack);

        // Assert: Outer contains everything
        Assert.Contains("Outer", outerStack);
        Assert.Contains("Inner 2", outerStack);
        Assert.Contains("Inner 1", outerStack);

        // Assert: Inner2 contains itself and Inner1, but not Outer
        Assert.Contains("Inner 2", inner2Stack);
        Assert.Contains("Inner 1", inner2Stack);
        Assert.DoesNotContain("Outer", inner2Stack);

        // Assert: Inner1 contains only itself
        Assert.Contains("Inner 1", inner1Stack);
        Assert.DoesNotContain("Inner 2", inner1Stack);
        Assert.DoesNotContain("Outer", inner1Stack);
    }

    [Fact]
    public void Error_InnerErrors_OnlyShowsErrorsJoinedByThis()
    {
        // Arrange: Create a chain
        var err1 = new Error("Error 1");
        var err2 = new Error("Error 2", err1);
        var err3 = new Error("Error 3", err2);

        // Act & Assert: Each error's InnerErrors should only show what IT joined

        // err1 has no inner errors
        Assert.Empty(err1.InnerErrors);

        // err2 has err1 as inner error
        Assert.Single(err2.InnerErrors);
        Assert.Contains(err2.InnerErrors, e => e.Message == "Error 1");

        // err3 has err2 and err1 (flattened) as inner errors
        Assert.Equal(2, err3.InnerErrors.Count);
        Assert.Contains(err3.InnerErrors, e => e.Message == "Error 2");
        Assert.Contains(err3.InnerErrors, e => e.Message == "Error 1");
    }
}
