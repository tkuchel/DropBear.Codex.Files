namespace DropBear.Codex.Files.Exceptions;

public class ContentTypeNotFoundException : Exception
{
    public ContentTypeNotFoundException(string message) : base(message)
    {
    }

    public ContentTypeNotFoundException()
    {
    }

    public ContentTypeNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
