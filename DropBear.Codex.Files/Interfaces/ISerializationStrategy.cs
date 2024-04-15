namespace DropBear.Codex.Files.Interfaces;

public interface ISerializationStrategy
{
    byte[] ProcessData<T>(T data);
    T RevertData<T>(byte[] data);
}
