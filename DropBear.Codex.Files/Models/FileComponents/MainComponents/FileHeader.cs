using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.MainComponents;

[MessagePackObject]
public class FileHeader
{
    // Default constructor for MessagePack deserialization
    public FileHeader() { }

    // Parameterized constructor for easy instantiation
    public FileHeader(FileVersion version, FileSignature signature)
    {
        Version = version;
        Signature = signature;
    }

    [Key(0)] public FileVersion Version { get; set; }

    [Key(1)] public FileSignature Signature { get; set; }

    // Method to update the file version
    public void UpdateVersion(int major, int minor, int build) =>
        Version = new FileVersion { Major = major, Minor = minor, Build = build };
    
}
