namespace Errgo.Tests;

public class ErrorSentinelTests
{
    public static readonly Error NotFound = new("Item not found");

    [Fact]
    public void Error_SentinelError_ValueEquality()
    {
        static (object?, Error) GetItemById(int id)
        {
            return (null, NotFound);
        }

        var (item, err) = GetItemById(123);
        
        Assert.Equal(NotFound, err);
        Assert.True(err == NotFound);
        Assert.True(err.Equals(NotFound));
    }

    [Fact]
    public void Error_SentinelError_ChainingConstructor()
    {
        var err = new Error($"Some error happened", NotFound);
        Assert.True(err);
        Assert.Contains("Some error happened", err.Stack);
        Assert.Contains("Item not found", err.Stack);
    }

    [Fact]
    public void Error_SentinelError_ChainingJoin()
    {
        var err = new Error($"Some error happened", NotFound);
        var joiner = Error.Empty;
        joiner.Join(err);
        Assert.True(joiner);
        Assert.Contains("Some error happened", joiner.Stack);
        Assert.Contains("Item not found", joiner.Stack);
    }

    [Fact]
    public void Error_SentinelError_ChainingConstructor2()
    {
        var itemId = 123;

        static (object?, Error) GetItemById(int id)
        {
            return (null, new($"Failed to get item with id {id}", NotFound));
        }

        var (_, err) = GetItemById(itemId);
        Assert.Contains($"Failed to get item with id {itemId}", err.Stack);
        Assert.Contains($"Item not found", err.Stack);
    }
}
