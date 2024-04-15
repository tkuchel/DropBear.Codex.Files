using System.Text;
using DropBear.Codex.Files.Interfaces;

namespace DropBear.Codex.Files.Builders;

public class ContentContainerBuilder
{
    private readonly ContentContainer _container;
    private IContentStrategy? _hashStrategy;

    public ContentContainerBuilder() => _container = new ContentContainer();

    public ContentContainerBuilder WithObject<T>(T obj, ISerializationStrategy serializationStrategy)
    {
        _container.ContentType = typeof(T).AssemblyQualifiedName ?? "Unidentifiable Content Type";
        var data = serializationStrategy.ProcessData(obj);
        _container.SetData(data);
        _container.ComputeHash();
        _container.IsSerialized = true;
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
        if (_hashStrategy != null)
            _container.Hash = Encoding.UTF8.GetString(_hashStrategy.ProcessData(_container.Data));
        else
            _container.ComputeHash();
        return this;
    }

    public ContentContainer Build()
    {
        if (_container.Data == null || _container.Data.Length == 0)
            throw new InvalidOperationException("Container data must be set before building.");
        return _container;
    }
}
