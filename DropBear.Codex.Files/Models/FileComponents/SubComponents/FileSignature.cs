using DropBear.Codex.Utilities.Helpers;
using FileSignatures;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents;

[MessagePackObject(true)]
public class FileSignature : FileFormat, IComparable<FileFormat>, IEquatable<FileFormat>
{
    public FileSignature()
        : base("dbf202441".GetBytes(), "application/dropbear-file", ".dbf")
    {
    }

    // The Signature, MediaType, and Extension properties are already defined in the base class FileFormat,
    // so they are removed from here to prevent hiding them unintentionally without adding new behavior.

    // Implement IComparable
    public int CompareTo(FileFormat? other)
    {
        if (other is null) return 1;

        // Comparison logic can be simplified or remain as specific as necessary for the use case.
        return string.CompareOrdinal(MediaType, other.MediaType) is not 0
            ? string.CompareOrdinal(MediaType, other.MediaType)
            : string.CompareOrdinal(Extension, other.Extension);
    }

    // Implement IEquatable
    public new bool Equals(FileFormat? other) =>
        // Use base.Equals for checking base properties and add specific logic if needed.
        base.Equals(other);

    public override bool Equals(object obj) =>
        // Utilize pattern matching for a concise type check and call the type-specific Equals.
        obj is FileFormat other && Equals(other);

    public override int GetHashCode() =>
        // Simplified to use base.GetHashCode if it meets the need or customize as needed.
        HashCode.Combine(MediaType, Extension, Signature);

    // Equality and comparison operators rely on the IEquatable<FileFormat> and IComparable<FileFormat> implementations.
    public static bool operator ==(FileSignature? left, FileFormat? right) => Equals(left, right);

    public static bool operator !=(FileSignature? left, FileFormat? right) => !Equals(left, right);

    public static bool operator <(FileSignature? left, FileFormat? right) =>
        left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator <=(FileSignature? left, FileFormat? right) =>
        left is null || left.CompareTo(right) <= 0;

    public static bool operator >(FileSignature? left, FileFormat? right) =>
        left is not null && left.CompareTo(right) > 0;

    public static bool operator >=(FileSignature? left, FileFormat? right) =>
        left is not null && left.CompareTo(right) >= 0;
}
