using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.Bases;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;

namespace DropBear.Codex.Files.Models.FileComponents;

public class FileMetaData : FileComponentBase, IFileMetaData
{
    private readonly DateTimeOffset _createdDate;
    private readonly DateTimeOffset _lastModifiedDate;
    private readonly string _author;
    private readonly List<ContentTypeInfo> _contentTypes;

    public FileMetaData(DateTimeOffset createdDate, DateTimeOffset lastModifiedDate, string author, List<ContentTypeInfo> contentTypes)
    {
        _createdDate = createdDate;
        _lastModifiedDate = lastModifiedDate;
        _author = author;
        _contentTypes = contentTypes;
    }

    public DateTimeOffset Created => _createdDate;
    public DateTimeOffset LastModified => _lastModifiedDate;
    public string Author => _author;
    public List<ContentTypeInfo> ContentTypes => _contentTypes;
}
