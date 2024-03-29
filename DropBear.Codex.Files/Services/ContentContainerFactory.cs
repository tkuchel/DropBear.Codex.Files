using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;

namespace DropBear.Codex.Files.Services;

public class ContentContainerFactory : IContentContainerFactory
{
    public IContentContainer Create(byte[] data, Type contentType)
    {
        var contentTypeInfo = new ContentTypeInfo
        {
            AssemblyName = contentType.Assembly.GetName().Name ?? string.Empty,
            TypeName = contentType.Name,
            Namespace = contentType.Namespace ?? string.Empty,
        };

        return new ContentContainer(data, contentTypeInfo);
    }
}
