namespace Errgo.Tests;

public class ErrorJoinTests
{
    [Fact]
    public void Error_Join_CanChainErrors()
    {
        Func<(int, Error)> func1 = () => (default, new Error("Error 1"));
        Func<(string?, Error)> func2 = () => (default, new Error("Error 2"));
        Func<(bool?, Error)> func3 = () => (null, new Error("Error 3"));

        var errJoiner = Error.Empty;

        var (val, err) = func1();
        if (err) errJoiner.Join(new Error("Func1 failed", err));

        (var str, err) = func2();
        if (err) errJoiner.Join(new Error("Func2 failed", err));

        (var flag, err) = func3();
        if (err) errJoiner.Join(new Error("Func3 failed", err));

        // Verify the error
        Assert.True(errJoiner);

        var stack = errJoiner.Stack;
        Assert.Contains("Func1 failed", stack);
        Assert.Contains("Func2 failed", stack);
        Assert.Contains("Func3 failed", stack);
        Assert.Contains("Error 1", stack);
        Assert.Contains("Error 2", stack);
        Assert.Contains("Error 3", stack);
    }

    [Fact]
    public void Error_Join_WithNullArray_DoesNotThrow()
    {
        var err = new Error("Original");
        err.Join(null!);
        Assert.Equal("Original", err.Message);
        Assert.Empty(err.InnerErrors);
    }

    [Fact]
    public void Error_Join_WithEmptyArray_DoesNotThrow()
    {
        var err = new Error("Original");
        err.Join(Array.Empty<Error>());
        Assert.Equal("Original", err.Message);
        Assert.Empty(err.InnerErrors);
    }

    [Fact]
    public void Error_Join_WithErrorNoneInArray_IgnoresErrorNone()
    {
        var err = new Error("Original");
        var validError = new Error("Valid");
        err.Join(Error.None, validError, Error.None);

        Assert.Contains("Valid", err.Stack);
        Assert.Single(err.InnerErrors);
    }

    [Fact]
    public void Error_Join_OnErrorNone_DoesNothing()
    {
        var err = Error.None;
        err.Join(new Error("Should not appear"));
        Assert.False(err);
    }

    [Fact]
    public void Error_Join_SameErrorTwice_AddsBothInstances()
    {
        var err = new Error("Original");
        var inner = new Error("Inner");

        err.Join(inner);
        err.Join(inner); // Join same error again

        Assert.Equal(2, err.InnerErrors.Count);
        Assert.Equal("Inner", err.InnerErrors[0].Message);
        Assert.Equal("Inner", err.InnerErrors[1].Message);
    }

    [Fact]
    public void Error_Join_MixedConstructor_CorrectOrder()
    {
        var err1 = new Error("Error 1");
        var err2 = new Error("Error 2", err1);

        err2.Join(new Error("Error 3"));
        err2.Join(new Error("Error 4"));

        var innerErrors = err2.InnerErrors;

        // Join prepends, constructor appends
        // Expected order: Error 4, Error 3, Error 1
        Assert.Equal(3, innerErrors.Count);
        Assert.Equal("Error 4", innerErrors[0].Message);
        Assert.Equal("Error 3", innerErrors[1].Message);
        Assert.Equal("Error 1", innerErrors[2].Message);
    }

    [Fact]
    public void Error_Join_ChainedErrors_FlattenedInInnerErrors()
    {
        var err1 = new Error("Error 1");
        var err2 = new Error("Error 2", err1);
        var err3 = new Error("Error 3");

        var joiner = new Error("Joiner");
        joiner.Join(err3, err2); // err2 has its own chain

        var innerErrors = joiner.InnerErrors;

        // Should flatten: err3, err2, err1
        Assert.Equal(3, innerErrors.Count);
        Assert.Equal("Error 3", innerErrors[0].Message);
        Assert.Equal("Error 2", innerErrors[1].Message);
        Assert.Equal("Error 1", innerErrors[2].Message);
    }

    [Fact]
    public void Error_Join_AfterConstructorChain_CombinesBoth()
    {
        var constructorInner = new Error("Constructor Inner");
        var err = new Error("Outer", constructorInner);

        var joinInner = new Error("Join Inner");
        err.Join(joinInner);

        Assert.Equal(2, err.InnerErrors.Count);
        Assert.Equal("Join Inner", err.InnerErrors[0].Message);
        Assert.Equal("Constructor Inner", err.InnerErrors[1].Message);
    }

