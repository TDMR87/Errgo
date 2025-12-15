namespace Errgo.Tests;

public class ErrorWrapTests
{
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
    public void Error_Wrap_SameErrorTwice_AddsBothInstances()
    {
        var err = new Error("Original");
        var inner = new Error("Inner");

        err.Wrap(inner);
        err.Wrap(inner); // Wrap same error again

        Assert.Equal(2, err.InnerErrors.Count);
        Assert.Equal("Inner", err.InnerErrors[0].Message);
        Assert.Equal("Inner", err.InnerErrors[1].Message);
    }

    [Fact]
    public void Error_Wrap_MixedConstructor_CorrectOrder()
    {
        var err1 = new Error("Error 1");
        var err2 = new Error("Error 2", err1);

        err2.Wrap(new Error("Error 3"));
        err2.Wrap(new Error("Error 4"));

        var innerErrors = err2.InnerErrors;

        // Wrap prepends, constructor appends
        // Expected order: Error 4, Error 3, Error 1
        Assert.Equal(3, innerErrors.Count);
        Assert.Equal("Error 4", innerErrors[0].Message);
        Assert.Equal("Error 3", innerErrors[1].Message);
        Assert.Equal("Error 1", innerErrors[2].Message);
    }

    [Fact]
    public void Error_Wrap_ChainedErrors_FlattenedInInnerErrors()
    {
        var err1 = new Error("Error 1");
        var err2 = new Error("Error 2", err1);
        var err3 = new Error("Error 3");

        var wrapper = new Error("Wrapper");
        wrapper.Wrap(err3, err2); // err2 has its own chain

        var innerErrors = wrapper.InnerErrors;

        // Should flatten: err3, err2, err1
        Assert.Equal(3, innerErrors.Count);
        Assert.Equal("Error 3", innerErrors[0].Message);
        Assert.Equal("Error 2", innerErrors[1].Message);
        Assert.Equal("Error 1", innerErrors[2].Message);
    }

    [Fact]
    public void Error_Wrap_AfterConstructorChain_CombinesBoth()
    {
        var constructorInner = new Error("Constructor Inner");
        var err = new Error("Outer", constructorInner);

        var wrapInner = new Error("Wrap Inner");
        err.Wrap(wrapInner);

        Assert.Equal(2, err.InnerErrors.Count);
        Assert.Equal("Wrap Inner", err.InnerErrors[0].Message);
        Assert.Equal("Constructor Inner", err.InnerErrors[1].Message);
    }

    [Fact]
    public void Error_Wrap_MultipleSequentialWraps_MaintainsChronologicalOrder()
    {
        // Create an error and wrap multiple errors in chronological sequence
        var err = new Error("Root Error");
        
        err.Wrap(new Error("First wrapped"));   // Wrapped at time T1
        err.Wrap(new Error("Second wrapped"));  // Wrapped at time T2
        err.Wrap(new Error("Third wrapped"));   // Wrapped at time T3
        err.Wrap(new Error("Fourth wrapped"));  // Wrapped at time T4

        var innerErrors = err.InnerErrors;

        // InnerErrors should be in reverse chronological order (latest first)
        Assert.Equal(4, innerErrors.Count);
        Assert.Equal("Fourth wrapped", innerErrors[0].Message);   // Most recent (T4)
        Assert.Equal("Third wrapped", innerErrors[1].Message);    // T3
        Assert.Equal("Second wrapped", innerErrors[2].Message);   // T2
        Assert.Equal("First wrapped", innerErrors[3].Message);    // Oldest (T1)
    }

    [Fact]
    public void Error_Wrap_MultipleErrorsInSingleCall_MaintainsParameterOrder()
    {
        var err = new Error("Root Error");
        
        // Wrap multiple errors in a single call
        err.Wrap(
            new Error("Error A"),
            new Error("Error B"),
            new Error("Error C")
        );

        var innerErrors = err.InnerErrors;

        // When wrapped in a single call, errors should maintain their parameter order
        Assert.Equal(3, innerErrors.Count);
        Assert.Equal("Error A", innerErrors[0].Message);
        Assert.Equal("Error B", innerErrors[1].Message);
        Assert.Equal("Error C", innerErrors[2].Message);
    }

    [Fact]
    public void Error_Wrap_ComplexScenario_CorrectChronologicalOrder()
    {
        // Complex scenario: constructor chain + multiple individual wraps + batch wrap
        var constructorInner = new Error("Constructor Inner");
        var err = new Error("Root", constructorInner);

        // First individual wrap
        err.Wrap(new Error("Wrap 1"));
        
        // Second individual wrap
        err.Wrap(new Error("Wrap 2"));
        
        // Batch wrap
        err.Wrap(
            new Error("Batch A"),
            new Error("Batch B")
        );
        
        // Final individual wrap
        err.Wrap(new Error("Wrap 3"));

        var innerErrors = err.InnerErrors;

        // Expected order (latest first):
        // Wrap 3 (most recent)
        // Batch A, Batch B (prepended as a group)
        // Wrap 2
        // Wrap 1
        // Constructor Inner (oldest)
        Assert.Equal(6, innerErrors.Count);
        Assert.Equal("Wrap 3", innerErrors[0].Message);
        Assert.Equal("Batch A", innerErrors[1].Message);
        Assert.Equal("Batch B", innerErrors[2].Message);
        Assert.Equal("Wrap 2", innerErrors[3].Message);
        Assert.Equal("Wrap 1", innerErrors[4].Message);
        Assert.Equal("Constructor Inner", innerErrors[5].Message);
    }

