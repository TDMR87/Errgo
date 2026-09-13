using System.Runtime.CompilerServices;

namespace Errgo;

public readonly record struct Error
{
    private readonly string?  message;
    private readonly string?  sourceMemberName;
    private readonly string?  sourceFilePath;
    private readonly int?     sourceLineNumber;
    private readonly Error[]? innerErrors;

    /// <summary>
    /// A single discriminant distinguishing the runtime-default value from explicitly constructed values.
    /// default(Error) results in state = <see cref="ErrorState.Default"/> and is still treated as an error.
    /// Error.None (state = <see cref="ErrorState.None"/>) is treated as a non-error.
    /// </summary>
    private readonly ErrorState state;

    /// <summary>
    /// Creates an error with a default error message.
    /// </summary>
    /// <remarks>
    /// NOTE: Source location information (member name, file path, and line number) is not captured.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Error()
    {
        this.state            = ErrorState.Constructed;
        this.sourceMemberName = null;
        this.sourceFilePath   = null;
        this.sourceLineNumber = null;
        this.innerErrors      = null;
        this.message          = Constants.DefaultErrorMessage;
    }

    /// <summary>
    /// Creates an error with the specified message.
    /// </summary>
    /// <param name="message">The error message. If null, defaults to default error message.</param>
    /// <param name="memberName">The member name where the error occurred. Automatically filled by the compiler.</param>
    /// <param name="filePath">The source file path where the error occurred. Automatically filled by the compiler.</param>
    /// <param name="lineNumber">The line number where the error occurred. Automatically filled by the compiler.</param>
    /// <remarks>
    /// The optional parameters for source location (memberName, filePath, lineNumber) are automatically 
    /// captured by the compiler using caller information attributes and should not be manually provided 
    /// in typical usage.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Error(
        string? message = Constants.DefaultErrorMessage,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int? lineNumber = null)
    {
        this.state            = ErrorState.Constructed;
        this.sourceMemberName = memberName;
        this.sourceFilePath   = filePath;
        this.sourceLineNumber = lineNumber;
        this.innerErrors      = null;
        this.message          = NormalizeMessage(message);
    }

    /// <summary>
    /// Creates an error with the specified message and wraps an inner error, forming an error chain.
    /// </summary>
    /// <param name="message">The error message for this error. If null, default error message.</param>
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Error(
        string message,
        in Error error,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int? lineNumber = null)
    {
        this.state            = ErrorState.Constructed;
        this.sourceMemberName = memberName;
        this.sourceFilePath   = filePath;
        this.sourceLineNumber = lineNumber;
        this.message          = NormalizeMessage(message);
        if (error.IsError) this.innerErrors = [error];
    }

    /// <summary>
    /// For internal use only (Error.None). Creates an Error with the specified isError state.
    /// </summary>
    /// <param name="isError"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Error(bool isError)
    {
        this.state            = isError ? ErrorState.Constructed : ErrorState.None;
        this.message          = null;
        this.sourceMemberName = null;
        this.sourceFilePath   = null;
        this.sourceLineNumber = null;
        this.innerErrors      = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Error(
        bool isError,
        string? message,
        string? sourceMemberName,
        string? sourceFilePath,
        int? sourceLineNumber,
        Error[]? innerErrors)
    {
        this.state            = isError ? ErrorState.Constructed : ErrorState.None;
        this.message          = message;
        this.sourceMemberName = sourceMemberName;
        this.sourceFilePath   = sourceFilePath;
        this.sourceLineNumber = sourceLineNumber;
        this.innerErrors      = innerErrors;
    }

    /// <summary>
    /// Implicit conversion from Error to bool.
    /// 
    /// NOTE: Error.None must evaluate to false, any other Error evaluates to true.
    /// </summary>
    /// <param name="error"></param>
    public static implicit operator bool(Error error) => error.IsError;

    /// <summary>
    /// Determines whether the current error instance represents an error or a non-error (Error.None)
    /// </summary>
    /// <remarks>
    /// Always use this property to check if an Error instance represents an error or not.
    /// </remarks>
    private bool IsError => state != ErrorState.None;

    /// <summary>
    /// Returns the canonical non-error sentinel value.
    /// Use this for returning an <see cref="Error"/> when no error occurred.
    /// </summary>
    public static Error None
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(isError: false);
    }

    /// <summary>
    /// Instantiates a new Error with no message, no inner errors and no source location information.
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
    /// Join combines the given errors, returning a new Error where the given 
    /// Errors are the new Error's inner errors. 
    /// 
    /// Discards any null or a non-error values.
    /// 
    /// Returns <see cref="Error.None"/> if every value in <paramref name="errors"/> is null or non-error.
    /// 
    /// Each errors existing inner errors are preserved and appended to the inner errors 
    /// of the returned Error in argument order.
    /// </summary>
    /// <remarks>
    /// An <see cref="Error"/> value returned by Join exposes joined errors through <see cref="InnerErrors"/>.
    /// Joined errors may be inspected with <see cref="Is"/> and <see cref="As"/>.
    /// </remarks>
    /// <param name="errors">Errors to join.</param>
    /// <returns>
    /// <see cref="Error.None"/> if every value in <paramref name="errors"/> is null or a non-error;
    /// otherwise an aggregated <see cref="Error"/>.
    /// </returns>
    public static Error Join(params Error?[]? errors)
    {
        if (errors is null || errors.Length == 0)
        {
            return None;
        }

        // Filter out non-errors
        var validErrors = new List<Error>(errors.Length);
        for (int i = 0; i < errors.Length; i++)
        {
            if (errors[i] is Error e && e.IsError)
            {
                validErrors.Add(e);
            }
        }

        if (validErrors.Count == 0) return None;
        if (validErrors.Count == 1) return validErrors[0];

        var firstError = validErrors[0];
        var rootErrInnerCount = firstError.innerErrors?.Length ?? 0;

        // Holds all the joined errors to be returned
        var joinedErrors = new Error[rootErrInnerCount + (validErrors.Count - 1)];

        // Each valid error might themselves contain inner errors.
        // Root error's inner errors are placed first into the destination array.
        if (rootErrInnerCount > 0) 
        {
            Array.Copy(
                sourceArray: firstError.innerErrors,
                sourceIndex: 0,
                destinationArray: joinedErrors,
                destinationIndex: 0,
                length: rootErrInnerCount);
        }

        // Append the additional errors after the root's errors
        for (int i = 1; i < validErrors.Count; i++)
        {
            joinedErrors[rootErrInnerCount++] = validErrors[i];
        }

        return new Error(
            isError: firstError.IsError,
            message: firstError.message,
            sourceMemberName: firstError.sourceMemberName,
            sourceFilePath: firstError.sourceFilePath,
            sourceLineNumber: firstError.sourceLineNumber,
            innerErrors: joinedErrors);
    }

    /// <summary>
    /// Gets the message associated with this error, without source location information.
    /// </summary>
    public string Message
    {
        get
        {
            if (!this.IsError) return string.Empty;
            return message ?? Constants.DefaultErrorMessage;
        }
    }

    /// <summary>
    /// Gets the message associated with this error with source location information.
    /// </summary>
    public string MessageDetails => this.ToString();

    /// <summary>
    /// Gets the member (e.g. method or property name) where this error occurred.
    /// </summary>
    public string? SourceMemberName => sourceMemberName;

    /// <summary>
    /// Gets the source file's path where this error occurred.
    /// </summary>
    public string? SourceFilePath => sourceFilePath;

    /// <summary>
    /// Gets the line number where this error occurred.
    /// </summary>
    public int? SourceLineNumber => sourceLineNumber;

    /// <summary>
    /// Gets the full error stack as a single string containing all inner errors (if any) with source location info.
    /// </summary>
    public string Stack
    {
        get
        {
            if (!this.IsError) return string.Empty;

            var current = this.ToString();
            if (innerErrors is null)
            {
                return string.IsNullOrWhiteSpace(current) ? string.Empty : current;
            }

            var builder = new System.Text.StringBuilder();
            var hasLine = false;

            if (!string.IsNullOrWhiteSpace(current))
            {
                builder.Append(current);
                hasLine = true;
            }

            AppendStack(innerErrors, builder, ref hasLine);
            return builder.ToString();
        }
    }

    /// <summary>
    /// Returns a string representation of the error with source location information.
    /// </summary>
    /// <returns>A string containing the error message and/or source location information. 
    /// Returns an empty string for Error.None.</returns>
    public override string ToString()
    {
        if (!this.IsError) return string.Empty;

        var message = this.Message;
        var memberName = string.IsNullOrWhiteSpace(this.sourceMemberName) ? null : this.sourceMemberName;
        var hasMemberName = memberName is not null;
        var fileNameRange = GetFileNameRange(this.sourceFilePath);
        var hasFileName = fileNameRange.Length > 0;
        var hasLineNumber = this.sourceLineNumber > 0;

        if (!hasMemberName && !hasFileName && !hasLineNumber)
            return message;

        var includeLineNumber = hasLineNumber && (!hasMemberName || hasFileName);
        var lineNumber = includeLineNumber ? this.sourceLineNumber!.Value : 0;
        var memberNameLength = hasMemberName ? memberName?.Length ?? 0 : 0;

        var length = message.Length;
        if (hasMemberName)length += 4 + memberNameLength;
        if (hasFileName)length += 4 + fileNameRange.Length;
        if (includeLineNumber) length += 6 + CountDigits(lineNumber);

        return string.Create(length, new ToStringState(
            message,
            memberName,
            this.sourceFilePath,
            fileNameRange.Start,
            fileNameRange.Length,
            lineNumber,
            hasMemberName,
            hasFileName,
            includeLineNumber), static (buffer, state) =>
        {
            var written = 0;

            state.Message.AsSpan().CopyTo(buffer);
            written += state.Message.Length;

            if (state.HasMemberName)
            {
                " at ".AsSpan().CopyTo(buffer[written..]);
                written += 4;

                var memberName = state.MemberName;
                if (memberName is not null)
                {
                    memberName.AsSpan().CopyTo(buffer[written..]);
                    written += memberName.Length;
                }
            }

            if (state.HasFileName)
            {
                " in ".AsSpan().CopyTo(buffer[written..]);
                written += 4;

                state.FilePath!.AsSpan(state.FileNameStart, state.FileNameLength).CopyTo(buffer[written..]);
                written += state.FileNameLength;
            }

            if (state.IncludeLineNumber)
            {
                ":line ".AsSpan().CopyTo(buffer[written..]);
                written += 6;
                state.LineNumber.TryFormat(buffer[written..], out var charsWritten);
            }
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (int Start, int Length) GetFileNameRange(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return default;

        var span = path.AsSpan();
        var end = span.Length;

        while (end > 0 && IsDirectorySeparator(span[end - 1])) end--;

        if (end == 0) return default;

        var start = end - 1;
        while (start >= 0 && !IsDirectorySeparator(span[start])) start--;

        start++;
        return (start, end - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDirectorySeparator(char value) => 
        value == Path.DirectorySeparatorChar || value == Path.AltDirectorySeparatorChar;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CountDigits(int value)
    {
        var digits = 1;
        while (value >= 10)
        {
            value /= 10;
            digits++;
        }

        return digits;
    }

    private readonly struct ToStringState(
        string message,
        string? memberName,
        string? filePath,
        int fileNameStart,
        int fileNameLength,
        int lineNumber,
        bool hasMemberName,
        bool hasFileName,
        bool includeLineNumber)
    {
        public readonly string Message = message;
        public readonly string? MemberName = memberName;
        public readonly string? FilePath = filePath;
        public readonly int FileNameStart = fileNameStart;
        public readonly int FileNameLength = fileNameLength;
        public readonly int LineNumber = lineNumber;
        public readonly bool HasMemberName = hasMemberName;
        public readonly bool HasFileName = hasFileName;
        public readonly bool IncludeLineNumber = includeLineNumber;
    }

    /// <summary>
    /// Recursively gets all inner errors in the error chain as a flat list (excluding the current error itself).
    /// </summary>
    /// <remarks>
    /// This property traverses through the entire inner error chain 
    /// and collects all inner errors into a single flat list.
    /// Do not use this property to check for the presence of a specific error in the chain, 
    /// as it may be inefficient for long chains.
    /// If you need to run many queries over the same snapshot, 
    /// materializing a collection once via <see cref="InnerErrors"/> and reusing it can be reasonable.
    /// Use <see cref="Is"/> to check if a specific error exists in the chain.
    /// Use <see cref="As"/> to find and extract a specific error from the chain.
    /// </remarks>
    public IReadOnlyList<Error> InnerErrors => FlattenInnerErrors();

    /// <summary>
    /// Creates a flat list of all inner errors in the error chain, excluding the current error itself.
    /// </summary>
    /// <returns>A read-only list of all inner errors in the chain.</returns>
    private IReadOnlyList<Error> FlattenInnerErrors()
    {
        if (!this.IsError) return [];
        if (this.innerErrors is null) return [];

        List<Error> flatList = [];
        CollectErrorsRecursively(this.innerErrors, flatList);
        return flatList;
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

    private static void AppendStack(Error[] errors, System.Text.StringBuilder builder, ref bool hasLine)
    {
        foreach (var error in errors)
        {
            var text = error.ToString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (hasLine) builder.AppendLine();
                builder.Append(text);
                hasLine = true;
            }

            if (error.innerErrors is not null)
            {
                AppendStack(error.innerErrors, builder, ref hasLine);
            }
        }
    }

    /// <summary>
    /// Checks if this error or any error in its inner errors matches the target error.
    /// </summary>
    /// <remarks>
    /// This comparison only considers the error message, source location information
    /// (member name, file path, and line number) is ignored.
    /// </remarks>
    /// <param name="target">The target error to check for</param>
    /// <returns>True if this error or any wrapped error has the same message as the target; otherwise, false</returns>
    public bool Is(Error target)
    {
        if (!this.IsError && !target.IsError) return true;
        if (!this.IsError || !target.IsError) return false;
        if (this.state == ErrorState.Default && target.state == ErrorState.Default) return true;

        if (this.Message.Equals(target.Message, StringComparison.Ordinal))
            return true;

        foreach (var innerError in innerErrors ?? [])
            if (innerError.Is(target)) return true;

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

        if (!this.IsError) return false;
        if (!target.IsError) return false;
        if (this.state == ErrorState.Default && target.state == ErrorState.Default)
        {
            match = this;
            return true;
        }

        if (this.Message.Equals(target.Message, StringComparison.Ordinal))
        {
            match = this;
            return true;
        }

        foreach (var innerError in innerErrors ?? [])
        {
            if (innerError.As(target, out match)) return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the current error is equal to the specified error.
    /// </summary>
    /// <remarks>This method ignores source location and compares only the error state and message text. 
    /// Message comparison is case-sensitive and ordinal.
    /// Use this method when equality should not consider where the error originated.</remarks>
    /// <param name="other">The error to compare with the current error.</param>
    /// <returns>true if both errors have the same error state and, if present, identical message text; otherwise, false.</returns>
    public bool Equals(Error other)
    {
        if (!this.IsError && !other.IsError) return true;
        if (!this.IsError || !other.IsError) return false;

        return this.Message.Equals(other.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns a hash code for the current object based on its effective message.
    /// If the error represents no error (<see cref="Error.None"/>), returns 0.
    /// </summary>
    /// <returns>An integer hash code representing the object's state. 
    /// Returns 0 if this is <see cref="Error.None"/>; otherwise, returns the
    /// hash code of the effective message text exposed by <see cref="Message"/>.
    /// </returns>
    public override int GetHashCode()
    {
        if (!this.IsError) return 0;
        return StringComparer.Ordinal.GetHashCode(this.Message);
    }

    // Checks whether the given message has meaningful content
    // or returns the default error message if not.
    private static string NormalizeMessage(string? message)
    {
        // Null messages are replaced with the default message
        if (message is null)
            return Constants.DefaultErrorMessage;

        // Explicit empty message is kept as-is
        if (message.Length == 0) 
            return message;

        // If the first char is not whitespace, keep message as-is
        if (!char.IsWhiteSpace(message[0])) 
            return message;

        // Otherwise check if the whole message is whitespace
        return string.IsNullOrWhiteSpace(message) 
            ? Constants.DefaultErrorMessage 
            : message;
    }

    /// <summary>
    /// ErrorState indicates the internal runtime state of an <see cref="Error"/> value
    /// so we can differentiate Error.Empty, Error.None, default(error) and new Error():
    /// </summary>
    private enum ErrorState : byte
    {
        /// <summary>
        /// ErrorState.Default = 0 is the enum's zero value, a zeroed struct (default(Error)). 
        /// so default(Error) zeroes the whole struct and state will be <see langword="default"/> 
        /// An Error value in this state is still considered an error.
        /// </summary>
        Default = 0,

        /// <summary>
        /// The canonical non-error state (Error.None) will be in this state.
        /// </summary>
        None = 1,

        /// <summary>
        /// Any explicitly constructed error value will be in this state.
        /// </summary>
        Constructed = 2
    }

    private static class Constants
    {
        public const string DefaultErrorMessage = "Unknown error";
    }
}