namespace Errgo.Tests;

public class ErrorEqualityTests
{
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
    public void Error_WithoutMessage_And_ErrorNone_AreNotEqual()
    {
        Assert.NotEqual(new Error(), Error.None);
        Assert.False(new Error() == Error.None);
        Assert.False(new Error().Equals(Error.None));
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
    public void Error_Equals_CaseSensitive()
    {
        var err1 = new Error("Error");
        var err2 = new Error("error");
        
        Assert.NotEqual(err1, err2);
        Assert.False(err1 == err2);
    }

    [Fact]
    public void Error_Equals_Null_ReturnsFalse()
    {
        var err = new Error("Test");
        
        Assert.False(err.Equals(null));
    }
}