    [Fact]
    public void Error_Wrap_MultipleIndependentErrorChains_MaintainsChronologicalOrder()
    {
        // Create three independent error chains
        // Chain 1: Database errors
        var dbError1 = new Error("Connection timeout");
        var dbError2 = new Error("Connection failed", dbError1);
        var dbError3 = new Error("Database unavailable", dbError2);

        // Chain 2: Validation errors
        var validationError1 = new Error("Field required");
        var validationError2 = new Error("Validation failed", validationError1);

        // Chain 3: Authorization errors
        var authError1 = new Error("Token expired");
        var authError2 = new Error("Authentication failed", authError1);
        var authError3 = new Error("Authorization denied", authError2);

        // Wrap all three chains into a single error in sequence
        var rootError = new Error("Operation failed");
        
        rootError.Wrap(dbError3);          // Wrapped at T1 (with its chain: dbError3, dbError2, dbError1)
        rootError.Wrap(validationError2);  // Wrapped at T2 (with its chain: validationError2, validationError1)
        rootError.Wrap(authError3);        // Wrapped at T3 (with its chain: authError3, authError2, authError1)

        var innerErrors = rootError.InnerErrors;

        // Expected flattened order (latest first, each chain maintains internal order):
        // authError3 (T3), authError2, authError1 (auth chain - 3 errors)
        // validationError2 (T2), validationError1 (validation chain - 2 errors)
        // dbError3 (T1), dbError2, dbError1 (database chain - 3 errors)
        // Total: 8 inner errors (NOT including rootError itself)
        Assert.Equal(8, innerErrors.Count);
        
        // Auth chain (most recent - T3)
        Assert.Equal("Authorization denied", innerErrors[0].Message);
        Assert.Equal("Authentication failed", innerErrors[1].Message);
        Assert.Equal("Token expired", innerErrors[2].Message);
        
        // Validation chain (middle - T2)
        Assert.Equal("Validation failed", innerErrors[3].Message);
        Assert.Equal("Field required", innerErrors[4].Message);
        
        // Database chain (oldest - T1)
        Assert.Equal("Database unavailable", innerErrors[5].Message);
        Assert.Equal("Connection failed", innerErrors[6].Message);
        Assert.Equal("Connection timeout", innerErrors[7].Message);
    }

    [Fact]
    public void Error_Wrap_MultipleChainedErrorsInSingleCall_MaintainsOrder()
    {
        // Create multiple independent error chains
        var chain1Inner = new Error("Chain 1 Inner");
        var chain1 = new Error("Chain 1 Outer", chain1Inner);

        var chain2Inner1 = new Error("Chain 2 Inner 1");
        var chain2Inner2 = new Error("Chain 2 Inner 2", chain2Inner1);
        var chain2 = new Error("Chain 2 Outer", chain2Inner2);

        var chain3 = new Error("Chain 3 Outer");

        // Wrap all chains in a single call
        var rootError = new Error("Root");
        rootError.Wrap(chain1, chain2, chain3);

        var innerErrors = rootError.InnerErrors;

        // Expected order when wrapping multiple chains at once:
        // All of chain1 (outer to inner), then chain2, then chain3
        // Total: 2 + 3 + 1 = 6 inner errors (NOT including rootError)
        Assert.Equal(6, innerErrors.Count);
        Assert.Equal("Chain 1 Outer", innerErrors[0].Message);
        Assert.Equal("Chain 1 Inner", innerErrors[1].Message);
        Assert.Equal("Chain 2 Outer", innerErrors[2].Message);
        Assert.Equal("Chain 2 Inner 2", innerErrors[3].Message);
        Assert.Equal("Chain 2 Inner 1", innerErrors[4].Message);
        Assert.Equal("Chain 3 Outer", innerErrors[5].Message);
    }

    [Fact]
    public void Error_Wrap_MixedSequentialAndBatchWrapsWithChains_CorrectOrder()
    {
        // Realistic scenario: wrapping multiple error chains at different times
        var rootError = new Error("Request failed");

        // T1: Wrap a simple error
        rootError.Wrap(new Error("Network timeout"));

        // T2: Wrap an error with a chain (2 errors total)
        var validationInner = new Error("Email invalid");
        var validationOuter = new Error("Validation failed", validationInner);
        rootError.Wrap(validationOuter);

        // T3: Wrap multiple chains at once (3 errors total)
        var dbInner = new Error("Table not found");
        var dbOuter = new Error("Query failed", dbInner);
        var cacheError = new Error("Cache miss");
        rootError.Wrap(dbOuter, cacheError);

        // T4: Wrap another simple error
        rootError.Wrap(new Error("Retry limit exceeded"));

        var innerErrors = rootError.InnerErrors;

        // Expected chronological order (latest first):
        // T4: Retry limit exceeded (1 error)
        // T3: Query failed, Table not found, Cache miss (3 errors)
        // T2: Validation failed, Email invalid (2 errors)
        // T1: Network timeout (1 error)
        // Total: 7 inner errors (NOT including rootError)
        Assert.Equal(7, innerErrors.Count);
        Assert.Equal("Retry limit exceeded", innerErrors[0].Message);     // T4
        Assert.Equal("Query failed", innerErrors[1].Message);             // T3 batch
        Assert.Equal("Table not found", innerErrors[2].Message);          // T3 batch chain
        Assert.Equal("Cache miss", innerErrors[3].Message);               // T3 batch
        Assert.Equal("Validation failed", innerErrors[4].Message);        // T2
        Assert.Equal("Email invalid", innerErrors[5].Message);            // T2 chain
        Assert.Equal("Network timeout", innerErrors[6].Message);          // T1
    }
}
