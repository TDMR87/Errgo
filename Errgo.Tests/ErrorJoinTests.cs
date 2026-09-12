using System.Reflection;

namespace Errgo.Tests;

public class ErrorJoinTests
{
    [Fact]
    public void Error_Join_WithNullArray_DoesNotThrow()
    {
        var err = new Error("Original");
        err = Error.Join(err, null!);
        Assert.Equal("Original", err.Message);
        Assert.Empty(err.InnerErrors);
    }

    [Fact]
    public void Error_Join_WithEmptyArray_DoesNotThrow()
    {
        var err = new Error("Original");
        err = Error.Join(err, Array.Empty<Error?>());
        Assert.Equal("Original", err.Message);
        Assert.Empty(err.InnerErrors);
    }

    [Fact]
    public void Error_Join_NonErrors_EqualsFalse()
    {
        var err = Error.Join(Error.None, Array.Empty<Error?>());
        Assert.False(err);
    }

    [Fact]
    public void Error_Join_WithErrorNoneInArray_IgnoresErrorNone()
    {
        var err = new Error("Original");
        var validError = new Error("Valid");
        err = Error.Join(err, Error.None, validError, Error.None);

        Assert.Contains("Valid", err.Stack);
        Assert.Single(err.InnerErrors);
    }

    [Fact]
    public void Error_Join_OnErrorNone_ReturnsFirstValidJoinedError()
    {
        var err = Error.None;
        err = Error.Join(err, new Error("err"));
        Assert.True(err);
        Assert.Equal("err", err.Message);
        Assert.Empty(err.InnerErrors);
    }

    [Fact]
    public void Error_Join_SameErrorTwice_AddsBothInstances()
    {
        var err = new Error("Original");
        var inner = new Error("Inner");

        err = Error.Join(err, inner);
        err = Error.Join(err, inner); // Join same error again

        Assert.Equal(2, err.InnerErrors.Count);
        Assert.Equal("Inner", err.InnerErrors.ElementAt(0).Message);
        Assert.Equal("Inner", err.InnerErrors.ElementAt(1).Message);
    }

    [Fact]
    public void Error_Join_MixedConstructor_CorrectOrder()
    {
        var err1 = new Error("Error 1");
        var err2 = new Error("Error 2", err1);

        err2 = Error.Join(err2, new Error("Error 3"));
        err2 = Error.Join(err2, new Error("Error 4"));

        var innerErrors = err2.InnerErrors;

        // Join preserves existing chain order and appends joined errors
        Assert.Equal(3, innerErrors.Count);
        Assert.Equal("Error 1", innerErrors.ElementAt(0).Message);
        Assert.Equal("Error 3", innerErrors.ElementAt(1).Message);
        Assert.Equal("Error 4", innerErrors.ElementAt(2).Message);
    }

    [Fact]
    public void Error_Join_ChainedErrors_FlattenedInInnerErrors()
    {
        var err1 = new Error("Error 1");
        var err2 = new Error("Error 2", err1);
        var err3 = new Error("Error 3");

        var wrapper = new Error("wrapper");
        wrapper = Error.Join(wrapper, err3, err2); // err2 has its own inner errors

        // Should flatten: err3, err2, err1
        Assert.True(wrapper);
        Assert.Equal("wrapper", wrapper.Message);
        Assert.Equal(3, wrapper.InnerErrors.Count);
        Assert.Equal("Error 3", wrapper.InnerErrors.ElementAt(0).Message);
        Assert.Equal("Error 2", wrapper.InnerErrors.ElementAt(1).Message);
        Assert.Equal("Error 1", wrapper.InnerErrors.ElementAt(2).Message);
    }

