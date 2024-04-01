using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents;

[MessagePackObject]
public class FileVersion : IEquatable<FileVersion>, IComparable<FileVersion>
{
    public FileVersion()
    {
        Major = DateTime.Today.Year;
        Minor = DateTime.Today.Month;
        Build = 0;
    }

    public FileVersion(int buildNumber)
    {
        Major = DateTime.Today.Year;
        Minor = DateTime.Today.Month;
        Build = buildNumber;
    }


    public FileVersion(int major, int minor, int build)
    {
        Major = major;
        Minor = minor;
        Build = build;
    }

    [Key(0)] public int Major { get; init; }

    [Key(1)] public int Minor { get; init; }

    [Key(2)] public int Build { get; init; }

    public int CompareTo(FileVersion? other)
    {
        if (other == null) return 1;
        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison is not 0) return majorComparison;
        var minorComparison = Minor.CompareTo(other.Minor);
        return minorComparison is not 0 ? minorComparison : Build.CompareTo(other.Build);
    }

    public bool Equals(FileVersion? other) =>
        other != null &&
        Major == other.Major &&
        Minor == other.Minor &&
        Build == other.Build;

    public override bool Equals(object? obj) => Equals(obj as FileVersion);

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Build);

    public static bool operator ==(FileVersion? left, FileVersion? right) =>
        EqualityComparer<FileVersion>.Default.Equals(left, right);

    public static bool operator !=(FileVersion? left, FileVersion? right) => !(left == right);

    public static bool operator <(FileVersion? left, FileVersion? right) =>
        left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator <=(FileVersion? left, FileVersion? right) => left is null || left.CompareTo(right) <= 0;

    public static bool operator >(FileVersion? left, FileVersion? right) =>
        left is not null && left.CompareTo(right) > 0;

    public static bool operator >=(FileVersion? left, FileVersion? right) =>
        left is not null && left.CompareTo(right) >= 0;
}
