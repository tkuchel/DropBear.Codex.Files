#region

using System.Runtime.Versioning;
using DropBear.Codex.Files.Models;

#endregion

namespace DropBear.Codex.Files.Builders;

[SupportedOSPlatform("windows")]
public class DropBearFileBuilder
{
    private readonly DropBearFile _file;

    public DropBearFileBuilder()
    {
        _file = new DropBearFile();
    }

    public DropBearFileBuilder SetFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        _file.FileName = fileName;
        return this;
    }

    public DropBearFileBuilder AddMetadata(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key) || value is null)
        {
            throw new ArgumentException("Key or value cannot be null or empty.", nameof(key));
        }

        _file.AddMetadata(key, value);
        return this;
    }

    public DropBearFileBuilder AddContentContainer(ContentContainer container)
    {
        if (container is null)
        {
            throw new ArgumentNullException(nameof(container), "Content container cannot be null.");
        }

        _file.AddContentContainer(container);
        return this;
    }

    public DropBearFileBuilder SetInitialVersion(string label, DateTimeOffset date)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Version label cannot be null or empty.", nameof(label));
        }

        _file.CurrentVersion = new FileVersion(label, date);
        return this;
    }


    public DropBearFile Build()
    {
        if (_file.CurrentVersion is null)
        {
            throw new InvalidOperationException("At least one version must be set before building the file.");
        }

        if (string.IsNullOrEmpty(_file.FileName))
        {
            throw new InvalidOperationException("File name must be set before building the file.");
        }

        return _file;
    }
}
