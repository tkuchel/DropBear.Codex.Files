using System.Security.Cryptography;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using FastRsync.Compression;
using ServiceStack.Text;

namespace DropBear.Codex.Files.Models.ContentContainers;

/// <summary>
///     Represents a content container for byte arrays, with optional compression.
/// </summary>
public class ByteContentContainer : IContentContainer
{
    private readonly RecyclableMemoryStreamManager _streamManager;
    private string? _lazyHash;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ByteContentContainer" /> class.
    /// </summary>
    /// <param name="streamManager">The stream manager for memory efficiency.</param>
    /// <param name="name">The name of the content.</param>
    /// <param name="content">The byte array content.</param>
    /// <param name="compress">Indicates whether the content should be compressed.</param>
    /// <exception cref="ArgumentNullException">Thrown if streamManager is null.</exception>
    public ByteContentContainer(RecyclableMemoryStreamManager? streamManager, string name, byte[]? content,
        bool compress)
    {
        _streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IsCompressed = compress;
        _content = CompressIfNeeded(content ?? Array.Empty<byte>(), compress);
        Length = _content.Length;
        ContentType = new ContentTypeInfo(typeof(byte[]));
    }

    public string Name { get; }
    public string Hash => _lazyHash ??= GenerateContentHash();

    private byte[] _content { get; }
    public byte[] Content() => _content;
    public int Length { get; }
    public ContentTypeInfo ContentType { get; }
    public bool IsCompressed { get; }

    /// <summary>
    ///     Verifies the hash of the content, recomputing it if requested.
    /// </summary>
    /// <param name="recomputeHash">Indicates whether the hash should be recomputed.</param>
    /// <returns>true if the hash is valid, false otherwise.</returns>
    public bool VerifyContentHash(bool recomputeHash = false)
    {
        if (!recomputeHash) return true;

        var newHash = GenerateContentHash();
        return Hash == newHash;
    }

    /// <summary>
    ///     Generates a SHA256 hash of the content.
    /// </summary>
    /// <returns>The base64 encoded hash string.</returns>
    private string GenerateContentHash()
    {
        try
        {
            using var stream = _streamManager.GetStream();
            stream.Write(_content, 0, _content.Length);
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

    /// <summary>
    ///     Compresses the content if required.
    /// </summary>
    /// <param name="content">The original content.</param>
    /// <param name="compress">Whether compression is needed.</param>
    /// <returns>The possibly compressed content.</returns>
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

    /// <summary>
    ///     Reads all bytes from the stream into a byte array.
    /// </summary>
    /// <param name="input">The input stream.</param>
    /// <returns>A byte array of the stream's content.</returns>
    private byte[] ReadStream(Stream input)
    {
        using var memoryStream = _streamManager.GetStream();
        input.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}
