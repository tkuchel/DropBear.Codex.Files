using Blake3;
using DropBear.Codex.Files.Interfaces;
using K4os.Compression.LZ4.Streams;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents;

[MessagePackObject]
public class ContentContainer : IContentContainer
{
    [Obsolete("For MessagePack", false)]
    public ContentContainer()
    {
        Name = string.Empty;
        ContentType = new ContentTypeInfo();
        Content = Array.Empty<byte>();
    }


    public ContentContainer(string name, byte[] content, ContentTypeInfo contentType, bool compress = false)
    {
        Name = name;
        ContentType = contentType;
        Content = Array.Empty<byte>();
        SetContent(content, compress);
        
        
    }

    public ContentContainer(Type type, string name, byte[] content, bool compress = false)
    {
        Name = name;
        ContentType = new ContentTypeInfo(type.Assembly.FullName ?? string.Empty, type.Name,
            type.Namespace ?? string.Empty);
        Content = Array.Empty<byte>();
        SetContent(content, compress);
    }



    [Key(0)] public string Name { get; set; }

    [Key(1)] public string Hash { get; private set; } = string.Empty;
#pragma warning disable CA1819
    [Key(2)] public byte[] Content { get; set; }
#pragma warning restore CA1819


    [Key(3)] public int Length => Content.Length;

    [Key(4)] public ContentTypeInfo ContentType { get; set; }

    [Key(5)] public bool IsCompressed { get; private set; }

    public bool VerifyContentHash(bool recomputeHash = false)
    {
        var contentToVerify = IsCompressed ? DecompressContent(Content) : Content;
        var currentHash = recomputeHash ? ComputeHash(contentToVerify) : Hash;
        return currentHash == ComputeHash(contentToVerify);
    }

    public void SetContent(byte[] content, bool compress)
    {
        Content = compress ? CompressContent(content) : content;
        IsCompressed = compress;
        Hash = ComputeHash(Content);
    }

    public void UpdateContent(byte[] content, bool compress) =>
        // A direct call to SetContent ensures compression and hash updates
        SetContent(content, compress);

    internal static byte[] CompressContent(byte[] content)
    {
        using var inputStream = new MemoryStream(content);
        using var compressedStream = new MemoryStream();
        using (var lz4Stream = LZ4Stream.Encode(compressedStream, leaveOpen: true))
        {
            inputStream.CopyTo(lz4Stream);
        }

        compressedStream.Position = 0;
        return compressedStream.ToArray();
    }

    internal static byte[] DecompressContent(byte[] compressedContent)
    {
        using var compressedStream = new MemoryStream(compressedContent);
        using var decompressedStream = new MemoryStream();
        using (var lz4Stream = LZ4Stream.Decode(compressedStream, leaveOpen: true))
        {
            lz4Stream.CopyTo(decompressedStream);
        }

        decompressedStream.Position = 0;
        return decompressedStream.ToArray();
    }

    public string UpdateContentHash()
    {
        Hash = ComputeHash(IsCompressed ? DecompressContent(Content) : Content);
        return Hash;
    }

    private static string ComputeHash(byte[] content) => Hasher.Hash(content).ToString();
}
