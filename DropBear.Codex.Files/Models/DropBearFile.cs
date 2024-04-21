using System.Text.Json.Serialization;

namespace DropBear.Codex.Files.Models;

public class DropBearFile
{
    public DropBearFile()
    {
        // Empty constructor for deserialization
    }

    // JSON constructor for detailed creation
    [JsonConstructor]
    public DropBearFile(
        Dictionary<string, string>? metadata,
        List<ContentContainer>? contentContainers,
        List<FileVersion>? versions,
        string baseFilePath,
        string fileName,
        FileVersion? currentVersion)
    {
        Metadata = metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ContentContainers = contentContainers ?? new List<ContentContainer>();
        Versions = versions ?? new List<FileVersion>();
        BaseFilePath = baseFilePath;
        FileName = fileName;
        CurrentVersion = currentVersion;
    }

    // Properties with public setters to allow deserialization
    public string BaseFilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public static string DefaultExtension { get; } = ".dbf";

    // Using attributes on properties to guide serialization
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("contentContainers")]
    public List<ContentContainer> ContentContainers { get; set; } = new();

    [JsonPropertyName("versions")] public List<FileVersion> Versions { get; set; } = new();

    [JsonPropertyName("currentVersion")] public FileVersion? CurrentVersion { get; set; }

    public string FullPath => Path.Combine(BaseFilePath, $"{FileName}{DefaultExtension}");

    // Methods for adding and modifying the DropBearFile contents, these do not need serialization attributes
    public void AddMetadata(string key, string value)
    {
        if (!Metadata.TryAdd(key, value))
            throw new ArgumentException("Duplicate metadata key.", nameof(key));
    }
    
    public void RemoveMetadata(string key)
    {
        if (!Metadata.Remove(key))
            throw new ArgumentException("Metadata key not found.", nameof(key));
    }

    public void AddContentContainer(ContentContainer container)
    {
        ArgumentNullException.ThrowIfNull(container, nameof(container));
        ContentContainers.Add(container);
    }
    
    public void RemoveContentContainer(ContentContainer container)
    {
        if (!ContentContainers.Remove(container))
            throw new ArgumentException("Content container not found.", nameof(container));
    }

    public void AddVersion(string label, DateTimeOffset date)
    {
        ArgumentException.ThrowIfNullOrEmpty(label, nameof(label));
        var version = CreateFileVersion(label, date);
        Versions.Add(version);
        CurrentVersion = version; // Optionally set the latest added version as the current version
    }

    public void AddVersion(FileVersion version)
    {
        ArgumentNullException.ThrowIfNull(version, nameof(version));
        Versions.Add(version);
        CurrentVersion = version; // Optionally set the latest added version as the current version
    }
    
    public FileVersion CreateFileVersion(string versionLabel, DateTimeOffset versionDate)
    {
        var baseFilePath = BaseFilePath;
        var currentFilePath = Path.Combine(baseFilePath, $"{FileName}{DefaultExtension}");
        var newFilePath = Path.Combine(baseFilePath, $"{FileName}_new{DefaultExtension}");
        var deltaPath = Path.Combine(baseFilePath, $"{FileName}.{versionLabel}.delta");
        var signaturePath = Path.Combine(baseFilePath, $"{FileName}.{versionLabel}.sig");

        return new FileVersion(versionLabel, versionDate, currentFilePath, newFilePath, deltaPath, signaturePath, baseFilePath);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is DropBearFile other)
        {
            return BaseFilePath == other.BaseFilePath &&
                   FileName == other.FileName &&
                   CurrentVersion.Equals(other.CurrentVersion) &&
                   Metadata.SequenceEqual(other.Metadata) &&
                   Versions.SequenceEqual(other.Versions) &&
                   ContentContainers.SequenceEqual(other.ContentContainers);
        }
        return false;
    }

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(BaseFilePath);
        hash.Add(FileName);
        hash.Add(CurrentVersion);
        foreach (var item in Metadata)
        {
            hash.Add(item.Key);
            hash.Add(item.Value);
        }
        foreach (var version in Versions)
        {
            hash.Add(version);
        }
        foreach (var container in ContentContainers)
        {
            hash.Add(container);
        }
        return hash.ToHashCode();
    }

}
