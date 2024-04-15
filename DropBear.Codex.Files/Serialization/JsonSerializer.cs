using System.Text;
using DropBear.Codex.Files.Interfaces;
using Newtonsoft.Json;

namespace DropBear.Codex.Files.Serialization;

public class JsonSerializer : ISerializer
{
    public string Serialize<T>(T obj) => JsonConvert.SerializeObject(obj);

    public byte[] SerializeToByteArray<T>(T obj)
    {
        var serializedData = JsonConvert.SerializeObject(obj);
        return Encoding.UTF8.GetBytes(serializedData);
    }

    public T Deserialize<T>(string data) => JsonConvert.DeserializeObject<T>(data) ??
                                            throw new InvalidOperationException("Failed to deserialize data");

    public T DeserializeFromByteArray<T>(byte[] data)
    {
        var serializedData = Encoding.UTF8.GetString(data);
        return JsonConvert.DeserializeObject<T>(serializedData) ??
               throw new InvalidOperationException("Failed to deserialize data");
    }
}
