namespace DropBear.Codex.Files.Interfaces;

public interface ISerializer
{
    string Serialize<T>(T obj);
    byte[] SerializeToByteArray<T>(T obj);
    T Deserialize<T>(string data);
    T DeserializeFromByteArray<T>(byte[] data);
}
