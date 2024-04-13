using Microsoft.IO;

namespace DropBear.Codex.Files.Interfaces;

public interface IContentContainerStrategy
{
    bool CanHandle(Type contentType);

    IContentContainer CreateContentContainer(string name, object content, bool compress,
        RecyclableMemoryStreamManager streamManager);
}
