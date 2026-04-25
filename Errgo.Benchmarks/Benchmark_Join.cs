using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;

namespace Errgo.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class Benchmark_Join
{
    private readonly Consumer consumer = new();

    private Error?[] allErrors = [];
    private Error?[] joinedErrors = [];
    private Error?[] joinErrorsWithRoot = [];
    private Error?[] joinErrorsWithExistingRoot = [];
    private Error existingErrorChain;

    [Params(1, 4, 16, 64)]
    public int JoinCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        const int maxJoinCount = 64;

        allErrors = new Error?[maxJoinCount];
        for (int i = 0; i < maxJoinCount; i++)
        {
            allErrors[i] = new Error($"Inner-{i}");
        }

        existingErrorChain = new Error("Chain-0");
        for (int i = 1; i < 8; i++)
        {
            existingErrorChain = new Error($"Chain-{i}", existingErrorChain);
        }

        joinedErrors = new Error?[JoinCount];
        Array.Copy(allErrors, joinedErrors, JoinCount);

        joinErrorsWithRoot = new Error?[JoinCount + 1];
        joinErrorsWithRoot[0] = new Error("Root");
        Array.Copy(joinedErrors, 0, joinErrorsWithRoot, 1, JoinCount);

        joinErrorsWithExistingRoot = new Error?[JoinCount + 1];
        joinErrorsWithExistingRoot[0] = new Error("Root", existingErrorChain);
        Array.Copy(joinedErrors, 0, joinErrorsWithExistingRoot, 1, JoinCount);
    }

    [Benchmark(Baseline = true, Description = "Join static array")]
    public void JoinParams()
    {
        var result = Error.Join(joinErrorsWithRoot);
        consumer.Consume(result);
        consumer.Consume(result.InnerErrors.Count);
    }

    [Benchmark(Description = "Join in loop one-by-one")]
    public void JoinSingle_InLoop()
    {
        var root = new Error("Root");
        for (int i = 0; i < joinedErrors.Length; i++)
        {
            root = Error.Join(root, joinedErrors[i]);
        }
        consumer.Consume(root);
        consumer.Consume(root.InnerErrors.Count);
    }

    [Benchmark(Description = "Join array into root with existing errors")]
    public void JoinParams_ExistingChain()
    {
        var wrapper = new Error("wrapper");
        var result = Error.Join(wrapper, joinErrorsWithExistingRoot);
        consumer.Consume(result);
        consumer.Consume(result.InnerErrors.Count);
    }
}

