namespace DropBear.Codex.Files.Interfaces;

public interface IContentContainerFactory
{
    IContentContainer CreateStringContentContainer(string name, string content, bool compress);
    IContentContainer CreateByteContentContainer(string name, byte[] content, bool compress);

    IContentContainer CreateStreamContentContainer(string name, Stream content, bool compress);
    // Add methods for other container types as needed
}
