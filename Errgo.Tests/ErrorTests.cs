namespace Errgo.Tests;

public class ErrorTests
{
    // Sentinel errors
    public static readonly Error NotFound = new("Item not found");

    // Helper functions to generate errors
    private static (object?, Error) GetDatabaseError() => (null, new Error("Database connection failed"));
    private static (object?, Error) GetErrorWithoutMessage() => (null, new Error(message: null));

    [Fact]
    public void Error_IdenticalMemberValues_AreEqual()
    {
        Error err1 = new("error");
        Error err2 = new("error");
        Assert.Equal(err1, err2);
        Assert.True(err1 == err2);
        Assert.True(err1.Equals(err2));
    }

    [Fact]
    public void Error_None_AreEqual()
    {
        Error err = Error.None;
        Assert.Equal(err, Error.None);
        Assert.True(err == Error.None);
        Assert.True(err.Equals(Error.None));
    }

    [Fact]
    public void Error_WithoutMessage_And_ErrorNone_AreNotEqual()
    {
        Assert.NotEqual(new Error(), Error.None);
        Assert.False(new Error() == Error.None);
        Assert.False(new Error().Equals(Error.None));
    }

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
    public void Error_None_ReturnsFalse()
    {
        Assert.False(Error.None);
    }

    [Fact]
    public void Error_SentinelError_ValueEquality()
    {
        static (object?, Error) GetItemById(int id)
        {
            return (null, NotFound);
        }

        var (item, err) = GetItemById(123);
        
        Assert.Equal(NotFound, err);
        Assert.True(err == NotFound);
        Assert.True(err.Equals(NotFound));
    }

    [Fact]
    public void Error_SentinelError_ChainingConstructor()
    {
        var err = new Error($"Some error happened", NotFound);
        Assert.True(err);
        Assert.Contains("Some error happened", err.Stack);
        Assert.Contains("Item not found", err.Stack);
    }

    [Fact]
    public void Error_SentinelError_ChainingWrap()
    {
        var err = new Error($"Some error happened", NotFound);
        var wrapper = Error.Empty;
        wrapper.Wrap(err);
        Assert.True(wrapper);
        Assert.Contains("Some error happened", wrapper.Stack);
        Assert.Contains("Item not found", wrapper.Stack);
    }

    [Fact]
    public void Error_SentinelError_ChainingConstructor2()
    {
        var itemId = 123;

        static (object?, Error) GetItemById(int id)
        {
            return (null, new($"Failed to get item with id {id}", NotFound));
        }

        var (_, err) = GetItemById(itemId);
        Assert.Contains($"Failed to get item with id {itemId}", err.Stack);
        Assert.Contains($"Item not found", err.Stack);
    }

    [Fact]
    public void Error_None_StackReturnsEmptyString()
    {
        Assert.Equal(string.Empty, Error.None.Stack);
    }

    [Fact]
    public void Error_None_InnerErrorsReturnsEmptyList()
    {
        var errors = Error.None.InnerErrors;
        Assert.Empty(errors);
    }

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
    public void Error_Message_NullMessageReturnsUnknownError()
    {
        var inner = new Error(message: null);
        var middle = new Error("Middle error", inner);
        var outer = new Error("Outer error", middle);

        Assert.Equal(2, outer.InnerErrors.Count);
        Assert.Equal("Outer error", outer.Message);
        Assert.Equal("Middle error", outer.InnerErrors[0].Message);
        Assert.Equal("Unknown error", outer.InnerErrors[1].Message);
    }

    [Fact]
    public void Error_Is_ReturnsFalseWhenErrorNone()
    {
        var err = new Error("Some error");
        Assert.False(err.Is(Error.None));
    }

    [Fact]
    public void Error_Is_ReturnsTrueWhenErrorInChain()
    {
        var inner = NotFound;
        var middle = new Error("Middle error", inner);
        var outer = new Error("Outer error", middle);

        Assert.True(outer.Is(NotFound));
        Assert.True(middle.Is(NotFound));
        Assert.True(inner.Is(NotFound));
    }

    [Fact]
    public void Error_Is_ReturnsFalseWhenErrorNotInChain()
    {
        var differentError = new Error("Different error");
        var err = new Error("Some error", NotFound);
        Assert.False(err.Is(differentError));
    }

    [Fact]
    public void Error_Is_WithAnyTargetReturnsFalse()
    {
        Assert.False(Error.None.Is(NotFound));
        Assert.False(Error.None.Is(new Error("Some error")));
    }

    [Fact]
    public void Error_Is_ErrorNoneReturnsTrue()
    {
        var err = Error.None;
        Assert.True(err.Is(Error.None));
        Assert.Equal(err, Error.None);
    }

