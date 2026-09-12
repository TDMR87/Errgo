using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;
using System.Runtime.CompilerServices;
using ErrorOr;
using FluentResults;
using Ardalis.Result;
using LightResults;
using Errgo;

namespace Errgo.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.Method)]
[RankColumn]
public class Benchmark_Comparisons
{
    private readonly Consumer consumer = new();

    [Benchmark(Description = "Propagate error through 10 method calls - Ardalis")]
    public void PropagateThroughDeepCallStack_Ardalis()
    {
        consumer.Consume(DeepCallStack_Ardalis_1());
    }

    [Benchmark(Description = "Propagate error through 10 method calls - Errgo")]
    public void PropagateThroughDeepCallStack_Errgo()
    {
        consumer.Consume(DeepCallStack_Errgo_1());
    }

    [Benchmark(Description = "Propagate error through 10 method calls - LightResults")]
    public void PropagateThroughDeepCallStack_LightResults()
    {
        consumer.Consume(DeepCallStack_LightResults_1());
    }

    [Benchmark(Description = "Propagate error through 10 method calls - ErrorOr")]
    public void PropagateThroughDeepCallStack_ErrorOr()
    {
        consumer.Consume(DeepCallStack_ErrorOr_1());
    }

    [Benchmark(Description = "Propagate error through 10 method calls - Exception")]
    public void PropagateThroughDeepCallStack_Exception()
    {
        try
        {
            DeepCallStack_Exception_1();
            consumer.Consume(new Exception("err"));
        }
        catch (Exception ex)
        {
            consumer.Consume(ex);
        }
    }

    [Benchmark(Description = "Propagate error through 10 method calls - FluentResults")]
    public void PropagateThroughDeepCallStack_FluentResults()
    {
        consumer.Consume(DeepCallStack_FluentResults_1());
    }

    [Benchmark(Description = "Create unsuccessful result - Ardalis")]
    public void CreateError_Ardalis()
    {
        consumer.Consume(Ardalis.Result.Result<int>.Error("Operation failed"));
    }

    [Benchmark(Description = "Create unsuccessful result - Errgo")]
    public void CreateError_Errgo()
    {
        Error result = new Errgo.Error("Operation failed");
        consumer.Consume(result);
    }

    [Benchmark(Description = "Create unsuccessful result - ErrorOr")]
    public void CreateError_ErrorOr()
    {
        var result = ErrorOr.Error.Failure(description: "Operation failed");
        consumer.Consume(result);
    }

    [Benchmark(Description = "Create unsuccessful result - FluentResults")]
    public void CreateError_FluentResults()
    {
        var result = FluentResults.Result.Fail("Operation failed");
        consumer.Consume(result);
    }

    [Benchmark(Description = "Create unsuccessful result - LightResults")]
    public void CreateError_LightResults()
    {
        var result = new LightResults.Error("Operation failed");
        consumer.Consume(result);
    }

    [Benchmark(Description = "Create unsuccessful result (throw) - Exception")]
    public void Throw_Exception()
    {
        try
        {
            throw new InvalidOperationException("Operation failed");
        }
        catch (Exception ex)
        {
            consumer.Consume(ex);
        }
    }

    [Benchmark(Description = "Create successful result - Ardalis")]
    public void CreateSuccess_Ardalis()
    {
        var result = Ardalis.Result.Result<int>.Success(9999);
        consumer.Consume(result);
    }

    [Benchmark(Description = "Create successful result - Errgo")]
    public void CreateSuccess_Errgo()
    {
        (int, Error) result = (9999, Error.None);
        consumer.Consume(result);
    }

    [Benchmark(Description = "Create successful result - LightResults")]
    public void CreateSuccess_LightResults()
    {
        var result = LightResults.Result.Success(9999);
        consumer.Consume(result);
    }

    [Benchmark(Description = "Create successful result - ErrorOr")]
    public void CreateSuccess_ErrorOr()
    {
        var result = (ErrorOr<int>)9999;
        consumer.Consume(result);
    }

    [Benchmark(Description = "Create successful result - FluentResults")]
    public void CreateSuccess_FluentResults()
    {
        var result = FluentResults.Result.Ok(9999);
        consumer.Consume(result);
    }


    [Benchmark(Description = "Multiple chained unsuccessful results - Errgo (Join)")]
    public void ErrorChaining_Errgo_Join()
    {
        var firstError = new Errgo.Error("First error");
        var secondError = new Errgo.Error("Second error");
        var thirdError = new Errgo.Error("Final error");
        var rootError = new Error("Root error");
        rootError = Error.Join(rootError, thirdError, secondError, firstError);
        consumer.Consume(rootError);
    }

