namespace DropBear.Codex.Files.Models;

public class FileVersion
{
    public FileVersion(string versionLabel, DateTimeOffset versionDate)
    {
        VersionLabel = versionLabel ?? throw new ArgumentNullException(nameof(versionLabel));
        VersionDate = versionDate;
    }

    public DateTimeOffset VersionDate { get; set; }
    public string VersionLabel { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is FileVersion other)
            return string.Equals(VersionLabel, other.VersionLabel, StringComparison.OrdinalIgnoreCase) &&
                   VersionDate.Equals(other.VersionDate);
        return false;
    }

    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            var hash = 17;
            hash = hash * 23 + (VersionLabel != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(VersionLabel) : 0);
            hash = hash * 23 + VersionDate.GetHashCode();
            return hash;
        }
    }
}
