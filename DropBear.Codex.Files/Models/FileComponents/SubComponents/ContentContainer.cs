using DropBear.Codex.Files.Interfaces;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents;

public class ContentContainer(byte[] data, ContentTypeInfo contentType) : IContentContainer
{
    public byte[] Data { get; set; } = data;
    public ContentTypeInfo ContentType { get; set; } = contentType;
}
