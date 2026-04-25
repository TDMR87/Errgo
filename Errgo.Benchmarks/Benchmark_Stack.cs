using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;

namespace Errgo.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class Benchmark_Stack
{
    private readonly Consumer consumer = new();

    private Error?[] flatErrors = [];
    private Error?[] nestedErrors = [];

    private Error flatRoot;
    private Error nestedRoot;
    private Error chainRoot;

    [Params(1, 4, 16, 64)]
    public int StackCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        const int maxStackCount = 64;

        flatErrors = new Error?[maxStackCount];
        nestedErrors = new Error?[maxStackCount];

        for (int i = 0; i < maxStackCount; i++)
        {
            flatErrors[i] = new Error($"Flat-{i}");

            var nestedInner = new Error($"Nested-{i}-0");
            nestedInner = new Error($"Nested-{i}-1", nestedInner);
            nestedErrors[i] = new Error($"Nested-{i}", nestedInner);
        }

        var flatInput = new Error?[StackCount + 1];
        flatInput[0] = new Error("Flat-Root");
        Array.Copy(flatErrors, 0, flatInput, 1, StackCount);
        flatRoot = Error.Join(flatInput);

        var nestedChain = new Error("Nested-Chain-0");
        for (int i = 1; i < 4; i++)
        {
            nestedChain = new Error($"Nested-Chain-{i}", nestedChain);
        }

        var nestedInput = new Error?[StackCount + 1];
        nestedInput[0] = new Error("Nested-Root", nestedChain);
        Array.Copy(nestedErrors, 0, nestedInput, 1, StackCount);
        nestedRoot = Error.Join(nestedInput);

        chainRoot = new Error("Chain-0");
        for (int i = 1; i <= StackCount; i++)
        {
            chainRoot = new Error($"Chain-{i}", chainRoot);
        }
    }

    [Benchmark(Baseline = true, Description = "Stack from flat joined root")]
    public void Stack_FlatJoin()
    {
        var stack = flatRoot.Stack;
        consumer.Consume(stack);
        consumer.Consume(stack.Length);
    }

    [Benchmark(Description = "Stack from nested joined root")]
    public void Stack_NestedJoin()
    {
        var stack = nestedRoot.Stack;
        consumer.Consume(stack);
        consumer.Consume(stack.Length);
    }

    [Benchmark(Description = "Stack from constructor chain")]
    public void Stack_ConstructorChain()
    {
        var stack = chainRoot.Stack;
        consumer.Consume(stack);
        consumer.Consume(stack.Length);
    }
}
