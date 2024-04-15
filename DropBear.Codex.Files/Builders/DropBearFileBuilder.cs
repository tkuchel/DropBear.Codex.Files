using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Builders;

public class DropBearFileBuilder
{
    private readonly DropBearFile _file;

    public DropBearFileBuilder(DropBearFile file) => _file = file;

    public DropBearFileBuilder AddMetadata(string key, string value)
    {
        _file.Metadata.Add(key, value);
        return this;
    }

    public DropBearFileBuilder AddVersion(IFileVersion version)
    {
        _file.Versions.Add(version);
        return this;
    }

    public DropBearFileBuilder AddContentContainer(IContentContainer container)
    {
        _file.ContentContainers.Add(container);
        return this;
    }

    public DropBearFile Build() => _file;
}
