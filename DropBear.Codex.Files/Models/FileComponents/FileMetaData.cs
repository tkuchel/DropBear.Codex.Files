using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.Bases;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;

namespace DropBear.Codex.Files.Models.FileComponents;

public class FileMetaData(string author, IList<ContentTypeInfo> contentTypes) : FileComponentBase, IFileMetaData
{
    public DateTimeOffset Created { get; } = DateTimeOffset.Now;

    public DateTimeOffset LastModified { get; private set; } = DateTimeOffset.Now;

    public string Author { get; } = author;

    public IReadOnlyCollection<ContentTypeInfo> ExpectedContentTypes => contentTypes.AsReadOnly();

    public void UpdateLastModified() => LastModified = DateTimeOffset.Now;
}
