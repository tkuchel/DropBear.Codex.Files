using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;

namespace DropBear.Codex.Files.Services;

public class ContentContainerFactory : IContentContainerFactory
{
    public IContentContainer Create(byte[] data, ContentTypeInfo contentType)
    {
        // Assuming ContentContainer is your IContentContainer implementation
        return new ContentContainer(data, contentType);
    }
}
