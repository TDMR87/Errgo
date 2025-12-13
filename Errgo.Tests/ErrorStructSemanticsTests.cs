namespace Errgo.Tests;

public class ErrorStructSemanticsTests
{
    [Fact]
    public void Error_Wrap_AfterCopy_SharesErrorChainReference()
    {
        var err1 = new Error("Original");
        err1.Wrap(new Error("Wrapped"));

        // Copy
        var errCopy = err1;

        // Both will have the wrapped error due to shared ErrorChain reference
        Assert.Equal(err1, errCopy);
        Assert.Single(err1.InnerErrors);
        Assert.Single(errCopy.InnerErrors);
        Assert.Equal(err1.InnerErrors[0], errCopy.InnerErrors[0]);
    }

    [Fact]
    public void Error_DefaultInArray_IsErrorNone()
    {
        var errors = new Error[3];
        
        Assert.Equal(Error.None, errors[0]);
        Assert.Equal(Error.None, errors[1]);
        Assert.Equal(Error.None, errors[2]);
        Assert.DoesNotContain(Error.Empty, errors);
    }
}
