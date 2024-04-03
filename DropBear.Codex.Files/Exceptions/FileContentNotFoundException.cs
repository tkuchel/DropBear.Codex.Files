namespace DropBear.Codex.Files.Exceptions;

/// <summary>
///     Exception thrown when the content of a file is not found.
/// </summary>
public class FileContentNotFoundException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FileContentNotFoundException" /> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public FileContentNotFoundException(string message) : base(message)
    {
        // No additional logic needed for this constructor.
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="FileContentNotFoundException" /> class.
    /// </summary>
    public FileContentNotFoundException()
    {
        // No additional logic needed for this constructor.
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="FileContentNotFoundException" /> class with a specified error message
    ///     and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">
    ///     The exception that is the cause of the current exception, or a null reference
    ///     if no inner exception is specified.
    /// </param>
    public FileContentNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
        // No additional logic needed for this constructor.
    }
}
