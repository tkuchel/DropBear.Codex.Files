using FileSignatures;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents;

[MessagePackObject]
public class FileSignature : FileFormat, IComparable<FileFormat>, IEquatable<FileFormat>
{
    public FileSignature() : base("dbf202441"u8.ToArray(), "application/dropbear-file", ".dbf")
    {
    }

    [Key(0)] public new byte[] Signature { get; } = "dbf202441"u8.ToArray();

    [Key(1)] public new string MediaType { get; } = "application/dropbear-file";

    [Key(2)] public new string Extension { get; } = ".dbf";

    // Implement IComparable
    public int CompareTo(FileFormat? other)
    {
        if (other is null) return 1;

        var mediaTypeComparison = string.Compare(MediaType, other.MediaType, StringComparison.Ordinal);
        return mediaTypeComparison is not 0
            ? mediaTypeComparison
            : string.Compare(Extension, other.Extension, StringComparison.Ordinal);
    }

    public new bool Equals(FileFormat? other) =>
        other is not null &&
        Signature.SequenceEqual(other.Signature) &&
        MediaType == other.MediaType &&
        Extension == other.Extension;

    // Implement IEquatable
    public override bool Equals(object obj) => Equals(obj as FileFormat);

    public override int GetHashCode() => HashCode.Combine(MediaType, Extension, Signature);

    // Define operators
    public static bool operator ==(FileSignature left, FileFormat right) =>
        EqualityComparer<FileFormat>.Default.Equals(left, right);

    public static bool operator !=(FileSignature left, FileFormat right) => !(left == right);

    public static bool operator <(FileSignature? left, FileFormat? right) =>
        left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator <=(FileSignature? left, FileFormat? right) =>
        left is null || left.CompareTo(right) <= 0;

    public static bool operator >(FileSignature? left, FileFormat? right) =>
        left is not null && left.CompareTo(right) > 0;

    public static bool operator >=(FileSignature? left, FileFormat? right) =>
        left is not null && left.CompareTo(right) >= 0;
}
