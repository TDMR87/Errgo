using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Errgo.Benchmarks;

/// <summary>
/// Benchmarks comparing Errgo Error type with .NET exceptions.
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
            return new Exception("No exception thrown");
        }
        catch (Exception ex)
        {
            return ex;
        }
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
            DeepCallStack_Exception_1();
            return new Exception("No exception thrown");
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private (object?, Error) MethodThatReturnsError() => (null, new Error("Operation failed"));

    private object? MethodThatThrowsException() => throw new InvalidOperationException("Operation failed");

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

    // Exception call stack - all methods throw to properly measure stack unwinding
    private void DeepCallStack_Exception_1() => DeepCallStack_Exception_2();
    private void DeepCallStack_Exception_2() => DeepCallStack_Exception_3();
    private void DeepCallStack_Exception_3() => DeepCallStack_Exception_4();
    private void DeepCallStack_Exception_4() => DeepCallStack_Exception_5();
    private void DeepCallStack_Exception_5() => DeepCallStack_Exception_6();
    private void DeepCallStack_Exception_6() => DeepCallStack_Exception_7();
    private void DeepCallStack_Exception_7() => DeepCallStack_Exception_8();
    private void DeepCallStack_Exception_8() => DeepCallStack_Exception_9();
    private void DeepCallStack_Exception_9() => DeepCallStack_Exception_10();
    private void DeepCallStack_Exception_10() => throw new InvalidOperationException("Operation failed");
}
