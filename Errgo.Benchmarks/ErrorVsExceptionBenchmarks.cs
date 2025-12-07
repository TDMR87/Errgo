using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Errgo.Benchmarks;

/// <summary>
/// Benchmarks comparing Errgo Error type with .NET exceptions.
/// These benchmarks measure the performance characteristics of error handling
/// using value types (Error) vs. reference types (Exception).
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ErrorVsExceptionBenchmarks
{
    [Benchmark(Description = "Error: Return error from method")]
    public (object?, Error) ReturnError()
    {
        var (val, err) = MethodThatReturnsError();
        if (err) return (null, err);
        else return (val, Error.None);
    }

    [Benchmark(Description = "Exception: Throw and catch exception")]
    public Exception ThrowAndCatchException()
    {
        try
        {
            var val = MethodThatThrowsException();
        }
        catch (Exception ex)
        {
            return ex;
        }

        return new Exception("No exception thrown");
    }

    private (object?, Error) MethodThatReturnsError()
    {
        return (null, new Error("Operation failed"));
    }

    private object? MethodThatThrowsException()
    {
        throw new InvalidOperationException("Operation failed");
    }


    [Benchmark(Description = "Error: through 10 method calls")]
    public Error ErrorThroughDeepCallStack()
    {
        return DeepCallStack_Error_1();
    }

    [Benchmark(Description = "Exception: through 10 method calls")]
    public Exception ExceptionThroughDeepCallStack()
    {
        try
        {
            return DeepCallStack_Exception_1();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    // Error call stack
    private Error DeepCallStack_Error_1() => DeepCallStack_Error_2();
    private Error DeepCallStack_Error_2() => DeepCallStack_Error_3();
    private Error DeepCallStack_Error_3() => DeepCallStack_Error_4();
    private Error DeepCallStack_Error_4() => DeepCallStack_Error_5();
    private Error DeepCallStack_Error_5() => DeepCallStack_Error_6();
    private Error DeepCallStack_Error_6() => DeepCallStack_Error_7();
    private Error DeepCallStack_Error_7() => DeepCallStack_Error_8();
    private Error DeepCallStack_Error_8() => DeepCallStack_Error_9();
    private Error DeepCallStack_Error_9() => DeepCallStack_Error_10();
    private Error DeepCallStack_Error_10() => new Error("Operation failed");

    // Exception call stack
    private Exception DeepCallStack_Exception_1() => DeepCallStack_Exception_2();
    private Exception DeepCallStack_Exception_2() => DeepCallStack_Exception_3();
    private Exception DeepCallStack_Exception_3() => DeepCallStack_Exception_4();
    private Exception DeepCallStack_Exception_4() => DeepCallStack_Exception_5();
    private Exception DeepCallStack_Exception_5() => DeepCallStack_Exception_6();
    private Exception DeepCallStack_Exception_6() => DeepCallStack_Exception_7();
    private Exception DeepCallStack_Exception_7() => DeepCallStack_Exception_8();
    private Exception DeepCallStack_Exception_8() => DeepCallStack_Exception_9();
    private Exception DeepCallStack_Exception_9() => DeepCallStack_Exception_10();
    private Exception DeepCallStack_Exception_10() => throw new InvalidOperationException("Operation failed");
}
