using DropBear.Codex.Files.Interfaces;

namespace DropBear.Codex.Files.Serialization;

public class MessagePackSerializer : ISerializer
{
    public string Serialize<T>(T obj)
    {
        // Serialize the object to MessagePack binary format
        var binaryData = MessagePack.MessagePackSerializer.Serialize(obj);
        // Convert binary data to a base64 string to comply with the interface
        return Convert.ToBase64String(binaryData);
    }

    public T Deserialize<T>(string data)
    {
        // Convert the base64 string back to binary data
        var binaryData = Convert.FromBase64String(data);
        // Deserialize the MessagePack binary format to an object
        return MessagePack.MessagePackSerializer.Deserialize<T>(binaryData);
    }
}
