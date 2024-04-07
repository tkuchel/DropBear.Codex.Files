using System.Security.Cryptography;
using System.Text;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using FastRsync.Compression;
using ServiceStack.Text;

namespace DropBear.Codex.Files.Models.ContentContainers;

public class StringContentContainer : IContentContainer
{
    private readonly RecyclableMemoryStreamManager _streamManager;
    private string? _lazyHash;

    public StringContentContainer(RecyclableMemoryStreamManager? streamManager, string name, string? content,
        bool compress)
    {
        _streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IsCompressed = compress;
        var contentBytes = string.IsNullOrEmpty(content) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(content);
        Content = CompressIfNeeded(contentBytes, compress);
        Length = Content.Length;
        ContentType = new ContentTypeInfo(typeof(byte[]));
    }

    public string Name { get; }
    public string Hash => _lazyHash ??= GenerateContentHash();

    public byte[] Content { get; }
    public int Length { get; }
    public ContentTypeInfo ContentType { get; }
    public bool IsCompressed { get; }

    public bool VerifyContentHash(bool recomputeHash = false)
    {
        if (!recomputeHash) return true;

        var newHash = GenerateContentHash();
        return Hash == newHash;
    }

    private string GenerateContentHash()
    {
        try
        {
            using var stream = _streamManager.GetStream();
            stream.Write(Content, 0, Content.Length);
            stream.Position = 0;
            using var sha256Hasher = SHA256.Create();
            var hash = sha256Hasher.ComputeHash(stream);
            return Convert.ToBase64String(hash);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to generate content hash.", ex);
        }
    }

    private byte[] CompressIfNeeded(byte[] content, bool compress)
    {
        if (!compress) return content;

        try
        {
            using var sourceStream = new MemoryStream(content);
            using var compressedStream = _streamManager.GetStream();
            GZip.Compress(sourceStream, compressedStream);
            compressedStream.Position = 0;
            return ReadStream(compressedStream);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to compress content.", ex);
        }
    }

    private byte[] ReadStream(Stream input)
    {
        using var memoryStream = _streamManager.GetStream();
        input.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}
