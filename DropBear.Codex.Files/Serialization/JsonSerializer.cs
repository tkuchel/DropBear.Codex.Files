using DropBear.Codex.Files.Interfaces;
using Newtonsoft.Json;

namespace DropBear.Codex.Files.Serialization;

public class JsonSerializer : ISerializer
{
    public string Serialize<T>(T obj)
    {
        return JsonConvert.SerializeObject(obj);
    }

    public T Deserialize<T>(string data)
    {
        return JsonConvert.DeserializeObject<T>(data) ?? throw new InvalidOperationException("Failed to deserialize data");
    }
}