    [Benchmark(Description = "Multiple chained unsuccessful results - Errgo (Constructor)")]
    public void ErrorChaining_Errgo_Constructor()
    {
        var firstError = new Errgo.Error("First error");
        var secondError = new Errgo.Error("Second error", firstError);
        var thirdError = new Errgo.Error("Final error", secondError);
        var rootError = new Errgo.Error("Root error", thirdError);
        consumer.Consume(rootError);
    }

    [Benchmark(Description = "Multiple chained unsuccessful results - FluentResults (Caused.By)")]
    public void ErrorChaining_FluentResults()
    {
        var firstError = new FluentResults.Error("First error");
        var secondError = new FluentResults.Error("Second error").CausedBy(firstError);
        var thirdError = new FluentResults.Error("Final error").CausedBy(secondError);
        var rootError = new FluentResults.Error("Root error").CausedBy(thirdError);
        consumer.Consume(rootError);
    }

    [Benchmark(Description = "Multiple chained unsuccessful results - FluentResults (Result.Fail)")]
    public void ErrorChaining_FluentResults_ResultFail()
    {
        FluentResults.Error firstError = new("First error");
        FluentResults.Error secondError = new("Second error", firstError);
        FluentResults.Error thirdError = new("Final error", secondError);
        FluentResults.Result result = FluentResults.Result.Fail(new FluentResults.Error("Root error", thirdError));
        consumer.Consume(result);
    }

    [Benchmark(Description = "Multiple chained unsuccessful results - ErrorOr")]
    public void ErrorChaining_ErrorOr()
    {
        var errors = new List<ErrorOr.Error>();
        errors.Add(ErrorOr.Error.Failure(description: "Root error"));
        errors.Add(ErrorOr.Error.Failure(description: "Final error"));
        errors.Add(ErrorOr.Error.Failure(description: "Second error"));
        errors.Add(ErrorOr.Error.Failure(description: "First error"));

        ErrorOr<int> result = errors;

        consumer.Consume(result);
    }

    [Benchmark(Description = "Multiple chained unsuccessful results - Ardalis")]
    public void ErrorChaining_Ardalis()
    {
        var errors = new List<Ardalis.Result.ValidationError>();
        errors.Add(new Ardalis.Result.ValidationError { ErrorMessage = "Root error" });
        errors.Add(new Ardalis.Result.ValidationError { ErrorMessage = "Final error" });
        errors.Add(new Ardalis.Result.ValidationError { ErrorMessage = "Second error" });
        errors.Add(new Ardalis.Result.ValidationError { ErrorMessage = "First error" });

        var result = Ardalis.Result.Result<int>.Invalid(errors);
        consumer.Consume(result);
    }

