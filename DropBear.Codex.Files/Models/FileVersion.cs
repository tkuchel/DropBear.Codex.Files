using DropBear.Codex.Files.Interfaces;

namespace DropBear.Codex.Files.Models;

public class FileVersion : IFileVersion
{
    public string VersionLabel { get; private set; }
    public DateTime VersionDate { get; private set; }

    public FileVersion(string versionLabel, DateTime versionDate)
    {
        VersionLabel = versionLabel;
        VersionDate = versionDate;
    }

    public void Compare(IFileVersion otherVersion)
    {
        // Implement comparison logic, possibly comparing VersionDate or VersionLabel
    }

    public void Rollback()
    {
        // Logic to revert to this file version
    }

    public void Update(IFileVersion newVersion)
    {
        // Update logic to apply changes from newVersion
    }
}
