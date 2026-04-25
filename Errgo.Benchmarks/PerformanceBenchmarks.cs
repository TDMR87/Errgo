using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Errgo.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class PerformanceBenchmarks
{
    private Error _chainedError;

    [GlobalSetup]
    public void Setup()
    {
        // Create a 10-level deep error chain
        _chainedError = new Error("Level 0");
        for (int i = 1; i < 10; i++)
        {
            _chainedError = new Error($"Level {i}", _chainedError);
        }
    }

    [Benchmark(Description = "Return Error.None")]
    public (int?, Error) CreateSuccess()
    {
        return (9999, Error.None);
    }

    [Benchmark(Description = "Create new Error")]
    public (int?, Error) CreateError()
    {
        return (null, new Error("Operation failed"));
    }

    [Benchmark(Description = "Create Error with inner error")]
    public Error CreateErrorWithInnerError()
    {
        var inner = new Error("Inner error");
        return new Error("Outer error", inner);
    }

    [Benchmark(Description = "Propagate through 10 method calls")]
    public Error PropagateThroughDeepCallStack()
    {
        return DeepCallStack_1();
    }

    [Benchmark(Description = "Join() - Add error to chain")]
    public Error JoinSingleError()
    {
        var err = new Error("Original");
        err = Error.Join(err, new Error("Additional error"));
        return err;
    }

    [Benchmark(Description = "Is() - Search 10-level chain")]
    public bool ErrorIs()
    {
        var target = new Error("Level 5");
        return _chainedError.Is(target);
    }

    [Benchmark(Description = "As() - Search 10-level chain")]
    public Error ErrorAs()
    {
        var target = new Error("Level 5");
        _chainedError.As(target, out Error err);
        return err;
    }

    [Benchmark(Description = "InnerErrors - Get from 10-level chain")]
    public List<Error> GetInnerErrors()
    {
        return _chainedError.InnerErrors.ToList();
    }

    [Benchmark(Description = "InnerErrors - Enumerate 10-level chain")]
    public int EnumerateInnerErrors()
    {
        var list = new List<Error>();
        foreach (var error in _chainedError.InnerErrors)
        {
            list.Add(error);
        }

        return list.Count;
    }

    [Benchmark(Description = "Equals() - Compare errors")]
    public bool ErrorEquals()
    {
        var err1 = Error.Sentinel("Test error");
        var err2 = Error.Sentinel("Test error");
        return err1.Equals(err2);
    }

    [Benchmark(Description = "ToString() - With source location")]
    public string ErrorToString()
    {
        return new Error("Test error").ToString();
    }

    [Benchmark(Description = "Stack - Get from 10-level chain")]
    public string GetStack()
    {
        return _chainedError.Stack;
    }

    private Error DeepCallStack_1() => DeepCallStack_2();
    private Error DeepCallStack_2() => DeepCallStack_3();
    private Error DeepCallStack_3() => DeepCallStack_4();
    private Error DeepCallStack_4() => DeepCallStack_5();
    private Error DeepCallStack_5() => DeepCallStack_6();
    private Error DeepCallStack_6() => DeepCallStack_7();
    private Error DeepCallStack_7() => DeepCallStack_8();
    private Error DeepCallStack_8() => DeepCallStack_9();
    private Error DeepCallStack_9() => DeepCallStack_10();
    private Error DeepCallStack_10() => new Error("Operation failed");
}

