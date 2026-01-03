using System.Runtime.CompilerServices;

namespace Errgo;

internal static class Constants
{
    public const string UnknownError = "Unknown error";
}

public readonly record struct Error
{
    private readonly bool _isError;
    private readonly string? _message;
    private readonly string? _sourceMemberName;
    private readonly string? _sourceFilePath;
    private readonly int? _sourceLineNumber;
    private readonly Error[]? _innerErrors;

    /// <summary>
    /// Creates an error with a default error message.
    /// </summary>
    /// <remarks>
    /// NOTE: Source location information (member name, file path, and line number) is not captured.
    /// </remarks>
    public Error()
    {
        _isError = true;
        _message = Constants.UnknownError;
        _sourceMemberName = null;
        _sourceFilePath = null;
        _sourceLineNumber = null;
        _innerErrors = null;
    }

    /// <summary>
    /// Creates an error with the specified message.
    /// </summary>
    /// <param name="message">The error message. If null or empty, defaults to "Unknown error".</param>
    /// <param name="memberName">The member name where the error occurred. Automatically filled by the compiler.</param>
    /// <param name="filePath">The source file path where the error occurred. Automatically filled by the compiler.</param>
    /// <param name="lineNumber">The line number where the error occurred. Automatically filled by the compiler.</param>
    /// <remarks>
    /// The optional parameters for source location (memberName, filePath, lineNumber) are automatically 
    /// captured by the compiler using caller information attributes and should not be manually provided 
    /// in typical usage.
    /// </remarks>
    public Error(
        string? message,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int? lineNumber = null)
    {
        _isError = true;
        _message = message ?? Constants.UnknownError;
        _sourceMemberName = memberName;
        _sourceFilePath = filePath;
        _sourceLineNumber = lineNumber;
        _innerErrors = null;
    }

    /// <summary>
    /// Creates an error with the specified message and wraps an inner error, forming an error chain.
    /// </summary>
    /// <param name="message">The error message for this error. If null or empty, defaults to "Unknown error".</param>
    /// <param name="error">The inner error to wrap in the chain.</param>
    /// <param name="memberName">The member name where the error occurred. Automatically filled by the compiler.</param>
    /// <param name="filePath">The source file path where the error occurred. Automatically filled by the compiler.</param>
    /// <param name="lineNumber">The line number where the error occurred. Automatically filled by the compiler.</param>
    /// <remarks>
    /// This constructor creates an error chain where the current error wraps the provided inner error.
    /// The optional parameters for source location (memberName, filePath, lineNumber) are automatically 
    /// captured by the compiler using caller information attributes and should not be manually provided 
    /// in typical usage.
    /// </remarks>
    public Error(
        string message,
        Error error,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int? lineNumber = null)
    {
        _isError = true;
        _message = message ?? Constants.UnknownError;
        _sourceMemberName = memberName;
        _sourceFilePath = filePath;
        _sourceLineNumber = lineNumber;

        if (!error._isError)
        {
            _innerErrors = null;
            return;
        }

        _innerErrors = [error];
    }

    /// <summary>
    /// For internal use only. Creates an Error with the specified isError flag.
    /// </summary>
    /// <param name="isError"></param>
    private Error(bool isError)
    {
        _isError = isError;
        _message = null;
        _sourceMemberName = null;
        _sourceFilePath = null;
        _sourceLineNumber = null;
        _innerErrors = null;
    } 

    /// <summary>
    /// Returns an non-Error with zeroed values. 
    /// Use this for returning an Error type when no error occurred.
    /// </summary>
    public static Error None => new(isError: false);

    /// <summary>
    /// Instantiates a new Error with no messages, no inner errors and no source location information.
    /// Note: an empty error is still considered an error.
    /// </summary>
    public static Error Empty => new(message: "", null, null, null);

    /// <summary>
    /// Gets the message associated with this error, without source location information.
    /// </summary>
    public string Message => _message ?? string.Empty;

    /// <summary>
    /// Gets the message associated with this error with source location information.
    /// </summary>
    public string MessageDetails => this.ToString();

    /// <summary>
    /// Gets the member (e.g. method or property) where this error occurred.
    /// </summary>
    public string SourceMemberName => _sourceMemberName ?? string.Empty;

    /// <summary>
    /// Gets the source file where this error occurred.
    /// </summary>
    public string SourceFilePath => _sourceFilePath ?? string.Empty;

    /// <summary>
    /// Gets the line number where this error occurred.
    /// </summary>
    public int SourceLineNumber => _sourceLineNumber ?? 0;

    /// <summary>
    /// Gets the full error stack as a single string containing all inner errors (if any) 
    /// with their source location info.
    /// </summary>
    public string Stack
    {
        get
        {
            var lines = new List<string> { this.ToString() };
            lines.AddRange(InnerErrors.Select(e => e.ToString()));
            return string.Join(Environment.NewLine, lines.Where(l => !string.IsNullOrWhiteSpace(l)));
        }
    }

    /// <summary>
    /// Implicit conversion from Error to bool.
    /// Error.None should evaluate to false, any other Error evaluates to true.
    /// </summary>
    /// <param name="error"></param>
    public static implicit operator bool(Error error) => error._isError;

    /// <summary>
    /// Returns a string representation of the error with source location information.
    /// </summary>
    /// <returns>A string containing the error message and/or source location information. 
    /// Returns an empty string for Error.None.</returns>
    public override string ToString()
    {
        if (!_isError) return string.Empty;

        var hasMemberName = !string.IsNullOrWhiteSpace(_sourceMemberName);
        if (!hasMemberName) return _message ?? string.Empty;

        var fileName = !string.IsNullOrWhiteSpace(_sourceFilePath) 
            ? Path.GetFileName(_sourceFilePath) 
            : string.Empty;

        if (!string.IsNullOrWhiteSpace(fileName) && _sourceLineNumber > 0)
            return $"{_message} at {_sourceMemberName} in {fileName} (line {_sourceLineNumber})";

        if (!string.IsNullOrWhiteSpace(fileName))
            return $"{_message} at {_sourceMemberName} in {fileName}";

        return $"{_message} at {_sourceMemberName}";
    }

    /// <summary>
    /// Recursively gets all inner errors in the error chain as a flat list (excluding the current error itself).
    /// </summary>
    /// <remarks>
    /// This property traverses through the entire error chain and collects all inner errors into a single flat list.
    /// Use <see cref="Is"/> to check if a specific error exists in the chain.
    /// Use <see cref="As"/> to find and extract a specific error from the chain.
    /// </remarks>
    public IReadOnlyList<Error> InnerErrors
    {
        get
        {
            if (!_isError) return [];

            var errorsFlattened = new List<Error>();

            if (_innerErrors is not null)
            {
                CollectErrorsRecursively(_innerErrors, errorsFlattened);
            }

            return errorsFlattened;
        }
    }

    /// <summary>
    /// Recursively collects errors from the specified error chain and flattens them to the provided errors collection.
    /// </summary>
    /// <remarks> Each error in the chain is added, followed by recursively collecting from any nested chains.</remarks>
    /// <param name="errors">The error chain from which to collect. Each error may contain a chain of nested errors.</param>
    /// <param name="collection">The collection to which errors are added. Errors from all chains are flattened to this list.</param>
    private static void CollectErrorsRecursively(Error[] errors, ICollection<Error> collection)
    {
        foreach (var error in errors)
        {
            collection.Add(error);

            if (error._innerErrors is not null)
            {
                CollectErrorsRecursively(error._innerErrors, collection);
            }
        }
    }

    /// <summary>
    /// Checks if this error or any error in its inner errors matches the target error.
    /// </summary>
    /// <remarks>
    /// This comparison only considers the error message, ignoring any source location information
    /// (member name, file path, and line number). This allows matching errors by their semantic
    /// meaning regardless of where they were created.
    /// </remarks>
    /// <param name="target">The target error to check for</param>
    /// <returns>True if this error or any wrapped error has the same message as the target; otherwise, false</returns>
    public bool Is(Error target)
    {
        if (!this._isError && !target._isError) return true;
        if (!this._isError || !target._isError) return false;
        if (_message is null) return false;
        if (_message.Equals(target._message, StringComparison.Ordinal)) return true;

        foreach (var error in _innerErrors ?? Enumerable.Empty<Error>())
        {
            if (error.Is(target)) return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to find an error in the inner error chain that matches the target error.
    /// </summary>
    /// <remarks>
    /// This method searches through the inner error chain for an error with the same message as the target.
    /// Source location information (member name, file path, and line number) is ignored.
    /// If a match is found, it returns the first occurrence of a matched error from the chain, 
    /// which may have different source location information than the target.
    /// </remarks>
    /// <param name="target">The target error to search for</param>
    /// <param name="match">When this method returns, contains the matching error if found; otherwise, Error.None</param>
    /// <returns>True if a matching error was found in the chain; otherwise, false</returns>
    public bool As(Error target, out Error match)
    {
        match = Error.None;

        if (!this._isError) return false;
        if (!target._isError) return false;
        if (_message is null) return false;
        if (_message.Equals(target._message, StringComparison.Ordinal))
        {
            match = this;
            return true;
        }

        foreach (var error in _innerErrors ?? Enumerable.Empty<Error>())
        {
            if (error.As(target, out match)) return true;
        }

        return false;
    }

    /// <summary>
    /// Joins errors into the inner error chain.
    /// Most recent errors are placed first in the chain.
    /// </summary>
    /// <param name="errors">Errors to add to the chain</param>
    public void Join(params Error[] errors)
    {
        if (errors == null || errors.Length == 0) return;

        // Prepend the most recent error first (reverse order)
        for (int i = errors.Length - 1; i >= 0; i--)
        {
            var error = errors[i];
            if (error._isError)
            {
                PrependInnerErrors(error);
            }
        }
    }

    /// <summary>
    /// Adds the specified error to the front of the inner error chain of this error instance.
    /// Error.None errors are ignored and are not added to the chain.
    /// </summary>
    /// <param name="error"></param>
    private void PrependInnerErrors(Error error)
    {
        if (!error) return;

        var innerErrors = _innerErrors;
        
        if (innerErrors is null)
        {
            Unsafe.AsRef(in _innerErrors) = [error];
            return;
        }

        var newArray = new Error[innerErrors.Length + 1];
        newArray[0] = error;
        Array.Copy(innerErrors, 0, newArray, 1, innerErrors.Length);
        Unsafe.AsRef(in _innerErrors) = newArray;
    }

    /// <summary>
    /// Determines whether the current error is equal to the specified error.
    /// </summary>
    /// <remarks>This method ignores source location and compares only the error state and message text. 
    /// Use this method when equality should not consider where the error originated.</remarks>
    /// <param name="other">The error to compare with the current error.</param>
    /// <returns>true if both errors have the same error state and, if present, identical message text; otherwise, false.</returns>
    public bool Equals(Error other)
    {
        if (!this._isError && !other._isError) return true;
        if (!this._isError || !other._isError) return false;
        if (_message is null || other._message is null) return false;

        return (_isError == true && other._isError == true) && 
               (_message.Equals(other._message, StringComparison.Ordinal));
    }

    /// <summary>
    /// Returns a hash code for the current object based on its message.
    /// If the error represents no error (Error.None), returns 0.
    /// </summary>
    /// <returns>An integer hash code representing the object's state. 
    /// Returns 0 if there is no error; otherwise, returns the
    /// hash code of the message text, or 0 if the message text is null.
    /// </returns>
    public override int GetHashCode()
    {
        if (!_isError) return 0;
        return _message?.GetHashCode() ?? 0;
    }
}