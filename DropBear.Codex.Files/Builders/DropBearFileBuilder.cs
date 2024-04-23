using System.Runtime.Versioning;
using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Builders;

[SupportedOSPlatform("windows")]
public class DropBearFileBuilder
{
    private readonly DropBearFile _file;

    public DropBearFileBuilder() => _file = new DropBearFile();

    public DropBearFileBuilder SetFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        _file.FileName = fileName;
        return this;
    }

    public DropBearFileBuilder SetBaseFilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException(nameof(path), "Base file path cannot be null or empty.");
        _file.BaseFilePath = path;
        return this;
    }

    public DropBearFileBuilder AddMetadata(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key) || value == null)
            throw new ArgumentException("Key or value cannot be null or empty.", nameof(key));
        _file.AddMetadata(key, value);
        return this;
    }

    public DropBearFileBuilder AddContentContainer(ContentContainer container)
    {
        if (container is null)
            throw new ArgumentNullException(nameof(container), "Content container cannot be null.");
        _file.AddContentContainer(container);
        return this;
    }

    public DropBearFileBuilder SetInitialVersion(string label, DateTimeOffset date)
    {
        if (string.IsNullOrWhiteSpace(_file.BaseFilePath))
            throw new InvalidOperationException("Base file path must be set before setting initial version.");
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Version label cannot be null or empty.", nameof(label));

        _file.AddVersion(label, date);
        return this;
    }

    public DropBearFileBuilder AddVersion(string label, DateTimeOffset date)
    {
        if (string.IsNullOrWhiteSpace(_file.BaseFilePath))
            throw new InvalidOperationException("Base file path must be set before adding a version.");
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Version label cannot be null or empty.", nameof(label));

        _file.AddVersion(label, date);
        UpdateCurrentVersionToLatest();
        return this;
    }

    private void UpdateCurrentVersionToLatest() =>
        _file.CurrentVersion = _file.Versions.MaxBy(v => v.VersionDate);

    public DropBearFile Build()
    {
        if (_file.Versions.Count is 0)
            throw new InvalidOperationException("At least one version must be set before building the file.");
        if (string.IsNullOrEmpty(_file.FileName) || string.IsNullOrEmpty(_file.BaseFilePath))
            throw new InvalidOperationException("File name and base path must be set before building the file.");

        return _file;
    }
}
