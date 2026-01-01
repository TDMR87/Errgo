namespace Errgo.Examples;

public record class GameSession(string Hash);
public record User(int Id, string Name);

public static class Errors
{
    // Define sentinel errors as static readonly fields for reusability
    public static readonly Error HashNotUnique = new("Hash already exists");
    public static readonly Error DatabaseConnectionFailed = new("Database connection failed");
    public static readonly Error NotFound = new("Item not found");
    public static readonly Error ValidationFailed = new("Validation failed");
    public static readonly Error Unauthorized = new("Unauthorized access");
}

public class Examples
{
    public async Task<(GameSession?, Error)> CreateGameSession(CancellationToken cancellationToken = default)
    {
        var (hash, err) = await GenerateUniqueGameSessionHash();
        if (err) return (null, err);

        (var gameSession, err) = await AddGameSession(hash, cancellationToken);
        if (err) return (null, err);

        return (new GameSession(hash), Error.None);
    }

    private async Task<(GameSession?, Error)> AddGameSession(string hash, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(20);
            // ... code to add the new game session to the database ...
            return (new GameSession(hash), Error.None);
        }
        catch
        {
            return (null, new("Error adding new game session to database"));
        }
    }

    void ExampleUsage()
    {
        var error = DoStuff();
    }

    Error DoStuff()
    {
        return new Error("Something went wrong");
    }

    private async Task<(string, Error)> GenerateUniqueGameSessionHash(int maxAttempts = 10)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            const string chars = "bcdfghjklmnpqrstvwxyz0123456789";
            var hashChars = new char[8];

            for (int i = 0; i < 8; i++)
            {
                hashChars[i] = chars[Random.Shared.Next(chars.Length)];
            }

            var hash = new string(hashChars);

            var (isUnique, err) = await IsHashUnique(hash);
            if (err || !isUnique) return (string.Empty, err);

            return (hash, Error.None);
        }

        return (string.Empty, new Error($"Failed to generate unique game session hash after {maxAttempts} attempts"));
    }

    private async Task<(bool, Error)> IsHashUnique(string hash)
    {
        // ... code to check if the hash is unique in the database ...
        await Task.Delay(20);
        return (false, Errors.HashNotUnique);
    }

    /// <summary>
    /// Demonstrates error propagation through layers (repository -> service).
    /// Shows how to wrap lower-level errors with context at higher levels.
    /// </summary>
    public void RepositoryPatternExample()
    {
        var (user, err) = GetUser(123);
        if (err)
        {
            // Wrap the repository error with service-layer context
            var wrappedErr = new Error("Failed to load user profile", err);

            // Check if specific error is in the chain
            if (wrappedErr.Is(Errors.DatabaseConnectionFailed))
            {
                Console.WriteLine("Database issue detected!");
                Console.WriteLine(wrappedErr.Stack); // Shows full error context
            }
        }
    }

    private (User?, Error) GetUser(int id)
    {
        // Simulate repository returning a database error
        return (null, Errors.DatabaseConnectionFailed);
    }

    /// <summary>
    /// Demonstrates accumulating multiple validation errors.
    /// Useful for form validation where you want to show all errors at once.
    /// </summary>
    public void ValidationScenarioExample()
    {
        var validationError = Error.Empty; // Start with an empty error container

        // Validate multiple fields and accumulate errors
        var (isValid, err) = ValidateEmail("invalid-email");
        if (err) validationError.Join(err);

        (isValid, err) = ValidateAge(-5);
        if (err) validationError.Join(err);

        (isValid, err) = ValidateUsername("");
        if (err) validationError.Join(err);

        if (validationError)
        {
            Console.WriteLine($"Found {validationError.InnerErrors.Count} validation errors:");
            Console.WriteLine(validationError.Stack);

            // Output:
            // Invalid email format
            // Invalid age: must be non-negative
            // Invalid username: cannot be empty
        }
    }

    private (bool, Error) ValidateEmail(string email)
    {
        if (!email.Contains("@"))
            return (false, new Error("Invalid email format"));
        return (true, Error.None);
    }

    private (bool, Error) ValidateAge(int age)
    {
        if (age < 0)
            return (false, new Error("Invalid age: must be non-negative"));
        return (true, Error.None);
    }

    private (bool, Error) ValidateUsername(string username)
    {
        if (string.IsNullOrEmpty(username))
            return (false, new Error("Invalid username: cannot be empty"));
        return (true, Error.None);
    }

    /// <summary>
    /// Demonstrates adding contextual information when wrapping errors.
    /// Useful for providing specific details about what operation failed.
    /// </summary>
    public void ServiceLayerPatternExample()
    {
        var userId = 123;
        var (_, err) = DeleteUser(userId);

        if (err)
        {
            // Add context with specific user ID
            var contextErr = new Error($"Failed to delete user {userId}", err);

            // Check for specific error types in the chain
            if (contextErr.Is(Errors.Unauthorized))
            {
                Console.WriteLine($"Unauthorized to delete user {userId}");
            }
        }
    }

    private (User?, Error) DeleteUser(int id)
    {
        return (null, Errors.Unauthorized);
    }

    /// <summary>
    /// Demonstrates the "Try" pattern similar to int.TryParse.
    /// Returns bool for success/failure and outputs the result and error.
    /// </summary>
    public void TryPatternExample()
    {
        if (!TryParseData("invalid", out var data, out var err))
        {
            Console.WriteLine($"Parse failed: {err.Message}");
            // data is null here
        }
        else
        {
            Console.WriteLine($"Successfully parsed: {data}");
        }
    }

    private bool TryParseData(string input, out object? data, out Error err)
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

    /// <summary>
    /// Demonstrates the early return pattern for error handling.
    /// This is the most common pattern - check error and return immediately.
    /// </summary>
    public void EarlyReturnPatternExample()
    {
        var (result, err) = ProcessWithEarlyReturn();

        if (err)
        {
            Console.WriteLine($"Processing failed: {err.Message}");
            // result is null here
        }
        else
        {
            Console.WriteLine($"Processing succeeded: {result}");
        }
    }

    private (object?, Error) ProcessWithEarlyReturn()
    {
        // Step 1
        var (data, err) = Step1();
        if (err) return (null, err); // Early return on error

        // Step 2 - would only execute if Step 1 succeeded
        (var processed, err) = Step2(data);
        if (err) return (null, err);

        return (processed, Error.None);
    }

    private (string?, Error) Step1()
    {
        return (null, new Error("Step 1 failed"));
    }

    private (object?, Error) Step2(string? input)
    {
        return (new { Data = input }, Error.None);
    }

    /// <summary>
    /// Demonstrates building up error context through multiple layers.
    /// Each layer adds its own context while preserving the inner errors.
    /// </summary>
    public void ChainedOperationsExample()
    {
        var (_, err) = ChainedOperation();

        if (err)
        {
            Console.WriteLine("Operation failed with error chain:");
            Console.WriteLine(err.Stack);

            // Output shows the full chain:
            // Step 3 failed at ...
            // Step 2 failed at ...
            // Step 1 failed at ...

            // You can also access inner errors individually
            foreach (var innerErr in err.InnerErrors)
            {
                Console.WriteLine($"- {innerErr.Message}");
            }
        }
    }

    private (object?, Error) ChainedOperation()
    {
        // Build a chain of errors, each wrapping the previous
        var err1 = new Error("Step 1 failed");
        var err2 = new Error("Step 2 failed", err1);
        var err3 = new Error("Step 3 failed", err2);

        return (null, err3);
    }
}