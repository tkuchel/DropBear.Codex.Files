using System.Collections.ObjectModel;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents;

/// <summary>
/// Represents a file signature.
/// </summary>
[MessagePackObject]
public class FileSignature : IEquatable<FileSignature>
{
    // This constructor is necessary for MessagePack deserialization.
    // Ensure it's empty or initializes properties with default values.
    [SerializationConstructor]
    public FileSignature() { }

    public FileSignature(byte[] signature, string mediaType, string extension, int headerLength = 0, int offset = 0)
    {
        Signature = new ReadOnlyCollection<byte>(signature ?? throw new ArgumentNullException(nameof(signature), "Signature cannot be null."));
        HeaderLength = headerLength == 0 ? Signature.Count : headerLength;
        Extension = extension ?? throw new ArgumentNullException(nameof(extension), "Extension cannot be null.");
        MediaType = !string.IsNullOrEmpty(mediaType) ? mediaType : throw new ArgumentNullException(nameof(mediaType), "MediaType cannot be null or empty.");
        Offset = offset;
    }

    [Key(0)]
    public ReadOnlyCollection<byte> Signature { get; private set; }

    [Key(1)]
    public int HeaderLength { get; private set; }

    [Key(2)]
    public string Extension { get; private set; }

    [Key(3)]
    public string MediaType { get; private set; }

    [Key(4)]
    public int Offset { get; private set; }

    /// <summary>
    ///     Determines whether the specified object is equal to the current file signature.
    /// </summary>
    /// <param name="other">The object to compare with the current file signature.</param>
    /// <returns><c>true</c> if the specified object is equal to the current file signature; otherwise, <c>false</c>.</returns>
    public bool Equals(FileSignature? other) => (other is not null &&
                                                 ReferenceEquals(this, other)) ||
                                                (GetType() == other?.GetType() &&
                                                 Signature.SequenceEqual(other.Signature) &&
                                                 HeaderLength == other.HeaderLength &&
                                                 Extension == other.Extension &&
                                                 MediaType == other.MediaType &&
                                                 Offset == other.Offset);

    /// <summary>
    ///     Determines whether the specified stream matches the file signature.
    /// </summary>
    /// <param name="stream">The stream to check for a match.</param>
    /// <returns><c>true</c> if the stream matches the file signature; otherwise, <c>false</c>.</returns>
    public bool IsMatch(Stream stream)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream), "Stream cannot be null.");
        if (stream.Length < HeaderLength || Offset > stream.Length - HeaderLength) return false;

        var originalPosition = stream.Position;
        try
        {
            stream.Position = Offset;
            var buffer = new byte[Signature.Count];
            return stream.Read(buffer, 0, buffer.Length) == buffer.Length && buffer.SequenceEqual(Signature);
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    /// <summary>
    ///     Determines whether the specified object is equal to the current file signature.
    /// </summary>
    /// <param name="obj">The object to compare with the current file signature.</param>
    /// <returns><c>true</c> if the specified object is equal to the current file signature; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) => Equals(obj as FileSignature);

    /// <summary>
    ///     Returns the hash code for the current file signature.
    /// </summary>
    /// <returns>A hash code for the current file signature.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + HeaderLength.GetHashCode();
            hash = hash * 31 + Offset.GetHashCode();
            hash = hash * 31 + Extension.GetHashCode(StringComparison.OrdinalIgnoreCase);
            hash = hash * 31 + MediaType.GetHashCode(StringComparison.OrdinalIgnoreCase);
            return Signature.Aggregate(hash, (current, element) => current * 31 + element.GetHashCode());
        }
    }

    /// <summary>
    ///     Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => $"{MediaType} ({Extension})";
}
