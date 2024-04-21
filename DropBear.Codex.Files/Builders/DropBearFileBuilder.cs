using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Builders;

public class DropBearFileBuilder
{
    private readonly DropBearFile _file;

    public DropBearFileBuilder() => _file = new DropBearFile();

    public DropBearFileBuilder SetFileName(string fileName)
    {
        _file.FileName = fileName;
        return this;
    }

    public DropBearFileBuilder SetBaseFilePath(string path)
    {
        _file.BaseFilePath = path ?? throw new ArgumentNullException(nameof(path), "Base file path cannot be null.");
        return this;
    }

    public DropBearFileBuilder AddMetadata(string key, string value)
    {
        _file.AddMetadata(key, value);
        return this;
    }

    public DropBearFileBuilder AddContentContainer(ContentContainer container)
    {
        _file.AddContentContainer(container);
        return this;
    }

    public DropBearFileBuilder SetInitialVersion(string label, DateTimeOffset date)
    {
        if (string.IsNullOrEmpty(_file.BaseFilePath))
            throw new InvalidOperationException("Base file path must be set before setting initial version.");

        _file.AddVersion(label, date);
        return this;
    }

    public DropBearFileBuilder AddVersion(string label, DateTimeOffset date)
    {
        if (string.IsNullOrEmpty(_file.BaseFilePath))
            throw new InvalidOperationException("Base file path must be set before adding a version.");

        // FileVersion now only needs label, date
        _file.AddVersion(label, date);

        // Update the current version to the latest based on date
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
