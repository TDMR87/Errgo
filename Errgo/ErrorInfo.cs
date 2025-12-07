using System.Runtime.CompilerServices;

namespace Errgo;

/// <summary>
/// Represents error information with source location details.
/// </summary>
internal readonly record struct ErrorInfo
{
    /// <summary>
    /// The error message text.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// The member (method/property) where the error occurred.
    /// </summary>
    public string MemberName { get; init; }

    /// <summary>
    /// The source file where the error occurred.
    /// </summary>
    public string FilePath { get; init; }

    /// <summary>
    /// The line number where the error occurred.
    /// </summary>
    public int LineNumber { get; init; }

    public ErrorInfo(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Message = message;
        MemberName = memberName;
        FilePath = filePath;
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Returns the full error information including source location.
    /// </summary>
    public override string ToString()
    {
        if (string.IsNullOrWhiteSpace(Message))
            return string.Empty;

        if (string.IsNullOrWhiteSpace(MemberName))
            return Message;

        var fileName = !string.IsNullOrWhiteSpace(FilePath)
            ? Path.GetFileName(FilePath)
            : string.Empty;

        if (!string.IsNullOrWhiteSpace(fileName) && LineNumber > 0)
            return $"{Message} at {MemberName} in {fileName} (line {LineNumber})";

        if (!string.IsNullOrWhiteSpace(fileName))
            return $"{Message} at {MemberName} in {fileName}";

        return $"{Message} at {MemberName}";
    }

    /// <summary>
    /// Implicit conversion from string to ErrorInfo.
    /// </summary>
    public static implicit operator ErrorInfo(string message) => new(message);

    /// <summary>
    /// Implicit conversion from ErrorInfo to string (returns just the message text).
    /// </summary>
    public static implicit operator string(ErrorInfo errorInfo) => errorInfo.Message ?? string.Empty;
}
