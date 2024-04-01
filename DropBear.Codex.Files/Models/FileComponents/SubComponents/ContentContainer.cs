using Blake3;
using K4os.Compression.LZ4.Streams;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents;

[MessagePackObject]
public class ContentContainer
{
    public ContentContainer(string name, byte[] content, ContentTypeInfo contentType, bool compress = false)
    {
        Name = name;
        ContentType = contentType;
        SetContent(content, compress);
    }

    // Constructor that accepts a Type parameter to auto-generate ContentTypeInfo
    public ContentContainer(Type type, string name, byte[] content, bool compress = false)
    {
        Name = name;
        SetContent(content, compress);
        ContentType = new ContentTypeInfo(type.Assembly.FullName ?? string.Empty, type.Name,
            type.Namespace ?? string.Empty);
    }

    [Key(0)] public string Name { get; set; }

    [Key(1)] public string Hash { get; set; } = string.Empty;

    [Key(2)] public byte[] Content { get; private set; } = Array.Empty<byte>();

    [Key(3)] public int Length => Content?.Length ?? 0;

    [Key(4)] public ContentTypeInfo ContentType { get; set; }

    [Key(5)] public bool IsCompressed { get; set; }

    // Method to update the hash of the content based on its current state
    public string UpdateContentHash()
    {
        // Compute the hash using the current content (consider compression state)
        var contentToHash = IsCompressed ? DecompressContent(Content) : Content;
        Hash = ComputeHash(contentToHash);

        // Return the updated hash
        return Hash;
    }

    // Sets or updates the content and compresses it if specified.
    public void SetContent(byte[] content, bool compress = false)
    {
        switch (compress)
        {
            case true when !IsCompressed:
                Content = CompressContent(content);
                IsCompressed = true;
                break;
            case false when IsCompressed:
                Content = DecompressContent(content);
                IsCompressed = false;
                break;
            default:
                Content = content;
                break;
        }

        Hash = ComputeHash(Content);
    }

    private static byte[] CompressContent(byte[] content)
    {
        using var inputStream = new MemoryStream(content);
        using var compressedStream = new MemoryStream();
        // Wrap the compressedStream with LZ4 compression stream
        using (var lz4Stream = LZ4Stream.Encode(compressedStream, leaveOpen: true))
        {
            inputStream.CopyTo(lz4Stream);
        }

        // It's important to return to the beginning of the stream if you're going to read from it next.
        compressedStream.Position = 0;
        return compressedStream.ToArray();
    }

    internal static byte[] DecompressContent(byte[] compressedContent)
    {
        using var compressedStream = new MemoryStream(compressedContent);
        using var decompressedStream = new MemoryStream();
        // Wrap the compressedStream with LZ4 decompression stream
        using (var lz4Stream = LZ4Stream.Decode(compressedStream, leaveOpen: true))
        {
            lz4Stream.CopyTo(decompressedStream);
        }

        // Again, ensure the stream's position is at the beginning if you intend to read from it right away.
        decompressedStream.Position = 0;
        return decompressedStream.ToArray();
    }

    // Verifies the content hash, recomputes if necessary
    public bool VerifyContentHash(bool recomputeHash = false)
    {
        if (!recomputeHash) return Hash == ComputeHash(IsCompressed ? DecompressContent(Content) : Content);
        var currentHash = ComputeHash(Content);
        return Hash == currentHash;
    }

    // Utilizes Blake3 for hash computation
    private static string ComputeHash(byte[] content) => Hasher.Hash(content).ToString();
}
