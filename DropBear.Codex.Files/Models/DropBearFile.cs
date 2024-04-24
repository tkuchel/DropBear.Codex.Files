using System.Collections.ObjectModel;
using System.Runtime.Versioning;
using System.Text.Json.Serialization;
using DropBear.Codex.Utilities.Extensions;

namespace DropBear.Codex.Files.Models;

[SupportedOSPlatform("windows")]
public class DropBearFile
{
    private const string DefaultExtension = ".dbf";

    public DropBearFile()
    {
        // Initialize the collections in the constructor
        ContentContainers = new Collection<ContentContainer>();
    }

    [JsonConstructor]
    public DropBearFile(
        Dictionary<string, string>? metadata,
        Collection<ContentContainer>? contentContainers,
        string fileName,
        FileVersion? currentVersion)
    {
        Metadata = metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ContentContainers = contentContainers ?? new Collection<ContentContainer>();
        FileName = fileName;
        CurrentVersion = currentVersion;
    }

    // Properties with public setters to allow deserialization
    public string FileName { get; set; } = string.Empty;

    // Using attributes on properties to guide serialization
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("contentContainers")]
    public Collection<ContentContainer> ContentContainers { get; set; }

    [JsonPropertyName("currentVersion")] public FileVersion? CurrentVersion { get; set; }

    public static string GetDefaultExtension() => DefaultExtension;

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

    public override bool Equals(object? obj)
    {
        if (obj is DropBearFile other)
            return CurrentVersion is not null &&
                   FileName == other.FileName &&
                   CurrentVersion.Equals(other.CurrentVersion) &&
                   Metadata.SequenceEqual(other.Metadata) &&
                   ContentContainers.SequenceEqual(other.ContentContainers);
        return false;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(FileName.GetReadOnlyVersion());
        hash.Add(CurrentVersion.GetReadOnlyVersion());
        foreach (var item in Metadata)
        {
            hash.Add(item.Key);
            hash.Add(item.Value);
        }

        foreach (var container in ContentContainers) hash.Add(container);
        return hash.ToHashCode();
    }
}
