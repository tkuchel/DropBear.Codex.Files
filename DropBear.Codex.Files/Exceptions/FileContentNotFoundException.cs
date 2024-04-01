namespace DropBear.Codex.Files.Exceptions;

public class FileContentNotFoundException : Exception
{
    public FileContentNotFoundException(string message) : base(message)
    {
    }

    public FileContentNotFoundException()
    {
    }

    public FileContentNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
