using System.Text;
using DropBear.Codex.Core;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Serialization.Factories;
using DropBear.Codex.Serialization.Interfaces;
using DropBear.Codex.Utilities.FeatureFlags;
using DropBear.Codex.Utilities.Hashing.Factories;
using DropBear.Codex.Utilities.Hashing.Interfaces;

namespace DropBear.Codex.Files.Models;

public class ContentContainer : IContentContainer
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
    public string ContentType { get; set; } = string.Empty;
    public IReadOnlyCollection<byte>? Data { get; internal set; }
    public string? Hash { get; private set; } = string.Empty;
    public Type? SerializerType { get; set; }
    public Type? CompressionType { get; set; }
    public Type? EncryptionType { get; set; }

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
        if (Data is null || Data.Count is 0)
            return Result<T>.Failure("No data available.");

        var builder = new SerializationBuilder();
        ConfigureBuilder(builder);

        var serializer = builder.Build();

        var deserializeResult = await serializer.DeserializeAsync<T>(Data.ToArray()).ConfigureAwait(false);
        return deserializeResult is not null
            ? Result<T>.Success(deserializeResult)
            : Result<T>.Failure("Failed to deserialize the data.");
    }

    private Result ComputeHash()
    {
        if (Data is null) return Result.Failure("The container data is null.");
        var hashResult = _hashingService.EncodeToBase64Hash(Data.ToArray());
        if (!hashResult.IsSuccess) return Result.Failure(hashResult.Error);
        Hash = hashResult.Value;
        return Result.Success();
    }

    private Result SetDataFromByteArray(byte[]? data)
    {
        if (data is null || data.Length is 0) return Result.Failure("Data is null or empty.");
        Data = data;
        _flagManager.SetFlag("IsDataSet");
        _flagManager.SetFlag("NoOperation");
        return ComputeHash();
    }

    private Result SetDataFromString(string? data)
    {
        if (string.IsNullOrEmpty(data)) return Result.Failure("Data is null or empty.");
        Data = Encoding.UTF8.GetBytes(data);
        _flagManager.SetFlag("IsDataSet");
        return ComputeHash();
    }

    private Result SetDataFromClassType<T>(T data)
    {
        if (data is null) return Result.Failure("Data is null.");
        _temporaryData = data;
        _flagManager.SetFlag("IsTemporaryDataSet");
        return ComputeHash();
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
}
