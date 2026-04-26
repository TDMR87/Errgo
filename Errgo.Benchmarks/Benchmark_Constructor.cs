using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;

namespace Errgo.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class Benchmark_Constructor
{
    private readonly Consumer consumer = new();

    private readonly string normalMessage = "Operation failed";
    private readonly string emptyMessage = string.Empty;
    private readonly string whitespaceMessage = "   ";

    private Error[] innerChains = [];

    [Params(1, 4, 16, 64)]
    public int Depth { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        const int maxDepth = 64;

        innerChains = new Error[maxDepth + 1];
        innerChains[0] = Error.None;

        var chain = new Error("Inner-1");
        innerChains[1] = chain;

        for (int i = 2; i <= maxDepth; i++)
        {
            chain = new Error($"Inner-{i}", chain);
            innerChains[i] = chain;
        }
    }

    [Benchmark(Baseline = true, Description = "new Error()")]
    public void Constructor_Default()
    {
        var err = new Error();
        consumer.Consume(err);
    }

    [Benchmark(Description = "new Error(message)")]
    public void Constructor_WithMessage()
    {
        var err = new Error(normalMessage);
        consumer.Consume(err);
    }

    [Benchmark(Description = "new Error(empty message)")]
    public void Constructor_WithEmptyMessage()
    {
        var err = new Error(emptyMessage);
        consumer.Consume(err);
    }

    [Benchmark(Description = "new Error(whitespace message)")]
    public void Constructor_WithWhitespaceMessage()
    {
        var err = new Error(whitespaceMessage);
        consumer.Consume(err);
    }

    [Benchmark(Description = "new Error(message, Error.None)")]
    public void Constructor_WithErrorNoneInner()
    {
        var err = new Error(normalMessage, Error.None);
        consumer.Consume(err);
    }

    [Benchmark(Description = "new Error(message, inner chain)")]
    public void Constructor_WithInnerChain()
    {
        var err = new Error(normalMessage, innerChains[Depth]);
        consumer.Consume(err);
    }

    [Benchmark(Description = "new Error(message, inner chain) + InnerErrors.Count")]
    public void Constructor_WithInnerChain_AndFlatten()
    {
        var err = new Error(normalMessage, innerChains[Depth]);
        consumer.Consume(err);
        consumer.Consume(err.InnerErrors.Count);
    }
}
