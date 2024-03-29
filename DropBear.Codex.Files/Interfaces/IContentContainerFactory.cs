using DropBear.Codex.Files.Models.FileComponents.SubComponents;

namespace DropBear.Codex.Files.Interfaces;

public interface IContentContainerFactory
{
    IContentContainer Create(byte[] data, ContentTypeInfo contentType);
}