    [Benchmark(Description = "Multiple chained unsuccessful results - LightResults")]
    public void ErrorChaining_LightResults()
    {
        var errors = new List<LightResults.Error>();
        errors.Add(new LightResults.Error("Root error"));
        errors.Add(new LightResults.Error("Final error"));
        errors.Add(new LightResults.Error("Second error"));
        errors.Add(new LightResults.Error("First error"));

        var result = LightResults.Result.Failure<int>(errors);
        consumer.Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Error DeepCallStack_Errgo_1() => DeepCallStack_Errgo_2();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Error DeepCallStack_Errgo_2() => DeepCallStack_Errgo_3();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Error DeepCallStack_Errgo_3() => DeepCallStack_Errgo_4();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Error DeepCallStack_Errgo_4() => DeepCallStack_Errgo_5();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Error DeepCallStack_Errgo_5() => DeepCallStack_Errgo_6();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Error DeepCallStack_Errgo_6() => DeepCallStack_Errgo_7();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Error DeepCallStack_Errgo_7() => DeepCallStack_Errgo_8();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Error DeepCallStack_Errgo_8() => DeepCallStack_Errgo_9();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Error DeepCallStack_Errgo_9() => DeepCallStack_Errgo_10();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Error DeepCallStack_Errgo_10() => new Error("Operation failed");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DeepCallStack_Exception_1() => DeepCallStack_Exception_2();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DeepCallStack_Exception_2() => DeepCallStack_Exception_3();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DeepCallStack_Exception_3() => DeepCallStack_Exception_4();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DeepCallStack_Exception_4() => DeepCallStack_Exception_5();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DeepCallStack_Exception_5() => DeepCallStack_Exception_6();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DeepCallStack_Exception_6() => DeepCallStack_Exception_7();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DeepCallStack_Exception_7() => DeepCallStack_Exception_8();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DeepCallStack_Exception_8() => DeepCallStack_Exception_9();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DeepCallStack_Exception_9() => DeepCallStack_Exception_10();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DeepCallStack_Exception_10() => throw new InvalidOperationException("Operation failed");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private FluentResults.Result DeepCallStack_FluentResults_1() => DeepCallStack_FluentResults_2();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private FluentResults.Result DeepCallStack_FluentResults_2() => DeepCallStack_FluentResults_3();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private FluentResults.Result DeepCallStack_FluentResults_3() => DeepCallStack_FluentResults_4();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private FluentResults.Result DeepCallStack_FluentResults_4() => DeepCallStack_FluentResults_5();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private FluentResults.Result DeepCallStack_FluentResults_5() => DeepCallStack_FluentResults_6();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private FluentResults.Result DeepCallStack_FluentResults_6() => DeepCallStack_FluentResults_7();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private FluentResults.Result DeepCallStack_FluentResults_7() => DeepCallStack_FluentResults_8();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private FluentResults.Result DeepCallStack_FluentResults_8() => DeepCallStack_FluentResults_9();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private FluentResults.Result DeepCallStack_FluentResults_9() => DeepCallStack_FluentResults_10();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private FluentResults.Result DeepCallStack_FluentResults_10() => FluentResults.Result.Fail("Operation failed");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ErrorOr<int> DeepCallStack_ErrorOr_1() => DeepCallStack_ErrorOr_2();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private ErrorOr<int> DeepCallStack_ErrorOr_2() => DeepCallStack_ErrorOr_3();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private ErrorOr<int> DeepCallStack_ErrorOr_3() => DeepCallStack_ErrorOr_4();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private ErrorOr<int> DeepCallStack_ErrorOr_4() => DeepCallStack_ErrorOr_5();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private ErrorOr<int> DeepCallStack_ErrorOr_5() => DeepCallStack_ErrorOr_6();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private ErrorOr<int> DeepCallStack_ErrorOr_6() => DeepCallStack_ErrorOr_7();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private ErrorOr<int> DeepCallStack_ErrorOr_7() => DeepCallStack_ErrorOr_8();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private ErrorOr<int> DeepCallStack_ErrorOr_8() => DeepCallStack_ErrorOr_9();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private ErrorOr<int> DeepCallStack_ErrorOr_9() => DeepCallStack_ErrorOr_10();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private ErrorOr<int> DeepCallStack_ErrorOr_10() => ErrorOr.Error.Failure(description: "Operation failed");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Ardalis.Result.Result<int> DeepCallStack_Ardalis_1() => DeepCallStack_Ardalis_2();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Ardalis.Result.Result<int> DeepCallStack_Ardalis_2() => DeepCallStack_Ardalis_3();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Ardalis.Result.Result<int> DeepCallStack_Ardalis_3() => DeepCallStack_Ardalis_4();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Ardalis.Result.Result<int> DeepCallStack_Ardalis_4() => DeepCallStack_Ardalis_5();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Ardalis.Result.Result<int> DeepCallStack_Ardalis_5() => DeepCallStack_Ardalis_6();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Ardalis.Result.Result<int> DeepCallStack_Ardalis_6() => DeepCallStack_Ardalis_7();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Ardalis.Result.Result<int> DeepCallStack_Ardalis_7() => DeepCallStack_Ardalis_8();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Ardalis.Result.Result<int> DeepCallStack_Ardalis_8() => DeepCallStack_Ardalis_9();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Ardalis.Result.Result<int> DeepCallStack_Ardalis_9() => DeepCallStack_Ardalis_10();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Ardalis.Result.Result<int> DeepCallStack_Ardalis_10() => Ardalis.Result.Result<int>.Error("Operation failed");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private LightResults.Result DeepCallStack_LightResults_1() => DeepCallStack_LightResults_2();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private LightResults.Result DeepCallStack_LightResults_2() => DeepCallStack_LightResults_3();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private LightResults.Result DeepCallStack_LightResults_3() => DeepCallStack_LightResults_4();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private LightResults.Result DeepCallStack_LightResults_4() => DeepCallStack_LightResults_5();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private LightResults.Result DeepCallStack_LightResults_5() => DeepCallStack_LightResults_6();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private LightResults.Result DeepCallStack_LightResults_6() => DeepCallStack_LightResults_7();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private LightResults.Result DeepCallStack_LightResults_7() => DeepCallStack_LightResults_8();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private LightResults.Result DeepCallStack_LightResults_8() => DeepCallStack_LightResults_9();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private LightResults.Result DeepCallStack_LightResults_9() => DeepCallStack_LightResults_10();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private LightResults.Result DeepCallStack_LightResults_10() => new LightResults.Error("Operation failed");
}
