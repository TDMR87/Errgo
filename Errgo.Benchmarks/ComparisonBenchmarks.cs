using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ErrorOr;
using Ardalis.Result;

namespace Errgo.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.Method)]
[RankColumn]
public class ComparisonBenchmarks
{
    #region Callstack

    [Benchmark(Description = "Propagate through 10 method calls - Ardalis")]
    public Ardalis.Result.Result<int> PropagateThroughDeepCallStack_Ardalis()
    {
        return DeepCallStack_Ardalis_1();
    }

    [Benchmark(Description = "Propagate through 10 method calls - Errgo")]
    public Errgo.Error PropagateThroughDeepCallStack_Errgo()
    {
        var err = DeepCallStack_Errgo_1();
        return err;
    }

    [Benchmark(Description = "Propagate through 10 method calls - ErrorOr")]
    public ErrorOr<int> PropagateThroughDeepCallStack_ErrorOr()
    {
        return DeepCallStack_ErrorOr_1();
    }

    [Benchmark(Description = "Propagate through 10 method calls - Exception")]
    public Exception PropagateThroughDeepCallStack_Exception()
    {
        try
        {
            DeepCallStack_Exception_1();
            return new Exception("err");
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    [Benchmark(Description = "Propagate through 10 method calls - FluentResults")]
    public FluentResults.Result PropagateThroughDeepCallStack_FluentResults()
    {
        return DeepCallStack_FluentResults_1();
    }

    #endregion

    #region Return unsuccessful result

    [Benchmark(Description = "Return unsuccessful result - Ardalis")]
    public Ardalis.Result.Result<int> CreateError_Ardalis()
    {
        return Result<int>.Error("Operation failed");
    }

    [Benchmark(Description = "Return unsuccessful result - Errgo")]
    public Errgo.Error CreateError_Errgo()
    {
        return new Errgo.Error("Operation failed");
    }

    [Benchmark(Description = "Return unsuccessful result - ErrorOr")]
    public ErrorOr<int> CreateError_ErrorOr()
    {
        return ErrorOr.Error.Failure(description: "Operation failed");
    }

    [Benchmark(Description = "Return unsuccessful result - FluentResults")]
    public FluentResults.Result CreateError_FluentResults()
    {
        return FluentResults.Result.Fail("Operation failed");
    }

    [Benchmark(Description = "Return unsuccessful result (throw) - Exception")]
    public Exception Throw_Exception()
    {
        try
        {
            throw new InvalidOperationException("Operation failed");
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    #endregion

    #region Return successful result

    [Benchmark(Description = "Return successful result - Ardalis")]
    public Ardalis.Result.Result<int> CreateSuccess_Ardalis()
    {
        var number = GetNumber();
        return Result<int>.Success(number);
    }

    [Benchmark(Description = "Return successful result - Errgo")]
    public (int, Error) CreateSuccess_Errgo()
    {
        var number = GetNumber();
        return (number, Error.None);
    }

    [Benchmark(Description = "Return successful result - ErrorOr")]
    public ErrorOr<int> CreateSuccess_ErrorOr()
    {
        var number = GetNumber();
        return number;
    }

    [Benchmark(Description = "Return successful result - FluentResults")]
    public FluentResults.Result<int> CreateSuccess_FluentResults()
    {
        var number = GetNumber();
        return FluentResults.Result.Ok(number);
    }

    #endregion

    #region Wrap unsuccessful result

    [Benchmark(Description = "Multiple chained unsuccessful results - Errgo")]
    public Error ErrorChaining_Errgo()
    {
        var firstError = new Errgo.Error("First error");
        var secondError = new Errgo.Error("Second error", firstError);
        var thirdError = new Errgo.Error("Final error", secondError);

        var rootError = new Error("Root error");
        rootError.Join(thirdError);
        return rootError;
    }

    [Benchmark(Description = "Multiple chained unsuccessful results - FluentResults")]
    public FluentResults.Result ErrorChaining_FluentResults()
    {
        var firstError = new FluentResults.Error("First error");
        var secondError = new FluentResults.Error("Second error").CausedBy(firstError);
        var thirdError = new FluentResults.Error("Final error").CausedBy(secondError);

        var rootError = new FluentResults.Error("Root error").CausedBy(thirdError);
        return rootError;
    }

    #endregion

    private Error DeepCallStack_Errgo_1() => DeepCallStack_Errgo_2();
    private Error DeepCallStack_Errgo_2() => DeepCallStack_Errgo_3();
    private Error DeepCallStack_Errgo_3() => DeepCallStack_Errgo_4();
    private Error DeepCallStack_Errgo_4() => DeepCallStack_Errgo_5();
    private Error DeepCallStack_Errgo_5() => DeepCallStack_Errgo_6();
    private Error DeepCallStack_Errgo_6() => DeepCallStack_Errgo_7();
    private Error DeepCallStack_Errgo_7() => DeepCallStack_Errgo_8();
    private Error DeepCallStack_Errgo_8() => DeepCallStack_Errgo_9();
    private Error DeepCallStack_Errgo_9() => DeepCallStack_Errgo_10();
    private Error DeepCallStack_Errgo_10() => new Error("Operation failed");

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

    private FluentResults.Result DeepCallStack_FluentResults_1() => DeepCallStack_FluentResults_2();
    private FluentResults.Result DeepCallStack_FluentResults_2() => DeepCallStack_FluentResults_3();
    private FluentResults.Result DeepCallStack_FluentResults_3() => DeepCallStack_FluentResults_4();
    private FluentResults.Result DeepCallStack_FluentResults_4() => DeepCallStack_FluentResults_5();
    private FluentResults.Result DeepCallStack_FluentResults_5() => DeepCallStack_FluentResults_6();
    private FluentResults.Result DeepCallStack_FluentResults_6() => DeepCallStack_FluentResults_7();
    private FluentResults.Result DeepCallStack_FluentResults_7() => DeepCallStack_FluentResults_8();
    private FluentResults.Result DeepCallStack_FluentResults_8() => DeepCallStack_FluentResults_9();
    private FluentResults.Result DeepCallStack_FluentResults_9() => DeepCallStack_FluentResults_10();
    private FluentResults.Result DeepCallStack_FluentResults_10() => FluentResults.Result.Fail("Operation failed");

    private ErrorOr<int> DeepCallStack_ErrorOr_1() => DeepCallStack_ErrorOr_2();
    private ErrorOr<int> DeepCallStack_ErrorOr_2() => DeepCallStack_ErrorOr_3();
    private ErrorOr<int> DeepCallStack_ErrorOr_3() => DeepCallStack_ErrorOr_4();
    private ErrorOr<int> DeepCallStack_ErrorOr_4() => DeepCallStack_ErrorOr_5();
    private ErrorOr<int> DeepCallStack_ErrorOr_5() => DeepCallStack_ErrorOr_6();
    private ErrorOr<int> DeepCallStack_ErrorOr_6() => DeepCallStack_ErrorOr_7();
    private ErrorOr<int> DeepCallStack_ErrorOr_7() => DeepCallStack_ErrorOr_8();
    private ErrorOr<int> DeepCallStack_ErrorOr_8() => DeepCallStack_ErrorOr_9();
    private ErrorOr<int> DeepCallStack_ErrorOr_9() => DeepCallStack_ErrorOr_10();
    private ErrorOr<int> DeepCallStack_ErrorOr_10() => ErrorOr.Error.Failure(description: "Operation failed");

    private Result<int> DeepCallStack_Ardalis_1() => DeepCallStack_Ardalis_2();
    private Result<int> DeepCallStack_Ardalis_2() => DeepCallStack_Ardalis_3();
    private Result<int> DeepCallStack_Ardalis_3() => DeepCallStack_Ardalis_4();
    private Result<int> DeepCallStack_Ardalis_4() => DeepCallStack_Ardalis_5();
    private Result<int> DeepCallStack_Ardalis_5() => DeepCallStack_Ardalis_6();
    private Result<int> DeepCallStack_Ardalis_6() => DeepCallStack_Ardalis_7();
    private Result<int> DeepCallStack_Ardalis_7() => DeepCallStack_Ardalis_8();
    private Result<int> DeepCallStack_Ardalis_8() => DeepCallStack_Ardalis_9();
    private Result<int> DeepCallStack_Ardalis_9() => DeepCallStack_Ardalis_10();
    private Result<int> DeepCallStack_Ardalis_10() => Result<int>.Error("Operation failed");

    private int GetNumber() => 9999;
}