using System.Text;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Builders;

public class ContentContainerBuilder
{
    private ContentContainer _container;
    private IContentStrategy? _hashStrategy;

    public ContentContainerBuilder() => _container = new ContentContainer();

    public ContentContainerBuilder WithObject<T>(T obj, ISerializationStrategy serializationStrategy)
    {
        _container = null;
        _container = ContentContainer.CreateFrom(obj,serializationStrategy);
        return this;
    }
    
    public ContentContainerBuilder WithData(byte[] data)
    {
        _container.SetData(data);
        return this;
    }

    public ContentContainerBuilder ApplyStrategy(IContentStrategy strategy)
    {
        _container.ApplyStrategy(strategy);
        return this;
    }

    public ContentContainerBuilder WithHashStrategy(IContentStrategy hashStrategy)
    {
        _hashStrategy = hashStrategy;
        return this;
    }

    public ContentContainerBuilder ComputeHashIfNeeded()
    {
        if (_hashStrategy is not null)
            _container.Hash = Encoding.UTF8.GetString(_hashStrategy.ProcessData(_container.Data));
        else
            _container.ComputeHash();
        return this;
    }

    public ContentContainer Build()
    {
        if (_container.Data is null || _container.Data.Length is 0)
            throw new InvalidOperationException("Container data must be set before building.");
        return _container;
    }
}
