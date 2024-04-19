using DropBear.Codex.Core;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Builders;

public class ContentContainerBuilder
{
    private readonly ContentContainer? _container = new();
    private readonly bool _shouldCompressDefault;
    private readonly bool _shouldEncryptDefault;
    private readonly bool _shouldSerializeDefault;
    private ICompressionStrategy? _compressionStrategy;
    private IEncryptionStrategy? _encryptionStrategy;
    private ISerializationStrategy? _serializationStrategy;

    public ContentContainerBuilder()
    {
        _shouldCompressDefault = true;
        _shouldEncryptDefault = true;
        _shouldSerializeDefault = true;
    }

    public ContentContainerBuilder WithObject<T>(T obj)
    {
        var setResult = _container?.SetData(obj);
        if (!setResult?.IsSuccess ?? false)
            throw new InvalidOperationException(setResult?.Error);
        return this;
    }

    public ContentContainerBuilder WithData(byte[] data)
    {
        var setResult = _container?.SetData(data);
        if (!setResult?.IsSuccess ?? false)
            throw new InvalidOperationException(setResult?.Error);
        return this;
    }

    public ContentContainerBuilder WithSerialization(ISerializationStrategy serializationStrategy)
    {
        _serializationStrategy = serializationStrategy;
        _container?._flagManager.SetFlag("ShouldSerialize");
        return this;
    }

    public ContentContainerBuilder WithCompression(ICompressionStrategy compressionStrategy)
    {
        _compressionStrategy = compressionStrategy;
        _container?._flagManager.SetFlag("ShouldCompress");
        return this;
    }

    public ContentContainerBuilder WithEncryption(IEncryptionStrategy encryptionStrategy)
    {
        _encryptionStrategy = encryptionStrategy;
        _container?._flagManager.SetFlag("ShouldEncrypt");
        return this;
    }

    public ContentContainerBuilder NoSerialization()
    {
        _container?._flagManager.ClearFlag("ShouldSerialize");
        return this;
    }

    public ContentContainerBuilder NoCompression()
    {
        _container?._flagManager.ClearFlag("ShouldCompress");
        return this;
    }

    public ContentContainerBuilder NoEncryption()
    {
        _container?._flagManager.ClearFlag("ShouldEncrypt");
        return this;
    }

    public ContentContainer Build()
    {
        if (_container?.Data is null || _container.Data.Count is 0)
            throw new InvalidOperationException("No data is set in the container.");

        // Apply serialization if needed
        ApplyStrategyIfSet(
            _container?._flagManager.IsFlagSet("ShouldSerialize") ?? _shouldSerializeDefault,
            _serializationStrategy,
            _container.ApplySerialization,
            "Serialization"
        );

        // Apply compression if needed
        ApplyStrategyIfSet(
            _container?._flagManager.IsFlagSet("ShouldCompress") ?? _shouldCompressDefault,
            _compressionStrategy,
            _container.ApplyCompression,
            "Compression"
        );

        // Apply encryption if needed
        ApplyStrategyIfSet(
            _container?._flagManager.IsFlagSet("ShouldEncrypt") ?? _shouldEncryptDefault,
            _encryptionStrategy,
            _container.ApplyEncryption,
            "Encryption"
        );

        return _container ?? throw new InvalidOperationException("Container is null.");
    }

    private void ApplyStrategyIfSet<T>(bool shouldApply, T strategy, Func<T, Result> applyStrategyFunc,
        string strategyName)
    {
        if (!shouldApply) return;
        if (strategy is null)
            throw new InvalidOperationException($"{strategyName} strategy is not set.");

        var result = applyStrategyFunc(strategy);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error);
    }
}