    [Fact]
    public void Error_Join_AfterConstructorChain_CombinesBoth()
    {
        var constructorInner = new Error("Constructor Inner");
        var err = new Error("Outer", constructorInner);

        var joinInner = new Error("Join Inner");
        err = Error.Join(err, joinInner);

        Assert.Equal(2, err.InnerErrors.Count);
        Assert.Equal("Constructor Inner", err.InnerErrors.ElementAt(0).Message);
        Assert.Equal("Join Inner", err.InnerErrors.ElementAt(1).Message);
    }

    [Fact]
    public void Error_Join_MultipleSequentialJoins_MaintainsChronologicalOrder()
    {
        // Create an error and join multiple errors in chronological sequence
        var err = new Error("Root Error");
        
        err = Error.Join(err, new Error("First joined"));   // Joined at time T1
        err = Error.Join(err, new Error("Second joined"));  // Joined at time T2
        err = Error.Join(err, new Error("Third joined"));   // Joined at time T3
        err = Error.Join(err, new Error("Fourth joined"));  // Joined at time T4

        var innerErrors = err.InnerErrors;

        // Join preserves join order (oldest first)
        Assert.Equal(4, innerErrors.Count);
        Assert.Equal("First joined", innerErrors.ElementAt(0).Message);    // T1
        Assert.Equal("Second joined", innerErrors.ElementAt(1).Message);   // T2
        Assert.Equal("Third joined", innerErrors.ElementAt(2).Message);    // T3
        Assert.Equal("Fourth joined", innerErrors.ElementAt(3).Message);   // T4
    }

    [Fact]
    public void Error_Join_MultipleErrorsInSingleCall_MaintainsParameterOrder()
    {
        var err = new Error("Root Error");
        
        // Join multiple errors in a single call
        err = Error.Join(err, 
            new Error("Error A"),
            new Error("Error B"),
            new Error("Error C")
        );

        // When joined in a single call, errors should maintain their parameter order
        Assert.Equal(3, err.InnerErrors.Count);
        Assert.Equal("Error A", err.InnerErrors.ElementAt(0).Message);
        Assert.Equal("Error B", err.InnerErrors.ElementAt(1).Message);
        Assert.Equal("Error C", err.InnerErrors.ElementAt(2).Message);
    }

    [Fact]
    public void Error_Join_ComplexScenario_CorrectChronologicalOrder()
    {
        // Complex scenario: constructor chain + multiple individual joins + batch join
        var constructorInner = new Error("Constructor Inner");
        var err = new Error("Root", constructorInner);

        // First individual join
        err = Error.Join(err, new Error("Join 1"));
        
        // Second individual join
        err = Error.Join(err, new Error("Join 2"));
        
        // Batch join
        err = Error.Join(err, 
            new Error("Batch A"),
            new Error("Batch B")
        );
        
        // Final individual join
        err = Error.Join(err, new Error("Join 3"));

        var innerErrors = err.InnerErrors;

        Assert.Equal(6, innerErrors.Count);
        Assert.Equal("Constructor Inner", innerErrors.ElementAt(0).Message);
        Assert.Equal("Join 1", innerErrors.ElementAt(1).Message);
        Assert.Equal("Join 2", innerErrors.ElementAt(2).Message);
        Assert.Equal("Batch A", innerErrors.ElementAt(3).Message);
        Assert.Equal("Batch B", innerErrors.ElementAt(4).Message);
        Assert.Equal("Join 3", innerErrors.ElementAt(5).Message);
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
        rootError = Error.Join(rootError, dbError3, validationError2);
        rootError = Error.Join(rootError, authError3);

        Assert.Equal(8, rootError.InnerErrors.Count);
        
        // Database chain
        Assert.Equal("Database unavailable", rootError.InnerErrors.ElementAt(0).Message);
        Assert.Equal("Connection failed", rootError.InnerErrors.ElementAt(1).Message);
        Assert.Equal("Connection timeout", rootError.InnerErrors.ElementAt(2).Message);
        
        // Validation chain
        Assert.Equal("Validation failed", rootError.InnerErrors.ElementAt(3).Message);
        Assert.Equal("Field required", rootError.InnerErrors.ElementAt(4).Message);
        
        // Auth chain
        Assert.Equal("Authorization denied", rootError.InnerErrors.ElementAt(5).Message);
        Assert.Equal("Authentication failed", rootError.InnerErrors.ElementAt(6).Message);
        Assert.Equal("Token expired", rootError.InnerErrors.ElementAt(7).Message);
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

        // Join all chains in a single call to a new root error
        var rootError = new Error("Root");
        rootError = Error.Join(rootError, chain1, chain2, chain3);

        // Expected order when joining multiple chains at once
        Assert.Equal(6, rootError.InnerErrors.Count);
        Assert.Equal("Root", rootError.Message);
        Assert.Equal("Chain 1 Outer", rootError.InnerErrors.ElementAt(0).Message);
        Assert.Equal("Chain 1 Inner", rootError.InnerErrors.ElementAt(1).Message);
        Assert.Equal("Chain 2 Outer", rootError.InnerErrors.ElementAt(2).Message);
        Assert.Equal("Chain 2 Inner 2", rootError.InnerErrors.ElementAt(3).Message);
        Assert.Equal("Chain 2 Inner 1", rootError.InnerErrors.ElementAt(4).Message);
        Assert.Equal("Chain 3 Outer", rootError.InnerErrors.ElementAt(5).Message);
    }

