using System.Collections.ObjectModel;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents;

public class FileSignature(
    byte[] signature,
    string mediaType,
    string extension,
    int headerLength = 0,
    int offset = 0)
    : IEquatable<FileSignature>
{
    public IReadOnlyList<byte> Signature { get; } = new ReadOnlyCollection<byte>(signature ??
        throw new ArgumentNullException(nameof(signature),
            "Signature cannot be null."));

    public int HeaderLength { get; } = headerLength is 0 ? signature.Length : headerLength;

    public string Extension { get; } = extension ??
                                       throw new ArgumentNullException(nameof(extension),
                                           "Extension cannot be null."); // Assuming extension should not be null or empty either

    public string MediaType { get; } = !string.IsNullOrEmpty(mediaType)
        ? mediaType
        : throw new ArgumentNullException(nameof(mediaType), "MediaType cannot be null or empty.");

    public int Offset { get; } = offset;

    public bool Equals(FileSignature? other) =>
        (other is not null &&
         ReferenceEquals(this, other)) ||
        (GetType() == other?.GetType() &&
         Signature.SequenceEqual(other.Signature) &&
         HeaderLength == other.HeaderLength &&
         Extension == other.Extension &&
         MediaType == other.MediaType &&
         Offset == other.Offset);

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

    public override bool Equals(object? obj) => Equals(obj as FileSignature);

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

    public override string ToString() => $"{MediaType} ({Extension})";
}
