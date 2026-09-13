namespace Errgo.Tests;

public class ErrorImmutabilityTests
{
    [Fact]
    public void Error_Join_DoesNotMutateOriginalInstance()
    {
        var original = new Error("Outer", new Error("Inner"));
        var copy = original;

        copy = Error.Join(copy, new Error("Another"));

        Assert.Single(original.InnerErrors);
        Assert.Equal(2, copy.InnerErrors.Count);
        Assert.DoesNotContain("Another", original.Stack);
        Assert.Contains("Another", copy.Stack);
    }

    [Fact]
    public void Error_InnerErrorsListMutation_DoesNotAffectError()
    {
        var error = new Error("Outer", new Error("Inner"));
        var innerErrors = error.InnerErrors;
        var mutableList = innerErrors as List<Error>;

        Assert.NotNull(mutableList);

        mutableList!.Add(new Error("Injected"));

        Assert.Single(error.InnerErrors);
        Assert.Equal("Inner", error.InnerErrors[0].Message);
    }

    [Fact]
    public void Error_Copy_SharesInnerErrorsArrayReference()
    {
        var err1 = new Error("Original");
        err1 = Error.Join(err1, new Error("Joined"));

        var errCopy = err1;

        var field = typeof(Error).GetField("_innerErrors",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

        var arr1 = (Error[]?)field.GetValue(err1);
        var arr2 = (Error[]?)field.GetValue(errCopy);

        Assert.NotNull(arr1);
        Assert.NotNull(arr2);
        Assert.True(ReferenceEquals(arr1, arr2));
    }

    [Fact]
    public void Error_JoinAfterCopy_DoesNotMutateSharedArray()
    {
        var err1 = new Error("Original");
        err1 = Error.Join(err1, new Error("Joined"));

        var errCopy = err1;

        errCopy = Error.Join(errCopy, new Error("Another Joined"));
        Assert.Equal(2, errCopy.InnerErrors.Count);
        Assert.Single(err1.InnerErrors);
    }
}

