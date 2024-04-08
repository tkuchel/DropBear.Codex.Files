using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.ContentContainers;
using ServiceStack.Text;

namespace DropBear.Codex.Files.Factory;

/// <summary>
/// Factory for creating various types of content containers.
/// </summary>
public class ContentContainerFactory : IContentContainerFactory {
    private readonly RecyclableMemoryStreamManager? _streamManager;
        
    public ContentContainerFactory(RecyclableMemoryStreamManager? streamManager) {
        _streamManager = streamManager;
    }

    public IContentContainer CreateStringContentContainer(string name, string content, bool compress) {
        return new StringContentContainer(_streamManager, name, content, compress);
    }

    public IContentContainer CreateByteContentContainer(string name, byte[] content, bool compress) {
        // Assuming ByteContentContainer is similar to StringContentContainer and accepts a stream manager
        return new ByteContentContainer(_streamManager, name, content, compress);
    }

    public IContentContainer CreateStreamContentContainer(string name, Stream content, bool compress) {
        // Assuming StreamContentContainer is similar to StringContentContainer and accepts a stream manager
        return new StreamContentContainer(_streamManager, name, content, compress);
    }
}