using System.Runtime.CompilerServices;

namespace Errgo;

public readonly record struct Error
{
    private readonly bool     isError;
    private readonly string?  message;
    private readonly string?  sourceMemberName;
    private readonly string?  sourceFilePath;
    private readonly int?     sourceLineNumber;
    private readonly Error[]  innerErrors;

    /// <summary>
    /// Creates an error with a default error message.
    /// </summary>
    /// <remarks>
    /// NOTE: Source location information (member name, file path, and line number) is not captured.
    /// </remarks>
    public Error()
    {
        isError = true;
        message = Constants.DefaultErrorMessage;
        sourceMemberName = null;
        sourceFilePath = null;
        sourceLineNumber = null;
        innerErrors = [];
    }

    /// <summary>
    /// Creates an error with the specified message.
    /// </summary>
    /// <param name="message">The error message. If null or empty, defaults to <see cref="Constants.DefaultErrorMessage"/>.</param>
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
        isError = true;
        this.message = message ?? Constants.DefaultErrorMessage;
        sourceMemberName = memberName;
        sourceFilePath = filePath;
        sourceLineNumber = lineNumber;
        innerErrors = [];
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
        isError = true;
        this.message = message ?? Constants.DefaultErrorMessage;
        sourceMemberName = memberName;
        sourceFilePath = filePath;
        sourceLineNumber = lineNumber;

        if (!error.isError)
        {
            innerErrors = [];
            return;
        }

        innerErrors = [error];
    }

    /// <summary>
    /// For internal use only. Creates an Error with the specified isError flag.
    /// Constructing an Error.None with this is faster than using => default;
    /// </summary>
    /// <param name="isError"></param>
    private Error(bool isError)
    {
        this.isError = isError;
        message = null;
        sourceMemberName = null;
        sourceFilePath = null;
        sourceLineNumber = null;
        innerErrors = [];
    }

    /// <summary>
    /// Implicit conversion from Error to bool.
    /// Error.None must evaluate to false, any other Error evaluates to true.
    /// </summary>
    /// <param name="error"></param>
    public static implicit operator bool(Error error) => error.isError;

    /// <summary>
    /// Returns an non-Error with zeroed values. 
    /// Use this for returning an Error type when no error occurred.
    /// </summary>
    public static Error None
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(isError: false);
    }

    /// <summary>
    /// Instantiates a new Error with no messages, no inner errors and no source location information.
    /// Note: an empty error is still considered an error.
    /// </summary>
    public static Error Empty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(message: string.Empty, null, null, null);
    }

    /// <summary>
    /// Creates an <see cref="Error"/> instance representing a sentinel error with the specified message.
    /// </summary>
    /// <param name="message">The error message to associate with the sentinel error. 
    /// This value should describe the error condition.</param>
    /// <remarks>
    /// Sentinel errors do not capture source location information (member name, file path, line number).
    /// Wrap the sentinel error in another <see cref="Error"/> instance if source location is needed.
    /// </remarks>
    /// <returns>An <see cref="Error"/> object initialized with the specified message and default values for other properties.</returns>
    public static Error Sentinel(string message) => new(message, null, null, null);

    /// <summary>
    /// Gets the message associated with this error, without source location information.
    /// </summary>
    public string Message => message ?? string.Empty;

    /// <summary>
    /// Gets the message associated with this error with source location information.
    /// </summary>
    public string MessageDetails => this.ToString();

    /// <summary>
    /// Gets the member (e.g. method or property name) where this error occurred.
    /// </summary>
    public string? Member => sourceMemberName;

    /// <summary>
    /// Gets the source file's path where this error occurred.
    /// </summary>
    public string? Filepath => sourceFilePath;

    /// <summary>
    /// Gets the line number where this error occurred.
    /// </summary>
    public int? LineNum => sourceLineNumber;

    /// <summary>
    /// Gets the full error stack as a single string containing all inner errors (if any) with source location info.
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
    /// Returns a string representation of the error with source location information.
    /// </summary>
    /// <returns>A string containing the error message and/or source location information. 
    /// Returns an empty string for Error.None.</returns>
    public override string ToString()
    {
        if (!this.isError) return string.Empty;

        var hasMemberName = !string.IsNullOrWhiteSpace(this.sourceMemberName);
        if (!hasMemberName) return this.message ?? string.Empty;

        var fileName = !string.IsNullOrWhiteSpace(this.sourceFilePath) 
            ? Path.GetFileName(this.sourceFilePath) 
            : string.Empty;

        if (!string.IsNullOrWhiteSpace(fileName) && this.sourceLineNumber > 0)
            return $"{this.message} at {this.sourceMemberName} in {fileName} (line {this.sourceLineNumber})";

        if (!string.IsNullOrWhiteSpace(fileName))
            return $"{this.message} at {this.sourceMemberName} in {fileName}";

        return $"{this.message} at {this.sourceMemberName}";
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
            if (!this.isError) return [];

            var flatList = new List<Error>();

            if (this.innerErrors is not null)
            {
                CollectErrorsRecursively(this.innerErrors, flatList);
            }

            return flatList;
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

            if (error.innerErrors is not null)
            {
                CollectErrorsRecursively(error.innerErrors, collection);
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
        if (!this.isError && !target.isError) return true;
        if (!this.isError || !target.isError) return false;
        if (this.message is null) return false;
        if (this.message.Equals(target.message, StringComparison.Ordinal)) return true;

        foreach (var error in innerErrors ?? [])
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

        if (!this.isError) return false;
        if (!target.isError) return false;
        if (this.message is null) return false;
        if (this.message.Equals(target.message, StringComparison.Ordinal))
        {
            match = this;
            return true;
        }

        foreach (var error in innerErrors ?? [])
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
    public void Join(params Error[]? errors)
    {
        if (errors == null || errors.Length == 0) return;

        for (int i = errors.Length - 1; i >= 0; i--)
        {
            var error = errors[i];
            if (error.isError)
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

        var innerErrors = this.innerErrors;
        if (innerErrors is null)
        {
            Unsafe.AsRef(in this.innerErrors) = [error];
            return;
        }

        var newArray = new Error[innerErrors.Length + 1];
        newArray[0] = error;
        Array.Copy(innerErrors, 0, newArray, 1, innerErrors.Length);
        Unsafe.AsRef(in this.innerErrors) = newArray;
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
        if (!this.isError && !other.isError) return true;
        if (!this.isError || !other.isError) return false;
        if (message is null || other.message is null) return false;

        return (isError == true && other.isError == true) && 
               (message.Equals(other.message, StringComparison.Ordinal));
    }

    /// <summary>
    /// Returns a hash code for the current object based on its message.
    /// If the error represents no error (Error.None), returns 0.
    /// </summary>
    /// <returns>An integer hash code representing the object's state. 
    /// Returns 0 if this is Error.None; otherwise, returns the
    /// hash code of the message text, or 0 if the message text is null.
    /// </returns>
    public override int GetHashCode()
    {
        if (!this.isError) return 0;
        return this.message?.GetHashCode() ?? 0;
    }

    private static class Constants
    {
        public const string DefaultErrorMessage = "Unknown error";
    }
}