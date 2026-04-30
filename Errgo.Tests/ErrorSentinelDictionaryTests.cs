namespace Errgo.Tests;

public class ErrorSentinelDictionaryTests
{
    [Fact]
    public void Error_Sentinel_CanBeUsedAsDictionaryKey_ForHandlerLookup()
    {
        var notFound = Error.Sentinel("NotFound");
        var unauthorized = Error.Sentinel("Unauthorized");

        var handlers = new Dictionary<Error, Func<string>>
        {
            [notFound] = () => "Handled not found",
            [unauthorized] = () => "Handled unauthorized"
        };

        var lookupKey = new Error("NotFound");

        Assert.True(handlers.TryGetValue(lookupKey, out var handler));
        Assert.NotNull(handler);
        Assert.Equal("Handled not found", handler());
    }

    [Fact]
    public void Error_Sentinel_Lookup_UsesSemanticEquality_NotSourceLocation()
    {
        var sentinel = Error.Sentinel("ValidationFailed");

        var handlers = new Dictionary<Error, Func<string>>
        {
            [sentinel] = () => "Handled validation failure"
        };

        var err = new Error("ValidationFailed");

        Assert.True(handlers.ContainsKey(err));
        Assert.Equal(sentinel.GetHashCode(), err.GetHashCode());
        Assert.Equal("Handled validation failure", handlers[err]());
    }
}
