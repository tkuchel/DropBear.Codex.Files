using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.ContentContainers;
using ServiceStack.Text;

namespace DropBear.Codex.Files.ContentContainerStrategies;

public class ByteContentContainerStrategy : IContentContainerStrategy
{
    public bool CanHandle(Type contentType) => contentType == typeof(byte[]);

    public IContentContainer CreateContentContainer(string name, object content, bool compress, RecyclableMemoryStreamManager streamManager)
        => new ByteContentContainer(streamManager, name, content as byte[], compress);
}
