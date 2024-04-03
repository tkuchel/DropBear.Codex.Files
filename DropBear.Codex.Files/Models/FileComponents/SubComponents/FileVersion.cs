using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents;

[MessagePackObject]
public class FileVersion : IEquatable<FileVersion>, IComparable<FileVersion>
{
    public FileVersion()
        : this(DateTime.Today.Year, DateTime.Today.Month, 0)
    {
    }

    public FileVersion(int buildNumber)
        : this(DateTime.Today.Year, DateTime.Today.Month, buildNumber)
    {
    }

    [SerializationConstructor]
    public FileVersion(int major, int minor, int build)
    {
        if (major < 0) throw new ArgumentOutOfRangeException(nameof(major), "Major version number cannot be negative.");

        if (minor < 0) throw new ArgumentOutOfRangeException(nameof(minor), "Minor version number cannot be negative.");

        if (build < 0) throw new ArgumentOutOfRangeException(nameof(build), "Build number cannot be negative.");

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

        var comparison = Major.CompareTo(other.Major);
        if (comparison is not 0) return comparison;

        comparison = Minor.CompareTo(other.Minor);
        return comparison is not 0 ? comparison : Build.CompareTo(other.Build);
    }

    public bool Equals(FileVersion? other) =>
        other != null && Major == other.Major && Minor == other.Minor && Build == other.Build;

    public override bool Equals(object? obj) => Equals(obj as FileVersion);

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Build);

    public static bool operator ==(FileVersion? left, FileVersion? right) => Equals(left, right);

    public static bool operator !=(FileVersion? left, FileVersion? right) => !Equals(left, right);

    public static bool operator <(FileVersion? left, FileVersion? right) =>
        left is null ? right != null : left.CompareTo(right) < 0;

    public static bool operator <=(FileVersion? left, FileVersion? right) => left is null || left.CompareTo(right) <= 0;

    public static bool operator >(FileVersion? left, FileVersion? right) => left != null && left.CompareTo(right) > 0;

    public static bool operator >=(FileVersion? left, FileVersion? right) => left != null && left.CompareTo(right) >= 0;
}
