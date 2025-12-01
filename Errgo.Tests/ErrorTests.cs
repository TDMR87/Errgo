using Errgo;

namespace Errgo.Tests;

public class ErrorTests
{
    [Fact]
    public void Errors_WithIdenticalMemberValues_AreEqual()
    {
        Error err1 = new("error");
        Error err2 = new("error");
        Assert.Equal(err1, err2);
        Assert.True(err1 == err2);
        Assert.True(err1.Equals(err2));
    }

    [Fact]
    public void Errors_None_AreEqual()
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
    public void Error_From_WithExpression()
    {
        Error err1 = new("error");
        var err2 = err1 with { Message = "different error" };
        Assert.True(err2);
        Assert.NotEqual(err1, err2);
    }

    [Fact]
    public void Error_From_String()
    {
        Error err = "error";
        Assert.True(err);
    }

    [Fact]
    public void Error_ReturnsTrue()
    {
        Assert.True(new Error());
    }

    [Fact]
    public void Error_None_ReturnsFalse()
    {
        Assert.False(Error.None);
    }

    [Fact]
    public void Error_Sentinel_ValueEquality()
    {
        (object?, Error) GetItemById(int id)
        {
            return (default, Errors.NotFound);
        }

        var (item, err) = GetItemById(123);
        
        Assert.Equal(Errors.NotFound, err);
        Assert.True(err == Errors.NotFound);
        Assert.True(err.Equals(Errors.NotFound));
    }

    [Fact]
    public void Error_Chaining()
    {
        var err = new Error($"Some error.", Errors.NotFound);
        Assert.True(err);
    }

    [Fact]
    public void Error_Chaining_MessagesAreChained()
    {
        var itemId = 123;

        (object?, Error) GetItemById(int id)
        {
            return (null, new($"Failed to get item with id {id}", Errors.NotFound));
        }

        var (_, err) = GetItemById(itemId);
        Assert.Equal($"Failed to get item with id {itemId}. Item not found", err.FullMessage);
    }
}
