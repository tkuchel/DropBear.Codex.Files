using DropBear.Codex.Files.Models.FileComponents.SubComponents;

namespace DropBear.Codex.Files.Interfaces;

public interface IContentContainer
{
    byte[] Data { get; }
    ContentTypeInfo ContentType { get;  }
}
