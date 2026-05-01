using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;

namespace Errgo.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class Benchmark_ToString
{
    private readonly Consumer consumer = new();

    private Error none;
    private Error sentinel;
    private Error withSource;
    private Error withLongMessageAndSource;

    [GlobalSetup]
    public void GlobalSetup()
    {
        none = Error.None;
        sentinel = Error.Sentinel("Operation failed");
        withSource = new Error(
            message: "Operation failed",
            memberName: "HandleRequest",
            filePath: @"C:\Git\Errgo\Errgo\Errgo\Services\RequestHandler.cs",
            lineNumber: 42);
        withLongMessageAndSource = new Error(
            message: "Operation failed because the remote dependency returned an invalid payload and the response could not be parsed",
            memberName: "HandleRequest",
            filePath: @"C:\Git\Errgo\Errgo\Errgo\Services\RequestHandler.cs",
            lineNumber: 42);
    }

    [Benchmark(Baseline = true, Description = "ToString on Error.None")]
    public void ToString_None()
    {
        var text = none.ToString();
        consumer.Consume(text);
        consumer.Consume(text.Length);
    }

    [Benchmark(Description = "ToString on sentinel error")]
    public void ToString_Sentinel()
    {
        var text = sentinel.ToString();
        consumer.Consume(text);
        consumer.Consume(text.Length);
    }

    [Benchmark(Description = "ToString with source info")]
    public void ToString_WithSource()
    {
        var text = withSource.ToString();
        consumer.Consume(text);
        consumer.Consume(text.Length);
    }

    [Benchmark(Description = "ToString with long message and source info")]
    public void ToString_WithLongMessageAndSource()
    {
        var text = withLongMessageAndSource.ToString();
        consumer.Consume(text);
        consumer.Consume(text.Length);
    }
}
