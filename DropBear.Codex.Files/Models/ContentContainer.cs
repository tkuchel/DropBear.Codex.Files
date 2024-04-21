using System.Text;
using System.Text.Json.Serialization;
using DropBear.Codex.Core;
using DropBear.Codex.Serialization.Factories;
using DropBear.Codex.Serialization.Interfaces;
using DropBear.Codex.Utilities.FeatureFlags;
using DropBear.Codex.Utilities.Hashing.Factories;
using DropBear.Codex.Utilities.Hashing.Interfaces;

namespace DropBear.Codex.Files.Models;

public class ContentContainer
{
    internal readonly DynamicFlagManager _flagManager = new();
    private readonly IHashingService _hashingService = new HashingServiceFactory().CreateService("XxHash");

    // ReSharper disable once NotAccessedField.Local
    private object? _temporaryData;

    public ContentContainer()
    {
        _flagManager.AddFlag("IsDataSet");
        _flagManager.AddFlag("IsTemporaryDataSet");
        _flagManager.AddFlag("IsHashComputed");
        _flagManager.AddFlag("ShouldSerialize");
        _flagManager.AddFlag("ShouldCompress");
        _flagManager.AddFlag("ShouldEncrypt");
        _flagManager.AddFlag("NoOperation");
    }

    [JsonPropertyName("contentType")] public string ContentType { get; set; } = "Unsupported/Unknown DataType";

    [JsonPropertyName("data")] public byte[]? Data { get; set; } = [];

    [JsonPropertyName("hash")] public string? Hash { get; set; } = string.Empty;

    [JsonIgnore] public Type? SerializerType { get; set; } = null;

    [JsonIgnore] public Type? CompressionType { get; set; } = null;

    [JsonIgnore] public Type? EncryptionType { get; set; } = null;

    public Result SetData<T>(T? data)
    {
        ContentType = typeof(T).AssemblyQualifiedName ?? "Unsupported/Unknown DataType";
        return data switch
        {
            byte[] byteArray => SetDataFromByteArray(byteArray),
            string str => SetDataFromString(str),
#pragma warning disable CA1508
            _ when data is not null && data.GetType().IsClass => SetDataFromClassType(data),
#pragma warning restore CA1508
            _ => Result.Failure("Data type not supported.")
        };
    }

    public async Task<Result<T>> GetDataAsync<T>()
    {
        if (Data is null || Data.Length == 0)
            return Result<T>.Failure("No data available.");

        // Verify the integrity of the data by comparing the stored hash with a newly computed hash.
        var currentHash = ComputeHash();
        if(currentHash.IsSuccess)
            if (Hash != currentHash.Value)
                return Result<T>.Failure("Data integrity check failed.");

        var builder = new SerializationBuilder();
        ConfigureBuilder(builder);
        var serializer = builder.Build();

        var deserializeResult = await serializer.DeserializeAsync<T>(Data.ToArray()).ConfigureAwait(false);
        return deserializeResult is not null
            ? Result<T>.Success(deserializeResult)
            : Result<T>.Failure("Failed to deserialize the data.");
    }

    public async Task<Result<byte[]>> GetRawDataAsync()
    {
        if (Data is null || Data.Length is 0)
            return Result<byte[]>.Failure("No data available.");

        return await Task.FromResult(Data.ToArray()).ConfigureAwait(false);
    }

    private Result<string> ComputeHash()
    {
        if (Data is null) return Result<string>.Failure("The container data is null.");
        var hashResult = _hashingService.EncodeToBase64Hash(Data.ToArray());
        if (!hashResult.IsSuccess) return Result<string>.Failure(hashResult.Error);
        Hash = hashResult.Value;
        return Result<string>.Success(hashResult.Value);
    }

    private Result SetDataFromByteArray(byte[]? data)
    {
        if (data is null || data.Length is 0) return Result.Failure("Data is null or empty.");
        Data = data;
        _flagManager.SetFlag("IsDataSet");
        _flagManager.SetFlag("NoOperation");
        ComputeHash();
        return Result.Success();
    }

    private Result SetDataFromString(string? data)
    {
        if (string.IsNullOrEmpty(data)) return Result.Failure("Data is null or empty.");
        Data = Encoding.UTF8.GetBytes(data);
        _flagManager.SetFlag("IsDataSet");
        ComputeHash();
        return Result.Success();
    }

    private Result SetDataFromClassType<T>(T data)
    {
        if (data is null) return Result.Failure("Data is null.");
        _temporaryData = data;
        _flagManager.SetFlag("IsTemporaryDataSet");
        ComputeHash();
        return Result.Success();
    }

    private void ConfigureBuilder(SerializationBuilder builder)
    {
        if (_flagManager.IsFlagSet("ShouldSerialize") && SerializerType != null)
        {
            if (CreateProvider(SerializerType) is ISerializer serializer) builder.WithSerializer(serializer);
            else throw new InvalidOperationException("Failed to initialize the serializer.");
        }

        if (_flagManager.IsFlagSet("ShouldCompress") && CompressionType != null)
        {
            if (CreateProvider(CompressionType) is ICompressionProvider compressor) builder.WithCompression(compressor);
            else throw new InvalidOperationException("Failed to initialize the compressor.");
        }

        if (!_flagManager.IsFlagSet("ShouldEncrypt") || EncryptionType == null) return;
        if (CreateProvider(EncryptionType) is IEncryptionProvider encryptor) builder.WithEncryption(encryptor);
        else throw new InvalidOperationException("Failed to initialize the encryptor.");
    }

    private static object? CreateProvider(Type providerType)
    {
        var constructor = providerType.GetConstructor(Type.EmptyTypes);
        if (constructor == null) return null;

        try
        {
            return constructor.Invoke(null);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create an instance of {providerType.FullName}: {ex.Message}", ex);
        }
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is ContentContainer other)
        {
            return ContentType == other.ContentType &&
                   EqualityComparer<byte[]>.Default.Equals(Data, other.Data) &&
                   Hash == other.Hash &&
                   SerializerType == other.SerializerType &&
                   CompressionType == other.CompressionType &&
                   EncryptionType == other.EncryptionType;
        }
        return false;
    }

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(ContentType);
        hash.Add(Data);
        hash.Add(Hash);
        hash.Add(SerializerType);
        hash.Add(CompressionType);
        hash.Add(EncryptionType);
        return hash.ToHashCode();
    }

}
