namespace Errgo.Examples;

public record class GameSession(string Hash);

public class Examples
{
    public async Task<(GameSession?, Error)> CreateGameSession(CancellationToken cancellationToken = default)
    {
        var (hash, err) = await GenerateUniqueGameSessionHash();
        if (err) return (null, err);

        (var gameSession, err) = await AddGameSession(hash, cancellationToken);
        if (err) return (null, err);

        return (gameSession, Error.None);
    }

    private async Task<(GameSession?, Error)> AddGameSession(string hash, CancellationToken cancellationToken)
    {
        try
        {
            // ... code to add the new game session to the database ...
            return (new GameSession(hash), Error.None);
        }
        catch
        {
            return (null, new Error("Error adding new game session to database"));
        }
    }

    private async Task<(string, Error)> GenerateUniqueGameSessionHash(int maxAttempts = 10)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            const string chars = "bcdfghjklmnpqrstvwxyz0123456789"; // vowels removed
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
        return (false, Errors.HashNotUniqueError);
    }
}

public static class Errors
{
    public static Error HashNotUniqueError = new("Hash already exists");
}