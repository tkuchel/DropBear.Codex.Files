using DropBear.Codex.Files.Compression;
using DropBear.Codex.Files.Interfaces;

namespace DropBear.Codex.Files.Strategies;

public class GZipCompressionStrategy : ICompressionStrategy
{
    private readonly GZipCompressor _compressor;

    public GZipCompressionStrategy()
    {
        _compressor = new GZipCompressor();
    }

    public byte[] ProcessData(byte[] data)
    {
        return _compressor.Compress(data);
    }

    public byte[] RevertData(byte[] data)
    {
        return _compressor.Decompress(data);
    }
}
