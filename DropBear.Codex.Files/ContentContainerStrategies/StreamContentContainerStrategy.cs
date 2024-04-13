using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.ContentContainers;
using Microsoft.IO;

namespace DropBear.Codex.Files.ContentContainerStrategies;

public class StreamContentContainerStrategy : IContentContainerStrategy
{
    public bool CanHandle(Type contentType) => contentType == typeof(Stream);

    public IContentContainer CreateContentContainer(string name, object content, bool compress,
        RecyclableMemoryStreamManager streamManager)
    {
        if (content is not Stream contentStream)
            throw new ArgumentException("Content must be of type Stream.", nameof(content));

        return new StreamContentContainer(streamManager, name, contentStream, compress);
    }
}
