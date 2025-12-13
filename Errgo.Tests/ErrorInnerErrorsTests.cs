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
        
        var wrapper = new Error("Wrapper");
        wrapper.Wrap(err1, err2, err3);

        var innerErrors = wrapper.InnerErrors;
        Assert.Equal(3, innerErrors.Count);  // Only the wrapped errors, not "Wrapper" itself
        Assert.Equal("First", innerErrors[0].Message);
        Assert.Equal("Second", innerErrors[1].Message);
        Assert.Equal("Third", innerErrors[2].Message);
    }
}
