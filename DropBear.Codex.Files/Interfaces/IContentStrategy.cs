namespace DropBear.Codex.Files.Interfaces;

public interface IContentStrategy
{
    byte[] ProcessData(byte[] data);
    byte[] RevertData(byte[] data);
}