    [Fact]
    public void Error_Join_MultipleSequentialJoins_MaintainsChronologicalOrder()
    {
        // Create an error and join multiple errors in chronological sequence
        var err = new Error("Root Error");
        
        err.Join(new Error("First joined"));   // Joined at time T1
        err.Join(new Error("Second joined"));  // Joined at time T2
        err.Join(new Error("Third joined"));   // Joined at time T3
        err.Join(new Error("Fourth joined"));  // Joined at time T4

        var innerErrors = err.InnerErrors;

        // InnerErrors should be in reverse chronological order (latest first)
        Assert.Equal(4, innerErrors.Count);
        Assert.Equal("Fourth joined", innerErrors[0].Message);   // Most recent (T4)
        Assert.Equal("Third joined", innerErrors[1].Message);    // T3
        Assert.Equal("Second joined", innerErrors[2].Message);   // T2
        Assert.Equal("First joined", innerErrors[3].Message);    // Oldest (T1)
    }

    [Fact]
    public void Error_Join_MultipleErrorsInSingleCall_MaintainsParameterOrder()
    {
        var err = new Error("Root Error");
        
        // Join multiple errors in a single call
        err.Join(
            new Error("Error A"),
            new Error("Error B"),
            new Error("Error C")
        );

        var innerErrors = err.InnerErrors;

        // When joined in a single call, errors should maintain their parameter order
        Assert.Equal(3, innerErrors.Count);
        Assert.Equal("Error A", innerErrors[0].Message);
        Assert.Equal("Error B", innerErrors[1].Message);
        Assert.Equal("Error C", innerErrors[2].Message);
    }

    [Fact]
    public void Error_Join_ComplexScenario_CorrectChronologicalOrder()
    {
        // Complex scenario: constructor chain + multiple individual joins + batch join
        var constructorInner = new Error("Constructor Inner");
        var err = new Error("Root", constructorInner);

        // First individual join
        err.Join(new Error("Join 1"));
        
        // Second individual join
        err.Join(new Error("Join 2"));
        
        // Batch join
        err.Join(
            new Error("Batch A"),
            new Error("Batch B")
        );
        
        // Final individual join
        err.Join(new Error("Join 3"));

        var innerErrors = err.InnerErrors;

        // Expected order (latest first):
        // Join 3 (most recent)
        // Batch A, Batch B (prepended as a group)
        // Join 2
        // Join 1
        // Constructor Inner (oldest)
        Assert.Equal(6, innerErrors.Count);
        Assert.Equal("Join 3", innerErrors[0].Message);
        Assert.Equal("Batch A", innerErrors[1].Message);
        Assert.Equal("Batch B", innerErrors[2].Message);
        Assert.Equal("Join 2", innerErrors[3].Message);
        Assert.Equal("Join 1", innerErrors[4].Message);
        Assert.Equal("Constructor Inner", innerErrors[5].Message);
    }

    [Fact]
    public void Error_Join_MultipleIndependentErrorChains_MaintainsChronologicalOrder()
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

        // Join all three chains into a single error in sequence
        var rootError = new Error("Operation failed");
        
        rootError.Join(dbError3);          // Joined at T1 (with its chain: dbError3, dbError2, dbError1)
        rootError.Join(validationError2);  // Joined at T2 (with its chain: validationError2, validationError1)
        rootError.Join(authError3);        // Joined at T3 (with its chain: authError3, authError2, authError1)

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
    public void Error_Join_MultipleChainedErrorsInSingleCall_MaintainsOrder()
    {
        // Create multiple independent error chains
        var chain1Inner = new Error("Chain 1 Inner");
        var chain1 = new Error("Chain 1 Outer", chain1Inner);

        var chain2Inner1 = new Error("Chain 2 Inner 1");
        var chain2Inner2 = new Error("Chain 2 Inner 2", chain2Inner1);
        var chain2 = new Error("Chain 2 Outer", chain2Inner2);

        var chain3 = new Error("Chain 3 Outer");

        // Join all chains in a single call
        var rootError = new Error("Root");
        rootError.Join(chain1, chain2, chain3);

        var innerErrors = rootError.InnerErrors;

        // Expected order when joining multiple chains at once:
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
    public void Error_Join_MixedSequentialAndBatchJoinsWithChains_CorrectOrder()
    {
        // Realistic scenario: joining multiple error chains at different times
        var rootError = new Error("Request failed");

        // T1: Join a simple error
        rootError.Join(new Error("Network timeout"));

        // T2: Join an error with a chain (2 errors total)
        var validationInner = new Error("Email invalid");
        var validationOuter = new Error("Validation failed", validationInner);
        rootError.Join(validationOuter);

        // T3: Join multiple chains at once (3 errors total)
        var dbInner = new Error("Table not found");
        var dbOuter = new Error("Query failed", dbInner);
        var cacheError = new Error("Cache miss");
        rootError.Join(dbOuter, cacheError);

        // T4: Join another simple error
        rootError.Join(new Error("Retry limit exceeded"));

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
