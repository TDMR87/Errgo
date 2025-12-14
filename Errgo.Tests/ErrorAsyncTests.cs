namespace Errgo.Tests;

public class ErrorAsyncTests
{
    [Fact]
    public async Task Error_InAsyncContext_PreservesSourceLocation()
    {
        var (_, err) = await GetErrorAsync();
        
        Assert.NotEmpty(err.SourceMemberName);
        Assert.Equal("GetErrorAsync", err.SourceMemberName);
        Assert.Contains("ErrorAsyncTests.cs", err.SourceFilePath);
        Assert.True(err.SourceLineNumber > 0);
    }

    [Fact]
    public async Task Error_AsyncChain_PreservesAllSourceLocations()
    {
        var (_, err) = await Level3Async();
        
        var innerErrors = err.InnerErrors;
        Assert.Equal(2, innerErrors.Count);
        
        Assert.Equal("Level2Async", innerErrors[0].SourceMemberName);
        Assert.Equal("Level1Async", innerErrors[1].SourceMemberName);
    }

    [Fact]
    public async Task Error_TaskReturningError_WorksCorrectly()
    {
        var task = GetErrorTaskAsync();
        var (_, err) = await task;
        
        Assert.True(err);
        Assert.Equal("Task error", err.Message);
    }

    [Fact]
    public async Task Error_MultipleAwaitedCalls_EachPreservesLocation()
    {
        var (_, err1) = await GetErrorAsync();
        var (_, err2) = await GetErrorAsync();
        
        Assert.Equal(err1.SourceMemberName, err2.SourceMemberName);
        Assert.NotEqual(0, err1.SourceLineNumber);
        Assert.NotEqual(0, err2.SourceLineNumber);
    }

    private async Task<(object?, Error)> GetErrorAsync()
    {
        await Task.Delay(1);
        return (null, new Error("Async error"));
    }

    private async Task<(object?, Error)> Level1Async()
    {
        await Task.Delay(1);
        return (null, new Error("Level 1 error"));
    }

    private async Task<(object?, Error)> Level2Async()
    {
        var (_, err) = await Level1Async();
        if (err) return (null, new Error("Level 2 error", err));
        return (null, Error.None);
    }

    private async Task<(object?, Error)> Level3Async()
    {
        var (_, err) = await Level2Async();
        if (err) return (null, new Error("Level 3 error", err));
        return (null, Error.None);
    }

    private async Task<(object?, Error)> GetErrorTaskAsync()
    {
        await Task.Delay(1);
        return (null, new Error("Task error"));
    }
}
