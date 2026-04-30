namespace Errgo.Examples;

public record class GameSession(string Hash);
public record User(int Id, string Name);

public static class Errors
{
    // Define sentinel errors as static readonly fields for reusability
    public static readonly Error HashNotUnique      = Error.Sentinel("Hash already exists");
    public static readonly Error DbConnectionFailed = Error.Sentinel("Database connection failed");
    public static readonly Error NotFound           = Error.Sentinel("Not found");
    public static readonly Error ValidationFailed   = Error.Sentinel("Validation failed");
    public static readonly Error Unauthorized       = Error.Sentinel("Unauthorized");
}

class MyType
{
    public int MyProperty { get; set; }
    public Error Error { get; set; }
}

public class Examples
{
    public void ValidationScenarioExample()
    {
        var errors = Error.Empty;

        var (isValid, err) = ValidateEmail("invalid-email@domain.com");
        if (err) errors = Error.Join(errors, err);

        (isValid, err) = ValidateAge(-5);
        if (err) errors = Error.Join(errors, err);

        (isValid, err) = ValidateUsername("");
        if (err) errors = Error.Join(errors, err);

        if (errors)
        {
            Console.WriteLine($"Found {errors.InnerErrors.Count} validation errors:");
            Console.WriteLine(errors.Stack);
        }
    }

    private (bool, Error) ValidateEmail(string email)
    {
        return !email.Contains("@") 
            ? (false, new Error("Invalid email format")) 
            : (true, Error.None);
    }

    private (bool, Error) ValidateAge(int age)
    {
        return age < 0 
            ? (false, new Error("Invalid age: must be non-negative")) 
            : (true, Error.None);
    }

    private (bool, Error) ValidateUsername(string username)
    {
        return string.IsNullOrEmpty(username) 
            ? (false, new Error("Invalid username: cannot be empty")) 
            : (true, Error.None);
    }

    public static void TryPatternExample()
    {
        if (!TryParseData("invalid", out var data, out var err))
            Console.WriteLine($"Parse failed: {err.Message}");
        else
            Console.WriteLine($"Successfully parsed: {data}");
    }

    private static bool TryParseData(string input, out object? data, out Error err)
    {
        data = null;
        if (input == "invalid")
        {
            err = new Error("Parse failed");
            return false;
        }

        data = new { Value = input };
        err = Error.None;
        return true;
    }

    public static void EarlyReturnPatternExample()
    {
        var (result, err) = ProcessWithEarlyReturn();

        if (err) Console.WriteLine($"Processing failed: {err.Message}");
        else Console.WriteLine($"Processing succeeded: {result}");
    }

    private static (object?, Error) ProcessWithEarlyReturn()
    {
        var (data, err) = Step1();
        if (err) return (null, err);

        (var processed, err) = Step2(data);
        if (err) return (null, err);

        return (processed, Error.None);
    }

    private static (string?, Error) Step1() => (null, new Error("Step 1 failed"));

    private static (object?, Error) Step2(string? input) => (new { Data = input }, Error.None);

    public static (object?, Error) SwitchCaseExample() => ProcessWithEarlyReturn() switch
    {
        (null, var err) => (null, err),
        var (obj, _) => (obj, Error.None),
    };
}
