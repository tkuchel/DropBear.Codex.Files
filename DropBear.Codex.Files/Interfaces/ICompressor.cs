namespace DropBear.Codex.Files.Interfaces;

public interface ICompressor
{
    byte[] Compress(byte[] data);
    byte[] Decompress(byte[] data); 
}