    [Fact]
    public void Error_Join_MixedSequentialAndBatchJoinsWithChains_CorrectOrder()
    {
        var rootError = new Error("Root error");

        // Join a single error
        rootError = Error.Join(rootError, new Error("Network timeout"));

        // Join an error with a chain (2 errors total)
        var errorInner = new Error("Email invalid");
        var errorOuter = new Error("Validation failed", errorInner);
        rootError = Error.Join(rootError, errorOuter);

        // Join multiple chains
        var err1 = new Error("Table not found");
        var err2 = new Error("Query failed", err1);
        var err3 = new Error("Cache miss");
        rootError = Error.Join(rootError, err2, err3);

        // Join another single error
        rootError = Error.Join(rootError, new Error("Retry limit exceeded"));

        Assert.Equal(7, rootError.InnerErrors.Count);
        Assert.Equal("Root error", rootError.Message);
        Assert.Equal("Network timeout", rootError.InnerErrors[0].Message);
        Assert.Equal("Validation failed", rootError.InnerErrors[1].Message);
        Assert.Equal("Email invalid", rootError.InnerErrors[2].Message);
        Assert.Equal("Query failed", rootError.InnerErrors[3].Message);
        Assert.Equal("Table not found", rootError.InnerErrors[4].Message);
        Assert.Equal("Cache miss", rootError.InnerErrors[5].Message);
        Assert.Equal("Retry limit exceeded", rootError.InnerErrors[6].Message);
    }

    [Fact]
    public void Error_Join_And_ConstructorChaining_ProduceSameStack()
    {
        const string memberName = nameof(Error_Join_And_ConstructorChaining_ProduceSameStack);
        string filePath = Assembly.GetExecutingAssembly().Location;

        var firstJoinError = new Error("First error", memberName, filePath, 101);
        var secondJoinError = new Error("Second error", memberName, filePath, 102);
        var thirdJoinError = new Error("Final error", memberName, filePath, 103);
        var joinRootError = new Error("Root error", memberName, filePath, 100);
        joinRootError = Error.Join(joinRootError, thirdJoinError, secondJoinError, firstJoinError);

        var firstConstructorError = new Error("First error", memberName, filePath, 101);
        var secondConstructorError = new Error("Second error", firstConstructorError, memberName, filePath, 102);
        var thirdConstructorError = new Error("Final error", secondConstructorError, memberName, filePath, 103);
        var constructorRootError = new Error("Root error", thirdConstructorError, memberName, filePath, 100);

        Assert.Equal(joinRootError.Stack, constructorRootError.Stack);
    }
}