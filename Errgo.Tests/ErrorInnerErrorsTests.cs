namespace Errgo.Tests;

public class ErrorInnerErrorsTests
{
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
    public void Error_InnerErrors_PreservesOrder()
    {
        var err1 = new Error("First");
        var err2 = new Error("Second");
        var err3 = new Error("Third");
        
        var joiner = new Error("Joiner");
        joiner = Error.Join(joiner, err1, err2, err3);

        var innerErrors = joiner.InnerErrors;
        Assert.Equal(3, innerErrors.Count);  // Only the joined errors, not "Joiner" itself
        Assert.Equal("First", innerErrors[0].Message);
        Assert.Equal("Second", innerErrors[1].Message);
        Assert.Equal("Third", innerErrors[2].Message);
    }

    [Fact]
    public void Error_InnerErrors_OnlyShowsErrorsJoinedByThis()
    {
        var err1 = new Error("Error 1");
        var err2 = new Error("Error 2", err1);
        var err3 = new Error("Error 3", err2);

        Assert.Empty(err1.InnerErrors);

        Assert.Single(err2.InnerErrors);
        Assert.Contains(err2.InnerErrors, e => e.Message == "Error 1");

        Assert.Equal(2, err3.InnerErrors.Count);
        Assert.Contains(err3.InnerErrors, e => e.Message == "Error 2");
        Assert.Contains(err3.InnerErrors, e => e.Message == "Error 1");
    }
}

