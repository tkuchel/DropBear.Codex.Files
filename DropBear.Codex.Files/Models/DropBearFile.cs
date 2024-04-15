using DropBear.Codex.Files.Builders;
using DropBear.Codex.Files.Interfaces;

namespace DropBear.Codex.Files.Models;

public class DropBearFile
{
    public DropBearFile()
    {
        Metadata = new Dictionary<string, string>();
        Versions = new List<IFileVersion>();
        ContentContainers = new List<IContentContainer>();
    }

    public Dictionary<string, string> Metadata { get; }
    public List<IFileVersion> Versions { get; }
    public List<IContentContainer> ContentContainers { get; }

    // Factory method to start the building process
    public static DropBearFileBuilder CreateBuilder() => new(new DropBearFile());
}


