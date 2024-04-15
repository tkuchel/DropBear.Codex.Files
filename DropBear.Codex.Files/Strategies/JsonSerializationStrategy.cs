using System.Text;
using DropBear.Codex.Files.Interfaces;
using Newtonsoft.Json;

namespace DropBear.Codex.Files.Strategies;

public class JsonSerializationStrategy : ISerializationStrategy
{
    public byte[] ProcessData<T>(T data)
    {
        // Directly serialize the object to a JSON byte array
        var jsonData = JsonConvert.SerializeObject(data);
        return Encoding.UTF8.GetBytes(jsonData);
    }

    public T RevertData<T>(byte[] data)
    {
        // Directly deserialize the byte array to an object
        var jsonData = Encoding.UTF8.GetString(data);
        return JsonConvert.DeserializeObject<T>(jsonData) ?? throw new InvalidOperationException("Failed to deserialize object.");
    }
}
