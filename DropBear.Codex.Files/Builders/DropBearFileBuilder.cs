using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Builders;

public class DropBearFileBuilder
{
    private readonly DropBearFile _file;

    private string baseFilePath;

    public DropBearFileBuilder() =>
        _file = new DropBearFile();

    public DropBearFileBuilder AddMetadata(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));
        if (value is null)
            throw new ArgumentNullException(nameof(value), "Value cannot be null.");

        try
        {
            _file.AddMetadata(key, value);
        }
        catch (ArgumentException ex)
        {
            // Maybe handle or rethrow with more context or logging if necessary
            throw new ArgumentException("Failed to add metadata.", nameof(key), ex);
        }

        return this;
    }

    public DropBearFileBuilder SetBaseFilePath(string path)
    {
        baseFilePath = path;
        return this;
    }

    public DropBearFileBuilder SetInitialVersion(string label, DateTime date)
    {
        if (string.IsNullOrEmpty(baseFilePath))
            throw new InvalidOperationException("Base file path must be set before setting the initial version.");

        var fileVersion = new FileVersion(label, date, baseFilePath);
        _file.AddVersion(fileVersion); // Assume this method sets the version in DropBearFile
        return this;
    }

    public DropBearFileBuilder AddVersion(IFileVersion version)
    {
        if (version is null)
            throw new ArgumentNullException(nameof(version), "Version cannot be null.");

        try
        {
            _file.AddVersion(version);
        }
        catch (Exception ex) // Consider specifying the type of expected exceptions if known
        {
            // Handle or rethrow the exception with more context or logging if necessary
            throw new InvalidOperationException("Failed to add version.", ex);
        }

        return this;
    }


    public DropBearFileBuilder AddContentContainer(IContentContainer container)
    {
        if (container is null)
            throw new ArgumentNullException(nameof(container), "Content container cannot be null.");

        try
        {
            _file.AddContentContainer(container);
        }
        catch (Exception ex) // Consider specifying the type of expected exceptions if known
        {
            // Handle or rethrow the exception with more context or logging if necessary
            throw new InvalidOperationException("Failed to add content container.", ex);
        }

        return this;
    }


    public DropBearFile Build()
    {
        if (_file.Versions.Count is 0)
            SetInitialVersion("v1.0", DateTime.UtcNow);  // Default initial version if not set

        return _file;
    }
}
