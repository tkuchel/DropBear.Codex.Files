using DropBear.Codex.Core;

namespace DropBear.Codex.Files.Interfaces;

public interface IContentContainer
{
    string ContentType { get; set; }
    IReadOnlyCollection<byte>? Data { get; }
    string? Hash { get; }
    Type? SerializerType { get; set; }
    Type? CompressionType { get; set; }
    Type? EncryptionType { get; set; }
    Result SetData<T>(T? data);
    Task<Result<T>> GetDataAsync<T>();
}