    [Fact]
    public void Error_As_FindsErrorInChain()
    {
        var inner = NotFound;
        var middle = new Error("Middle error", inner);
        var outer = new Error("Outer error", middle);

        Assert.True(outer.As(NotFound, out var match));
        Assert.Equal(NotFound.Message, match.Message);
        
        Assert.True(middle.As(NotFound, out match));
        Assert.Equal(NotFound.Message, match.Message);
        
        Assert.True(inner.As(NotFound, out match));
        Assert.Equal(NotFound.Message, match.Message);
    }

    [Fact]
    public void Error_As_DoesNotFindErrorInChain()
    {
        var differentError = new Error("Different error");
        var err = new Error("Some error", NotFound);

        Assert.False(err.As(differentError, out var match));
        Assert.Equal(Error.None, match);
    }

    [Fact]
    public void Error_As_ReturnsErrorWithSourceLocation()
    {
        var (_, err) = GetDatabaseError();
        
        // Create a sentinel error without source location for matching
        var dbErrorSentinel = new Error("Database connection failed");
        
        // Use As to find the actual error in the chain with source location
        Assert.True(err.As(dbErrorSentinel, out var match));
        Assert.Equal("Database connection failed", match.Message);
        Assert.Equal("GetDatabaseError", match.MemberName);
        Assert.Contains("ErrorTests.cs", match.FilePath);
        Assert.True(match.LineNumber > 0);
    }

    [Fact]
    public void Error_As_WithErrorNoneReturnsFalse()
    {
        Assert.False(Error.None.As(NotFound, out var match));
        Assert.Equal(Error.None, match);
        
        Assert.False(Error.None.As(new Error("Some error"), out match));
        Assert.Equal(Error.None, match);
    }

    [Fact]
    public void Error_CapturesSourceLocation()
    {
        var (_, err) = GetDatabaseError();

        // Error properties contain the full details
        Assert.Equal("Database connection failed", err.Message);
        Assert.Equal("GetDatabaseError", err.MemberName);
        Assert.Contains("ErrorTests.cs", err.FilePath);
        Assert.True(err.LineNumber > 0);
        
        // Stack includes location info
        Assert.Contains("Database connection failed", err.Stack);
        Assert.Contains("GetDatabaseError", err.Stack);
        Assert.Contains("ErrorTests.cs", err.Stack);
    }

    [Fact]
    public void Error_Wrap_CanChainErrors()
    {
        Func<(int, Error)> func1 = () => (default, new Error("Error 1"));
        Func<(string?, Error)> func2 = () => (default, new Error("Error 2"));
        Func<(bool?, Error)> func3 = () => (null, new Error("Error 3"));

        var errWrapper = Error.Empty;

        var (val, err) = func1();
        if (err) errWrapper.Wrap(new Error("Func1 failed", err));

        (var str, err) = func2();
        if (err) errWrapper.Wrap(new Error("Func2 failed", err));

        (var flag, err) = func3();
        if (err) errWrapper.Wrap(new Error("Func3 failed", err));

        // Verify the error
        Assert.True(errWrapper);
        
        var stack = errWrapper.Stack;
        Assert.Contains("Func1 failed", stack);
        Assert.Contains("Func2 failed", stack);
        Assert.Contains("Func3 failed", stack);
        Assert.Contains("Error 1", stack);
        Assert.Contains("Error 2", stack);
        Assert.Contains("Error 3", stack);
    }

    [Fact]
    public void Error_WithoutMessage_ShowsSourceLocationInStack()
    {
        var (_, err) = GetErrorWithoutMessage();
        
        // Even without a message, source location should be visible
        Assert.Equal("Unknown error", err.Message);
        Assert.Equal("GetErrorWithoutMessage", err.MemberName);
        Assert.Contains("ErrorTests.cs", err.FilePath);
        Assert.True(err.LineNumber > 0);
        
        var stack = err.Stack;
        Assert.Contains("Unknown error at GetErrorWithoutMessage", stack);
        Assert.Contains("ErrorTests.cs", stack);
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
        Assert.Contains("ErrorTests.cs", err.Stack);
    }

    #region New Edge Case Tests

    [Fact]
    public void Error_Empty_IsAnError()
    {
        var err = Error.Empty;
        Assert.True(err);
        Assert.NotEqual(Error.None, err);
    }

    [Fact]
    public void Error_Empty_HasEmptyMessage()
    {
        var err = Error.Empty;
        Assert.Equal(string.Empty, err.Message);
    }

    [Fact]
    public void Error_Details_MatchesToString()
    {
        var err = new Error("Test error");
        Assert.Equal(err.ToString(), err.Details);
    }

    [Fact]
    public void Error_Wrap_WithNullArray_DoesNotThrow()
    {
        var err = new Error("Original");
        err.Wrap(null!);
        Assert.Equal("Original", err.Message);
        Assert.Empty(err.InnerErrors);
    }

