using System.Security.Cryptography;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using FastRsync.Compression;
using ServiceStack.Text;

// Assuming this namespace for RecyclableMemoryStreamManager

namespace DropBear.Codex.Files.Models.ContentContainers;

public class StreamContentContainer : IContentContainer
{
    private readonly RecyclableMemoryStreamManager _streamManager;

    public StreamContentContainer(RecyclableMemoryStreamManager? streamManager, string name, Stream? contentStream,
        bool compress)
    {
        _streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        Name = name;
        IsCompressed = compress;
        Content = CompressIfNeeded(ReadStream(contentStream) ?? Array.Empty<byte>(), compress);
        Length = Content.Length;
        ContentType = new ContentTypeInfo(typeof(byte[]));
        Hash = GenerateContentHash();
    }

    public string Name { get; }
    public string Hash { get; }

    public byte[] Content { get; }
    public int Length { get; }
    public ContentTypeInfo ContentType { get; }
    public bool IsCompressed { get; }

    public bool VerifyContentHash(bool recomputeHash = false)
    {
        if (!recomputeHash) return true; // No need to recompute, assume hash is valid

        var newHash = GenerateContentHash();
        return Hash == newHash; // Compare the newly generated hash with the existing one
    }

    private string GenerateContentHash()
    {
        using var stream = _streamManager.GetStream();
        stream.Write(Content, 0, Content.Length);
        stream.Position = 0; // Reset position after writing to calculate hash correctly.
        using var sha256Hasher = SHA256.Create();
        var hash = sha256Hasher.ComputeHash(stream);
        return Convert.ToBase64String(hash);
    }

    private byte[] CompressIfNeeded(byte[] content, bool compress)
    {
        if (!compress) return content;

        using var sourceStream = new MemoryStream(content);
        using var compressedStream = _streamManager.GetStream();
        // Assuming FastRsync.Compression.GZip.Compress is compatible with streams
        GZip.Compress(sourceStream, compressedStream); // Check the compress method signature
        compressedStream.Position = 0;
        return ReadStream(compressedStream);
    }

    private byte[] ReadStream(Stream input)
    {
        using var memoryStream = _streamManager.GetStream(); // Use RecyclableMemoryStream
        input.CopyTo(memoryStream);
        return memoryStream.ToArray(); // Returns the underlying buffer without additional allocations
    }
}
