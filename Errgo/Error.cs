using System.Runtime.CompilerServices;

namespace Errgo;

public readonly record struct Error
{
    private readonly bool _hasError;
    private readonly ErrorChain? _errorChain;
    private readonly ErrorInfo _info;

    /// <summary>
    /// The default constructor creates an Error with no error (Error.None).
    /// </summary>
    public Error()
    {
        _info = new ErrorInfo("Unknown error", string.Empty, string.Empty, 0);
        _hasError = true;
        _errorChain = null;
    }

    public Error(
        string? message = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        message ??= "Unknown error";
        _info = new ErrorInfo(message, memberName, filePath, lineNumber);
        _hasError = true;
        _errorChain = null;
    }

    public Error(
        string message,
        Error error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        message ??= "Unknown error";
        _info = new ErrorInfo(message, memberName, filePath, lineNumber);
        _hasError = true;

        if (error == Error.None)
        {
            _errorChain = null;
            return;
        }

        // Create a chain with just the inner error
        // The current error's info is stored in _info, not duplicated in the chain
        _errorChain = new ErrorChain();
        _errorChain.Append(error);
    }

    /// <summary>
    /// Returns an Error with zeroed values for returning an Error in cases where no error occurred.
    /// </summary>
    public static Error None => default;

    /// <summary>
    /// Instantiates a new Error with no messages or inner errors.
    /// </summary>
    public static Error Empty => new("", "", "", 0);

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message => _info.Message;

    /// <summary>
    /// Gets the error message, with details about the source location info.
    /// </summary>
    public string Details => this.ToString();

    /// <summary>
    /// Gets the member (e.g. method or property) where the error occurred.
    /// </summary>
    public string MemberName => _info.MemberName ?? string.Empty;

    /// <summary>
    /// Gets the source file where the error occurred.
    /// </summary>
    public string FilePath => _info.FilePath ?? string.Empty;

    /// <summary>
    /// Gets the line number where the error occurred.
    /// </summary>
    public int LineNumber => _info.LineNumber;

    /// <summary>
    /// Gets the full error chain as a single message with source location info.
    /// </summary>
    public string Stack
    {
        get
        {
            var lines = new List<string>
            {
                this.ToString()
            };
            
            lines.AddRange(InnerErrors.Select(e => e.ToString()));
            
            return string.Join(Environment.NewLine, lines.Where(l => !string.IsNullOrWhiteSpace(l)));
        }
    }

    /// <summary>
    /// Implicit conversion from Error to bool - true if error exists
    /// </summary>
    /// <param name="error"></param>
    public static implicit operator bool(Error error) => error._hasError;

    /// <summary>
    /// Returns a string representation of the error with source location information.
    /// </summary>
    /// <returns>A string containing the error message and/or source location information. 
    /// Returns an empty string for Error.None.</returns>
    public override string ToString()
    {
        // Error.None should return empty string
        if (this == Error.None)
            return string.Empty;

        var hasMemberName = !string.IsNullOrWhiteSpace(_info.MemberName);
        var hasFilePath = !string.IsNullOrWhiteSpace(_info.FilePath);
        var hasLineNumber = _info.LineNumber > 0;

        // If no source info, just return the message
        if (!hasMemberName)
            return _info.Message;

        var fileName = hasFilePath 
            ? Path.GetFileName(_info.FilePath) 
            : string.Empty;

        // Message with source info
        if (!string.IsNullOrWhiteSpace(fileName) && hasLineNumber)
            return $"{_info.Message} at {_info.MemberName} in {fileName} (line {_info.LineNumber})";

        if (!string.IsNullOrWhiteSpace(fileName))
            return $"{_info.Message} at {_info.MemberName} in {fileName}";

        return $"{_info.Message} at {_info.MemberName}";
    }

    /// <summary>
    /// Gets all errors in the error chain (excluding the current error itself).
    /// </summary>
    public IReadOnlyList<Error> InnerErrors
    {
        get
        {
            var errors = new List<Error>();
            
            if (this == None) return errors;

            if (_errorChain != null)
            {
                CollectErrors(_errorChain.Errors, errors);
            }

            return errors;
        }
    }

    /// <summary>
    /// Recursively collects errors from the specified list and adds them to the provided collection.
    /// </summary>
    /// <remarks>Errors are collected from both direct errors and any nested error chains. 
    /// Each error in the chain is added, followed by recursively collecting from any nested chains.</remarks>
    /// <param name="errors">The list of errors from which to collect. Each error may contain a chain of nested errors.</param>
    /// <param name="collection">The collection to which errors are added. Errors from all chains are appended to this list.</param>
    private static void CollectErrors(IReadOnlyList<Error> errors, List<Error> collection)
    {
        foreach (var error in errors)
        {
            // Add this error (it contains the message/info for this level)
            collection.Add(error);
            
            // If this error has a nested chain, recursively collect from it
            if (error._errorChain != null)
            {
                CollectErrors(error._errorChain.Errors, collection);
            }
        }
    }

    /// <summary>
    /// Checks if this error or any error in its chain matches the target error based on Error message.
    /// </summary>
    /// <remarks>
    /// This comparison only considers the error message text, ignoring source location information
    /// (member name, file path, and line number). This allows matching errors by their semantic
    /// meaning regardless of where they were created.
    /// </remarks>
    /// <param name="target">The target error to check for</param>
    /// <returns>True if this error or any wrapped error has the same message as the target; otherwise, false</returns>
    public bool Is(Error target)
    {
        if (this == Error.None && target == Error.None) return true;
        if (this == Error.None) return false;
        if (target == Error.None) return false;

        // Check this error (compare only message, not source location)
        if (_info.Message.Equals(target._info.Message, StringComparison.Ordinal))
            return true;

        // Check errors in the chain recursively
        if (_errorChain != null)
        {
            foreach (var error in _errorChain.Errors)
            {
                if (error.Is(target)) // Recursive check
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to find an error in the chain that matches the target error based on message text.
    /// </summary>
    /// <remarks>
    /// This method searches through the error chain for an error with the same message as the target.
    /// Source location information (member name, file path, and line number) is ignored in the comparison.
    /// If a match is found, it returns the matched error from the chain, which may have different
    /// source location information than the target.
    /// </remarks>
    /// <param name="target">The target error to search for</param>
    /// <param name="match">When this method returns, contains the matching error if found; otherwise, Error.None</param>
    /// <returns>True if a matching error was found in the chain; otherwise, false</returns>
    public bool As(Error target, out Error match)
    {
        match = Error.None;

        if (this == Error.None) return false;
        if (target == Error.None) return false;

        // Check this error (compare only message, not source location)
        if (_info.Message.Equals(target._info.Message, StringComparison.Ordinal))
        {
            match = this;
            return true;
        }

        // Check errors in the chain recursively
        if (_errorChain != null)
        {
            foreach (var error in _errorChain.Errors)
            {
                if (error.As(target, out match)) // Recursive check
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Wraps errors into the error chain.
    /// Most recent errors are prepended to maintain Go-style ordering (newest first).
    /// </summary>
    /// <param name="errors">Errors to add to the chain</param>
    public void Wrap(params Error[] errors)
    {
        if (errors == null || errors.Length == 0) return;

        // Get or create chain (using Unsafe to bypass readonly)
        var chain = _errorChain;
        if (chain == null)
        {
            chain = new ErrorChain();
            if (!string.IsNullOrWhiteSpace(_info.Message))
            {
                chain.Append(new Error(_info.Message));
            }

            // Set the chain back to the readonly struct field
            Unsafe.AsRef(in _errorChain) = chain;
        }

        // Prepend new errors to the chain (most recent first)
        // Process in reverse order so the first error in the params ends up first
        for (int i = errors.Length - 1; i >= 0; i--)
        {
            var error = errors[i];
            if (error != Error.None)
            {
                // Prepend the complete error (with its chain intact)
                chain.Prepend(error);
            }
        }
    }

    /// <summary>
    /// Determines whether the current error is equal to the specified error based on the presence of an error and the
    /// message text. Custom equality based only on message text, not source location.
    /// </summary>
    /// <remarks>This method ignores source location and compares only the error state and message text. Use
    /// this method when equality should not consider where the error originated.</remarks>
    /// <param name="other">The error to compare with the current error. The comparison is based on whether both errors have occurred and,
    /// if so, their message text.</param>
    /// <returns>true if both errors have the same error state and, if present, identical message text; otherwise, false.</returns>
    public bool Equals(Error other)
    {
        if (_hasError != other._hasError) return false;
        if (!_hasError && !other._hasError) return true;
        return _info.Message == other._info.Message;
    }

    /// <summary>
    /// Returns a hash code for the current object based on its error state and message text.
    /// </summary>
    /// <returns>An integer hash code representing the object's state. Returns 0 if there is no error; otherwise, returns the
    /// hash code of the message text, or 0 if the message text is null.</returns>
    public override int GetHashCode()
    {
        if (!_hasError) return 0;
        return _info.Message?.GetHashCode() ?? 0;
    }
}