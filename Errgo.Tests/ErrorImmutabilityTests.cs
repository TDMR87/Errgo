namespace Errgo.Tests;

public class ErrorImmutabilityTests
{
    [Fact]
    public void Error_Join_DoesNotMutateOriginalInstance()
    {
        var original = new Error("Outer", new Error("Inner"));
        var copy = original;

        copy.Join(new Error("Another"));

        Assert.Single(original.InnerErrors);
        Assert.Equal("Inner", original.InnerErrors[0].Message);
        Assert.Equal(2, copy.InnerErrors.Count);
        Assert.Equal("Another", copy.InnerErrors[0].Message);
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
}
