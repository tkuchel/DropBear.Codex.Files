namespace DropBear.Codex.Files.Exceptions;

public class ContentContainerStrategyNotFound : Exception
{
    public ContentContainerStrategyNotFound(string message) : base(message)
    {
    }

    public ContentContainerStrategyNotFound()
    {
    }

    public ContentContainerStrategyNotFound(string message, Exception innerException) : base(message, innerException)
    {
    }
}
