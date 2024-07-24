#region

using System.Runtime.Versioning;
using DropBear.Codex.Files.Enums;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Serialization.Factories;
using DropBear.Codex.Serialization.Interfaces;

#endregion

namespace DropBear.Codex.Files.Builders;

[SupportedOSPlatform("windows")]
public class ContentContainerBuilder : IInitialContainerBuilder, IDataConfigurable, ISerializable
{
    private readonly ContentContainer _container = new();
    private Type? _compressionProviderType;
    private Type? _encryptionProviderType;
    private Type? _serializerType;

    public ISerializable WithSerializer<T>() where T : ISerializer
    {
        _serializerType = typeof(T);
        return this;
    }

    public ICompressible WithCompression<T>() where T : ICompressionProvider
    {
        _compressionProviderType = typeof(T);
        return this;
    }

    public IEncryptable WithEncryption<T>() where T : IEncryptionProvider
    {
        _encryptionProviderType = typeof(T);
        return this;
    }

    public IBuildable NoSerialization()
    {
        _serializerType = null;
        return this;
    }

    public IDataConfigurable WithObject<T>(T obj)
    {
        var setResult = _container.SetData(obj);
        if (!setResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to set data: {setResult.ErrorMessage}");
        }

        return this;
    }

    public IBuildable WithData(byte[] data)
    {
        var setResult = _container.SetData(data);
        if (!setResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to set data: {setResult.ErrorMessage}");
        }

        return this;
    }

    IEncryptable ICompressible.WithCompression<T>()
    {
        _compressionProviderType = typeof(T);
        return this;
    }

    IBuildable IEncryptable.WithEncryption<T>()
    {
        _encryptionProviderType = typeof(T);
        return this;
    }

    public IBuildable NoEncryption()
    {
        _encryptionProviderType = null;
        return this;
    }

    public async Task<ContentContainer> BuildAsync()
    {
        // Handle no serializer or no serialization required
        if (_serializerType == null && !_container.RequiresSerialization())
        {
            _container.ComputeAndSetHash();
            _container.EnableFlag(ContentContainerFlags.NoSerialization);
            return _container;
        }

        if (_compressionProviderType != null)
        {
            _container.AddProvider("CompressionProvider", _compressionProviderType);
        }

        if (_encryptionProviderType != null)
        {
            _container.AddProvider("EncryptionProvider", _encryptionProviderType);
        }

        if (_serializerType != null)
        {
            _container.AddProvider("Serializer", _serializerType);
        }

        var serializerBuilder = new SerializationBuilder();
        _container.ConfigureContainerSerializer(serializerBuilder);
        var serializer = _container.RequiresSerialization() ? serializerBuilder.Build() : null;

        var data = _container.TemporaryData;
        if (data == null)
        {
            throw new InvalidOperationException("No data is available for serialization.");
        }

        var serializedData = serializer != null ? await serializer.SerializeAsync(data).ConfigureAwait(false) : null;
        if (serializedData == null || serializedData.Length == 0)
        {
            throw new InvalidOperationException("Serialization failed to produce data.");
        }

        _container.Data = serializedData;
        _container.ComputeAndSetHash();

        return _container;
    }

    public IBuildable NoCompression()
    {
        _compressionProviderType = null;
        return this;
    }
}
