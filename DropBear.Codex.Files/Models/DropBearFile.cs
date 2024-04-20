using DropBear.Codex.Files.Builders;
using DropBear.Codex.Files.Interfaces;

namespace DropBear.Codex.Files.Models;

public class DropBearFile
{
    private readonly List<IContentContainer> _contentContainers = [];
    private readonly List<IFileVersion> _versions = [];

    public DropBearFile() => Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<string, string> Metadata { get; }
    public IReadOnlyList<IFileVersion> Versions => _versions.AsReadOnly();
    public IReadOnlyList<IContentContainer> ContentContainers => _contentContainers.AsReadOnly();

    public static DropBearFileBuilder CreateBuilder() => new(new DropBearFile());

    public void AddMetadata(string key, string value)
    {
        if (!Metadata.TryAdd(key, value))
            throw new ArgumentException("Duplicate metadata key.", nameof(key));
    }

    public void RemoveMetadata(string key) => Metadata.Remove(key);

    public void AddVersion(IFileVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);
        _versions.Add(version);
    }

    public void RemoveVersion(IFileVersion version) => _versions.Remove(version);

    public void AddContentContainer(IContentContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        _contentContainers.Add(container);
    }

    public void RemoveContentContainer(IContentContainer container) => _contentContainers.Remove(container);
}
