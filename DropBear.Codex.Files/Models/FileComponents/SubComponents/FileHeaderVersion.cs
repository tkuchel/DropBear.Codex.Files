using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents;
[MessagePackObject]
public class FileHeaderVersion
{
    [Key(0)]
    public int Major { get; }
    [Key(1)]
    public int Minor { get; }
    [Key(2)]
    public int Build { get; }

    [SerializationConstructor]
    public FileHeaderVersion(int major, int minor, int build)
    {
        Major = major;
        Minor = minor;
        Build = build;
    }
}
