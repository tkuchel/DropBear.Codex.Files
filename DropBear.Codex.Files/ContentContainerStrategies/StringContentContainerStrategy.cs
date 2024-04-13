using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.ContentContainers;
using Microsoft.IO;

namespace DropBear.Codex.Files.ContentContainerStrategies;

public class StringContentContainerStrategy : IContentContainerStrategy
{
    public bool CanHandle(Type contentType) => contentType == typeof(string);

    public IContentContainer CreateContentContainer(string name, object content, bool compress,
        RecyclableMemoryStreamManager streamManager)
        => new StringContentContainer(streamManager, name, content as string, compress);
}
