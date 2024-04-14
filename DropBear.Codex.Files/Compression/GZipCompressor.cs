using System.IO.Compression;
using DropBear.Codex.Files.Interfaces;
using Microsoft.IO;

namespace DropBear.Codex.Files.Compression;

public class GZipCompressor : ICompressor
{
    private readonly RecyclableMemoryStreamManager _memoryStreamManager;

    public GZipCompressor() => _memoryStreamManager = new RecyclableMemoryStreamManager();

    public byte[] Compress(byte[] data)
    {
        using var compressedStream = _memoryStreamManager.GetStream("GZipCompressor-Compress");
        using var zipStream = new GZipStream(compressedStream, CompressionMode.Compress, true);
        zipStream.Write(data, 0, data.Length);
        zipStream.Close();
        compressedStream.Position = 0; // Reset position to read the stream content
        return compressedStream.ToArray();
    }

    public byte[] Decompress(byte[] data)
    {
        using var compressedStream = _memoryStreamManager.GetStream("GZipCompressor-Decompress-Input", data);
        using var decompressedStream = _memoryStreamManager.GetStream("GZipCompressor-Decompress-Output");
        using var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        zipStream.CopyTo(decompressedStream);
        decompressedStream.Position = 0; // Reset position to read the stream content
        return decompressedStream.ToArray();
    }
}
