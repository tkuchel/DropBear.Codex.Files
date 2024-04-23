using System.Runtime.Versioning;
using DropBear.Codex.Files.Enums;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Serialization.Factories;
using DropBear.Codex.Serialization.Interfaces;

namespace DropBear.Codex.Files.Builders;

[SupportedOSPlatform("windows")]
public class ContentContainerBuilder
{
    private readonly ContentContainer _container = new();
    private Type? _compressionProviderType;
    private Type? _encryptionProviderType;
    private Type? _serializerType;

    public ContentContainerBuilder WithObject<T>(T obj)
    {
        var setResult = _container.SetData(obj);
        if (!setResult.IsSuccess)
            throw new InvalidOperationException($"Failed to set data: {setResult.Error}");
        return this;
    }

    public ContentContainerBuilder WithData(byte[] data)
    {
        var setResult = _container.SetData(data);
        if (!setResult.IsSuccess)
            throw new InvalidOperationException($"Failed to set data: {setResult.Error}");
        return this;
    }

    public ContentContainerBuilder WithSerializer<T>() where T : ISerializer
    {
        _serializerType = typeof(T);
        return this;
    }

    public ContentContainerBuilder WithCompression<T>() where T : ICompressionProvider
    {
        _compressionProviderType = typeof(T);
        return this;
    }

    public ContentContainerBuilder WithEncryption<T>() where T : IEncryptionProvider
    {
        _encryptionProviderType = typeof(T);
        return this;
    }

    public ContentContainerBuilder NoSerialization()
    {
        _serializerType = null;
        return this;
    }

    public ContentContainerBuilder NoCompression()
    {
        _compressionProviderType = null;
        return this;
    }

    public ContentContainerBuilder NoEncryption()
    {
        _encryptionProviderType = null;
        return this;
    }

    public async Task<ContentContainer> BuildAsync()
    {
        // Handle no serializer or no serialization required
        if (_serializerType is null && !_container.RequiresSerialization())
        {
            _container.ComputeAndSetHash();
            _container.EnableFlag(ContentContainerFlags.NoSerialization);
            return _container;
        }

        // Handle compression
        if (_compressionProviderType is not null)
            _container.AddProvider("CompressionProvider", _compressionProviderType);

        // Handle encryption
        if (_encryptionProviderType is not null)
            _container.AddProvider("EncryptionProvider", _encryptionProviderType);

        // Register the serializer type in the providers collection
        if (_serializerType is not null)
            _container.AddProvider("Serializer", _serializerType);

        // Configure and build the serializer
        var serializerBuilder = new SerializationBuilder();
        _container.ConfigureContainerSerializer(serializerBuilder);
        var serializer = _container.RequiresSerialization() ? serializerBuilder.Build() : null;

        // Get the data that needs to be serialized
        var data = _container.TemporaryData;
        if (data is null)
            throw new InvalidOperationException("No data is available for serialization.");

        // Serialize the data
        if (serializer is null) return _container;

        var serializedData = await serializer.SerializeAsync(data).ConfigureAwait(false);
        if (serializedData is null || serializedData.Length is 0)
            throw new InvalidOperationException("Serialization failed to produce data.");

        // Store the serialized data in the container
        _container.Data = serializedData;
        _container.ComputeAndSetHash();


        return _container;
    }
}
