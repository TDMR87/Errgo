namespace Errgo.Tests;

public class Errors
{
    public static readonly Error NotFound = Error.Sentinel("Item not found");
}

public class ErrorSentinelTests
{
    [Fact]
    public void Error_SentinelError_NoSourceLocationInformation()
    {
        static (object?, Error) GetItemById(int id)
        {
            return (null, Errors.NotFound);
        }

        var (item, err) = GetItemById(123);

        Assert.Null(err.SourceMemberName);
        Assert.Null(err.SourceLineNumber);
        Assert.Null(err.SourceFilepath);
    }

    [Fact]
    public void Error_SentinelError_HasSourceLocationInformation_WhenWrappedInError()
    {
        static (object?, Error) GetItemById(int id)
        {
            return (null, new Error($"Error getting item with id {id}", Errors.NotFound));
        }

        var (item, err) = GetItemById(123);

        Assert.Equal("Error getting item with id 123", err.Message);
        Assert.Equal(Errors.NotFound, err.InnerErrors[0]);
        Assert.NotNull(err.SourceFilepath);
        Assert.NotEqual(0, err.SourceLineNumber);
        Assert.Equal(nameof(Error_SentinelError_HasSourceLocationInformation_WhenWrappedInError), err.SourceMemberName);
    }

    [Fact]
    public void Error_SentinelError_ValueEquality()
    {
        static (object?, Error) GetItemById(int id)
        {
            return (null, Errors.NotFound);
        }

        var (item, err) = GetItemById(123);

        Assert.Equal(Errors.NotFound, err);
        Assert.True(err == Errors.NotFound);
        Assert.True(err.Equals(Errors.NotFound));
    }

    [Fact]
    public void Error_SentinelError_ChainingConstructor()
    {
        var err = new Error($"Some error happened", Errors.NotFound);
        Assert.True(err);
        Assert.Contains("Some error happened", err.Stack);
        Assert.Contains("Item not found", err.Stack);
    }

    [Fact]
    public void Error_SentinelError_ChainingJoin()
    {
        var err = new Error($"Some error happened", Errors.NotFound);
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
            return (null, new($"Failed to get item with id {id}", Errors.NotFound));
        }

        var (_, err) = GetItemById(itemId);
        Assert.Contains($"Failed to get item with id {itemId}", err.Stack);
        Assert.Contains($"Item not found", err.Stack);
    }
}
