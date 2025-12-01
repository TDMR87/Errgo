namespace Errgo;

/// <summary>
/// Internal wrapper for error chaining to avoid struct cycle.
/// </summary>
internal sealed class InnerError(Error error)
{
    public Error Error { get; } = error;
}

public readonly record struct Error
{
    private readonly bool _hasError;
    private readonly InnerError? _innerError;

    public Error()
    {
        Message = string.Empty;
        _hasError = true;
        _innerError = null;
    }

    public Error(string message)
    {
        Message = message;
        _hasError = true;
        _innerError = null;
    }

    public Error(string message, Error error)
    {
        Message = message;
        _hasError = true;
        _innerError = new InnerError(error);
    }

    /// <summary>
    /// Gets a message that describes the current error.
    /// </summary>
    public readonly string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the inner error that was wrapped by this error, if any.
    /// </summary>
    public readonly Error? InnerError => _innerError?.Error;

    /// <summary>
    /// Gets the full error chain as a single message with all errors joined.
    /// </summary>
    public readonly string FullMessage
    {
        get
        {
            if (this == None) return string.Empty;

            var messages = new List<string>();
            Error current = this;

            while (current)
            {
                messages.Add(current.Message);
                if (current._innerError == null) break;
                current = current._innerError.Error;
            }

            return string.Join(". ", messages);
        }
    }

    /// <summary>
    /// Gets all error messages in the chain as a read-only list.
    /// </summary>
    public readonly IReadOnlyList<string> Messages
    {
        get
        {
            var messages = new List<string>();
            
            if (this == None) return messages;

            Error current = this;

            while (current)
            {
                messages.Add(current.Message);
                if (current._innerError == null) break;
                current = current._innerError.Error;
            }

            return messages;
        }
    }

    /// <summary>
    /// Returns an empty Error for no-error cases.
    /// A boolean check to Error.None will return false.
    /// </summary>
    public static Error None { get; }

    /// <summary>
    /// Checks if this error or any error in its chain matches the target error.
    /// </summary>
    /// <param name="target">The target error to check for</param>
    /// <returns>True if this error or any wrapped error equals the target</returns>
    public readonly bool Is(Error target)
    {
        if (!target) return false;
        
        Error current = this;
        while (current)
        {
            if (current == target) return true;
            if (current._innerError == null) break;
            current = current._innerError.Error;
        }

        return false;
    }

    /// <summary>
    /// Implicit conversion from Error to bool - true if error exists
    /// </summary>
    /// <param name="error"></param>
    public static implicit operator bool(Error error) => error._hasError;

    /// <summary>
    /// Implicit conversion from string to Error
    /// </summary>
    /// <param name="message"></param>
    public static implicit operator Error(string message) => new(message);
}