    [Fact]
    public void Error_Wrap_WithEmptyArray_DoesNotThrow()
    {
        var err = new Error("Original");
        err.Wrap(Array.Empty<Error>());
        Assert.Equal("Original", err.Message);
        Assert.Empty(err.InnerErrors);
    }

    [Fact]
    public void Error_Wrap_WithErrorNoneInArray_IgnoresErrorNone()
    {
        var err = new Error("Original");
        var validError = new Error("Valid");
        err.Wrap(Error.None, validError, Error.None);
        
        Assert.Contains("Valid", err.Stack);
        Assert.Single(err.InnerErrors);
    }

    [Fact]
    public void Error_Wrap_OnErrorNone_DoesNothing()
    {
        var err = Error.None;
        err.Wrap(new Error("Should not appear"));
        Assert.False(err);
    }

    [Fact]
    public void Error_Constructor_WithErrorNoneAsInner_NoChain()
    {
        var err = new Error("Outer", Error.None);
        Assert.Equal("Outer", err.Message);
        Assert.Empty(err.InnerErrors);
    }

    [Fact]
    public void Error_ToString_WithNoSourceLocation_ReturnsMessageOnly()
    {
        var err = new Error("Test", "", "", 0);
        Assert.Equal("Test", err.ToString());
    }

    [Fact]
    public void Error_GetHashCode_ForErrorNone_ReturnsZero()
    {
        Assert.Equal(0, Error.None.GetHashCode());
    }

    [Fact]
    public void Error_GetHashCode_SameMessage_SameHash()
    {
        var err1 = new Error("Same");
        var err2 = new Error("Same");
        Assert.Equal(err1.GetHashCode(), err2.GetHashCode());
    }

    [Fact]
    public void Error_GetHashCode_DifferentMessage_DifferentHash()
    {
        var err1 = new Error("Message 1");
        var err2 = new Error("Message 2");
        Assert.NotEqual(err1.GetHashCode(), err2.GetHashCode());
    }

    [Fact]
    public void Error_EqualityOperator_ConsistentWithEquals()
    {
        var err1 = new Error("Test");
        var err2 = new Error("Test");
        var err3 = new Error("Different");

        Assert.True(err1 == err2);
        Assert.True(err1.Equals(err2));
        
        Assert.False(err1 == err3);
        Assert.False(err1.Equals(err3));
    }

    [Fact]
    public void Error_InnerErrors_DeeplyNestedChains()
    {
        var err1 = new Error("Error 1");
        var err2 = new Error("Error 2", err1);
        var err3 = new Error("Error 3", err2);
        var err4 = new Error("Error 4", err3);

        var innerErrors = err4.InnerErrors;
        Assert.Equal(3, innerErrors.Count);
        Assert.Equal("Error 3", innerErrors[0].Message);
        Assert.Equal("Error 2", innerErrors[1].Message);
        Assert.Equal("Error 1", innerErrors[2].Message);
    }

    [Fact]
    public void Error_Wrap_MultipleTimesPrepends()
    {
        var err = new Error("Original");
        err.Wrap(new Error("First wrap"));
        err.Wrap(new Error("Second wrap"));

        var stack = err.Stack;
        var lines = stack.Split(Environment.NewLine);

        // Original is the outer error (appears first in Stack)
        Assert.Contains("Original", lines[0]);
        Assert.Contains("Second wrap", lines[1]);
        Assert.Contains("First wrap", lines[2]);
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
    public void Error_NotEqual_WhenOnlyOneIsErrorNone()
    {
        var err = new Error("Test");
        Assert.False(err.Equals(Error.None));
        Assert.False(Error.None.Equals(err));
        Assert.False(err == Error.None);
        Assert.False(Error.None == err);
    }

    [Fact]
    public void Error_ImplicitBoolConversion_ConsistentBehavior()
    {
        var errorWithMessage = new Error("Test");
        var errorNone = Error.None;
        var errorEmpty = Error.Empty;

        Assert.True(errorWithMessage);
        Assert.False(errorNone);
        Assert.True(errorEmpty); // Empty is still an error
    }

    [Fact]
    public void Error_InnerErrors_PreservesOrder()
    {
        var err1 = new Error("First");
        var err2 = new Error("Second");
        var err3 = new Error("Third");
        
        var wrapper = new Error("Wrapper");
        wrapper.Wrap(err1, err2, err3);

        var innerErrors = wrapper.InnerErrors;
        Assert.Equal(3, innerErrors.Count);  // Only the wrapped errors, not "Wrapper" itself
        Assert.Equal("First", innerErrors[0].Message);
        Assert.Equal("Second", innerErrors[1].Message);
        Assert.Equal("Third", innerErrors[2].Message);
    }

    #endregion
}
