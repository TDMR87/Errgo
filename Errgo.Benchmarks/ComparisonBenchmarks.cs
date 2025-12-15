using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ErrorOr;
using Ardalis.Result;

namespace Errgo.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ComparisonBenchmarks
{
    #region Callstack

    [Benchmark(Description = "Errgo - Through 10 method calls")]
    public Errgo.Error ErrgoThroughDeepCallStack()
    {
        var err = DeepCallStack_Errgo_1();
        return err;
    }

    [Benchmark(Description = "Exception - Through 10 method calls")]
    public Exception ExceptionThroughDeepCallStack()
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

    [Benchmark(Description = "FluentResults - Through 10 method calls")]
    public FluentResults.Result FluentResultsThroughDeepCallStack()
    {
        return DeepCallStack_FluentResults_1();
    }

    [Benchmark(Description = "ErrorOr - Through 10 method calls")]
    public ErrorOr<int> ErrorOrThroughDeepCallStack()
    {
        return DeepCallStack_ErrorOr_1();
    }

    [Benchmark(Description = "Ardalis - Through 10 method calls")]
    public Ardalis.Result.Result<int> ArdalisThroughDeepCallStack()
    {
        return DeepCallStack_Ardalis_1();
    }

    #endregion

    #region Return unsuccessful result

    [Benchmark(Description = "Errgo - Return unsuccessful result")]
    public Errgo.Error ErrgoCreateError()
    {
        return new Errgo.Error("Operation failed");
    }

    [Benchmark(Description = "FluentResults - Return unsuccessful result")]
    public FluentResults.Result FluentResultsCreateError()
    {
        return FluentResults.Result.Fail("Operation failed");
    }

    [Benchmark(Description = "ErrorOr - Return unsuccessful result")]
    public ErrorOr<int> ErrorOrCreateError()
    {
        return ErrorOr.Error.Failure(description: "Operation failed");
    }

    [Benchmark(Description = "Ardalis - Return unsuccessful result")]
    public Ardalis.Result.Result<int> ArdalisCreateError()
    {
        return Result<int>.Error("Operation failed");
    }

    [Benchmark(Description = "Exception - Return unsuccessful result (throw)")]
    public Exception ExceptionThrow()
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

    [Benchmark(Description = "Errgo - return successful result")]
    public (int, Error) ErrgoCreateSuccess()
    {
        var number = GetNumber();
        return (number, Error.None);
    }

    [Benchmark(Description = "FluentResults - Return successful result")]
    public FluentResults.Result<int> FluentResultsCreateSuccess()
    {
        var number = GetNumber();
        return FluentResults.Result.Ok(number);
    }

    [Benchmark(Description = "ErrorOr - Return successful result")]
    public ErrorOr<int> ErrorOrCreateSuccess()
    {
        var number = GetNumber();
        return number;
    }

    [Benchmark(Description = "Ardalis - Return successful result")]
    public Ardalis.Result.Result<int> ArdalisCreateSuccess()
    {
        var number = GetNumber();
        return Result<int>.Success(number);
    }

    #endregion

    #region Wrap unsuccessful result

    [Benchmark(Description = "Errgo - Multiple chained unsuccessful results")]
    public Error ErrgoErrorChaining()
    {
        var firstError = new Errgo.Error("First error");
        var secondError = new Errgo.Error("Second error", firstError);
        var thirdError = new Errgo.Error("Final error", secondError);

        var rootError = new Error("Root");
        rootError.Wrap(thirdError);
        return rootError;
    }

    [Benchmark(Description = "FluentResults - Multiple chained unsuccessful results")]
    public FluentResults.Result FluentResultsErrorChaining()
    {
        var firstError = new FluentResults.Error("First error");
        var secondError = new FluentResults.Error("Second error").CausedBy(firstError);
        var thirdError = new FluentResults.Error("Final error").CausedBy(secondError);

        var rootError = new FluentResults.Error("Root error").CausedBy(thirdError);
        return rootError;
    }

    [Benchmark(Description = "ErrorOr - Multiple chained unsuccessful results")]
    public ErrorOr<int> ErrorOrErrorChaining()
    {
        // ErrorOr doesn't support hierarchical error chaining
        // flat list instead
        var firstError = ErrorOr.Error.Validation();
        var secondError = ErrorOr.Error.Forbidden();
        var thirdError = ErrorOr.Error.NotFound();

        var rootError = new List<ErrorOr.Error> 
        { 
            firstError, 
            secondError, 
            thirdError 
        };

        return rootError;
    }

    [Benchmark(Description = "Ardalis - Multiple chained unsuccessful results")]
    public Ardalis.Result.Result<int> ArdalisErrorChaining()
    {
        // Ardalis.Result doesn't support hierarchical error chaining
        // flat list instead
        var errors = new List<string> 
        { 
            "First error", 
            "Second error", 
            "Third error" 
        };

        var result = Result<int>.Error(new ErrorList(errors));
        return result;
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