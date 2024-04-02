using FileSignatures;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents
{
    /// <summary>
    /// Represents the signature of a DropBear file format.
    /// </summary>
    [MessagePackObject(true)]
    public class FileSignature : FileFormat, IComparable<FileFormat>, IEquatable<FileFormat>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileSignature"/> class with default values.
        /// </summary>
        public FileSignature() : base("dbf202441"u8.ToArray(), "application/dropbear-file", ".dbf")
        {
        }

        /// <summary>
        /// Gets the signature bytes.
        /// </summary>
#pragma warning disable CA1819
        public new byte[] Signature { get; } = "dbf202441"u8.ToArray();
#pragma warning restore CA1819

        /// <summary>
        /// Gets the media type.
        /// </summary>
        public new string MediaType { get; } = "application/dropbear-file";

        /// <summary>
        /// Gets the file extension.
        /// </summary>
        public new string Extension { get; } = "dbf";

        // Implement IComparable
        /// <inheritdoc/>
        public int CompareTo(FileFormat? other)
        {
            if (other is null) return 1;

            var mediaTypeComparison = string.Compare(MediaType, other.MediaType, StringComparison.Ordinal);
            return mediaTypeComparison is not 0
                ? mediaTypeComparison
                : string.Compare(Extension, other.Extension, StringComparison.Ordinal);
        }

        // Implement IEquatable
        /// <inheritdoc/>
        public new bool Equals(FileFormat? other) =>
            other is not null &&
            Signature.SequenceEqual(other.Signature) &&
            MediaType == other.MediaType &&
            Extension == other.Extension;

        // Define operators
        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as FileFormat);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(MediaType, Extension, Signature);

        // Define comparison operators
        /// <inheritdoc/>
        public static bool operator ==(FileSignature left, FileFormat right) =>
            EqualityComparer<FileFormat>.Default.Equals(left, right);

        /// <inheritdoc/>
        public static bool operator !=(FileSignature left, FileFormat right) => !(left == right);

        /// <inheritdoc/>
        public static bool operator <(FileSignature? left, FileFormat? right) =>
            left is null ? right is not null : left.CompareTo(right) < 0;

        /// <inheritdoc/>
        public static bool operator <=(FileSignature? left, FileFormat? right) =>
            left is null || left.CompareTo(right) <= 0;

        /// <inheritdoc/>
        public static bool operator >(FileSignature? left, FileFormat? right) =>
            left is not null && left.CompareTo(right) > 0;

        /// <inheritdoc/>
        public static bool operator >=(FileSignature? left, FileFormat? right) =>
            left is not null && left.CompareTo(right) >= 0;
    }
}
