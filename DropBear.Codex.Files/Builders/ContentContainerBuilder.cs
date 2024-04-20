using DropBear.Codex.Files.Models;
using DropBear.Codex.Serialization.Factories;
using DropBear.Codex.Serialization.Interfaces;

namespace DropBear.Codex.Files.Builders;

/// <summary>
///     Builds a ContentContainer with configurable serialization, compression, and encryption capabilities.
/// </summary>
public class ContentContainerBuilder
{
    private readonly ContentContainer _container = new();
    private ICompressionProvider? _compressionProvider;
    private IEncryptionProvider? _encryptionProvider;
    private ISerializer? _serializationProvider;

    /// <summary>
    ///     Configures the container with an object that should be handled by the builder.
    /// </summary>
    public ContentContainerBuilder WithObject<T>(T obj)
    {
        var setResult = _container.SetData(obj);
        if (!setResult.IsSuccess)
            throw new InvalidOperationException($"Failed to set data: {setResult.Error}");
        return this;
    }

    /// <summary>
    ///     Configures the container with raw byte data.
    /// </summary>
    public ContentContainerBuilder WithData(byte[] data)
    {
        var setResult = _container.SetData(data);
        if (!setResult.IsSuccess)
            throw new InvalidOperationException($"Failed to set data: {setResult.Error}");
        _container._flagManager.ClearFlag("ShouldSerialize");
        return this;
    }

    /// <summary>
    ///     Adds a serialization provider to the builder. Provider must be specified to enable serialization.
    /// </summary>
    public ContentContainerBuilder WithSerializer(ISerializer serializer)
    {
        _serializationProvider = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _container._flagManager.SetFlag("ShouldSerialize");
        return this;
    }

    /// <summary>
    ///     Adds a compression provider to the builder. Provider must be specified to enable compression.
    /// </summary>
    public ContentContainerBuilder WithCompression(ICompressionProvider compressionProvider)
    {
        _compressionProvider = compressionProvider ?? throw new ArgumentNullException(nameof(compressionProvider));
        _container._flagManager.SetFlag("ShouldCompress");
        return this;
    }

    /// <summary>
    ///     Adds an encryption provider to the builder. Provider must be specified to enable encryption.
    /// </summary>
    public ContentContainerBuilder WithEncryption(IEncryptionProvider encryptionProvider)
    {
        _encryptionProvider = encryptionProvider ?? throw new ArgumentNullException(nameof(encryptionProvider));
        _container._flagManager.SetFlag("ShouldEncrypt");
        return this;
    }

    /// <summary>
    ///     Clears the serialization configuration, disabling serialization.
    /// </summary>
    public ContentContainerBuilder NoSerialization()
    {
        _container._flagManager.ClearFlag("ShouldSerialize");
        return this;
    }

    /// <summary>
    ///     Clears the compression configuration, disabling compression.
    /// </summary>
    public ContentContainerBuilder NoCompression()
    {
        _container._flagManager.ClearFlag("ShouldCompress");
        return this;
    }

    /// <summary>
    ///     Clears the encryption configuration, disabling encryption.
    /// </summary>
    public ContentContainerBuilder NoEncryption()
    {
        _container._flagManager.ClearFlag("ShouldEncrypt");
        return this;
    }

    /// <summary>
    ///     Asynchronously builds the ContentContainer applying serialization, compression, and encryption based on the
    ///     configuration.
    /// </summary>
    public async Task<ContentContainer> BuildAsync()
    {
        ValidateContainerSetup();
        
        if(_container._flagManager.IsFlagSet("NoOperation"))
            return _container;

        var builder = new SerializationBuilder();
        ConfigureBuilder(builder);

        var serializer = builder.Build();

        if (_container.Data is null || _container.Data.Count is 0)
            return _container;

        var serializeResult = await serializer.SerializeAsync(_container.Data.ToArray()).ConfigureAwait(false);
        if (serializeResult is null || serializeResult.Length is 0)
            throw new InvalidOperationException("Failed to serialize the data.");

        _container.Data = serializeResult;

        return _container;
    }

    private void ValidateContainerSetup()
    {
        if (_container is null)
            throw new InvalidOperationException("No container is set.");

        if (_container.Data is not null && _container.Data.Count != 0 &&
            _container._flagManager.IsFlagSet("IsDataSet")) return;
        if (!_container._flagManager.IsFlagSet("IsTemporaryDataSet"))
            throw new InvalidOperationException("No data is set in the container.");
    }

    private void ConfigureBuilder(SerializationBuilder builder)
    {
        // Configure serializer if flag is set and provider is available
        if (_container._flagManager.IsFlagSet("ShouldSerialize"))
        {
            if (_serializationProvider is null)
                throw new InvalidOperationException("No serialization provider is set.");
            builder.WithSerializer(_serializationProvider);
            _container.SerializerType = _serializationProvider.GetType();
        }

        // Configure compression if flag is set and provider is available
        if (_container._flagManager.IsFlagSet("ShouldCompress"))
        {
            if (_compressionProvider is null)
                throw new InvalidOperationException("No compression provider is set.");
            builder.WithCompression(_compressionProvider);
            _container.CompressionType = _compressionProvider.GetType();
        }

        // Configure encryption if flag is set and provider is available
        if (!_container._flagManager.IsFlagSet("ShouldEncrypt")) return;
        if (_encryptionProvider is null)
            throw new InvalidOperationException("No encryption provider is set.");
        builder.WithEncryption(_encryptionProvider);
        _container.EncryptionType = _encryptionProvider.GetType();
    }
}
