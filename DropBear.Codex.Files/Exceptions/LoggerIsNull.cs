namespace DropBear.Codex.Files.Exceptions;

public class LoggerIsNull : Exception
{
    public LoggerIsNull(string message) : base(message)
    {
    }

    public LoggerIsNull()
    {
    }

    public LoggerIsNull(string message, Exception innerException) : base(message, innerException)
    {
    }
}
