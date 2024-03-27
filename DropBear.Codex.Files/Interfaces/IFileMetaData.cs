using DropBear.Codex.Files.Models.FileComponents.SubComponents;

namespace DropBear.Codex.Files.Interfaces;

public interface IFileMetaData
{
    public DateTimeOffset Created { get; }
    public DateTimeOffset LastModified { get; }
    public string Author { get; }
    public List<ContentTypeInfo> ContentTypes { get; }
}
