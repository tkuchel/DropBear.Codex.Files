using DropBear.Codex.Files.Interfaces;
using MessagePack;

namespace DropBear.Codex.Files.Strategies;

public class MessagePackSerializationStrategy : ISerializationStrategy
{
    public byte[] ProcessData<T>(T data) =>
        // Directly serialize the object to a MessagePack byte array
        MessagePackSerializer.Serialize(data);

    public T RevertData<T>(byte[] data) =>
        // Directly deserialize the byte array to an object
        MessagePackSerializer.Deserialize<T>(data);

}
