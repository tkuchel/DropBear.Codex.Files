using System.Runtime.Versioning;
using System.Text;
using System.Text.Json.Serialization;
using DropBear.Codex.Core;
using DropBear.Codex.Files.Enums;
using DropBear.Codex.Files.Extensions;
using DropBear.Codex.Serialization.Factories;
using DropBear.Codex.Utilities.Extensions;
using DropBear.Codex.Utilities.Hashing.Factories;
using DropBear.Codex.Utilities.Hashing.Interfaces;

namespace DropBear.Codex.Files.Models;

[SupportedOSPlatform("windows")]
public class ContentContainer
{
    private readonly IHasher _hasher = new HashFactory().GetHasher("XxHash");
    private readonly Dictionary<string, Type> _providers = new(StringComparer.OrdinalIgnoreCase);

    public ContentContainer() => Flags = ContentContainerFlags.NoOperation; // Start with NoOperation as default

    [JsonPropertyName("flags")] public ContentContainerFlags Flags { get; private set; }

    [JsonPropertyName("contentType")] public string ContentType { get; private set; } = "Unsupported/Unknown DataType";

#pragma warning disable CA1819
    [JsonPropertyName("data")] public byte[]? Data { get; internal set; }
#pragma warning restore CA1819

    [JsonPropertyName("hash")] public string? Hash { get; private set; }

    public object? TemporaryData { get; private set; }

    public bool RequiresSerialization() => Flags.HasFlag(ContentContainerFlags.ShouldSerialize);
    public bool RequiresCompression() => Flags.HasFlag(ContentContainerFlags.ShouldCompress);
    public bool RequiresEncryption() => Flags.HasFlag(ContentContainerFlags.ShouldEncrypt);

    public Result SetData<T>(T? data)
    {
        if (data is null)
            return Result.Failure("Data is null.");

        switch (data)
        {
            case byte[] byteArray:
                Data = byteArray;
                Flags |= ContentContainerFlags.DataIsSet;
                break;
            case string str:
                Data = Encoding.UTF8.GetBytes(str);
                Flags |= ContentContainerFlags.DataIsSet;
                break;
            default:
                TemporaryData = data;
                Flags |= ContentContainerFlags.TemporaryDataIsSet;
                Flags |= ContentContainerFlags.ShouldSerialize;
                break;
        }

        ContentType = typeof(T).FullName ?? "Unsupported/Unknown DataType";
        if (Data is not null) ComputeAndSetHash();
        return Result.Success();
    }

    public void AddProvider(string key, Type type)
    {
        if (key is null || type is null)
            throw new ArgumentException("Key and Type must not be null.", nameof(key));

        if (!_providers.TryAdd(key, type))
            throw new ArgumentException($"An item with the key '{key}' has already been added.", nameof(key));
    }

    // This method can be used to set up the dictionary during deserialization
    internal void SetProviders(Dictionary<string, Type> providers)
    {
        foreach (var provider in providers) _providers[provider.Key] = provider.Value;
    }

    public Dictionary<string, Type> GetProvidersDictionary() =>
        // Return a new dictionary copying all entries from the _providers
        new(_providers, StringComparer.OrdinalIgnoreCase);

    internal void ComputeAndSetHash() => ComputeHash();

    private void ComputeHash()
    {
        if (Data is null) return;
        var hashResult = _hasher.EncodeToBase64Hash(Data.ToArray());
        Hash = hashResult.IsSuccess ? hashResult.Value : null;
    }

    public async Task<Result<T>> GetDataAsync<T>()
    {
        // Check if data is actually present
        if (Data is null || Data.Length is 0)
            return Result<T>.Failure("No data available.");

        // Verify the integrity of the data
        if (Hash is null)
            return Result<T>.Failure("No hash available.");

        var hashResult = _hasher.EncodeToBase64Hash(Data.ToArray());
        if (!hashResult.IsSuccess || hashResult.Value != Hash)
            return Result<T>.Failure("Data integrity check failed.");

        // Handling the case where no serialization is needed and the type is byte[]
        if (typeof(T) == typeof(byte[]))
            return IsFlagEnabled(ContentContainerFlags.NoSerialization)
                ? Result<T>.Success((T)(object)Data)
                : Result<T>.Failure("No serialization required, but type is byte[].");

        // Configure and build the serializer
        var serializerBuilder = new SerializationBuilder();
        ConfigureContainerSerializer(serializerBuilder);
        var serializer = RequiresSerialization() ? serializerBuilder.Build() : null;

        try
        {
            // Use the serializer to deserialize the data
            if (serializer is null)
                return Result<T>.Failure("No serializer configured.");

            var result = await serializer.DeserializeAsync<T>(Data).ConfigureAwait(false);
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            // Capture and return any errors encountered during deserialization
            return Result<T>.Failure($"Deserialization failed: {ex.Message}");
        }
    }

    internal void ConfigureContainerSerializer(SerializationBuilder serializerBuilder)
    {
        // Retrieve the type from the keyed collection and create the serializer
        if (RequiresSerialization())
        {
            var serializerProviderType = GetProviderType("Serializer");
            serializerBuilder.WithSerializer(serializerProviderType);
        }

        // Retrieve the type from the keyed collection and create the compression provider
        if (RequiresCompression())
        {
            var compressionProviderType = GetProviderType("CompressionProvider");
            serializerBuilder.WithCompression(compressionProviderType);
        }

        // Retrieve the type from the keyed collection and create the encryption provider
        if (RequiresEncryption())
        {
            var encryptionProviderType = GetProviderType("EncryptionProvider");
            serializerBuilder.WithEncryption(encryptionProviderType);
        }
    }

    // Helper method to retrieve provider type from the keyed collection

    private Type GetProviderType(string keyName)
    {
        if (_providers.TryGetValue(keyName, out var type))
            return type;
        throw new KeyNotFoundException($"Provider for {keyName} not found.");
    }

    // Method to enable flags
    public void EnableFlag(ContentContainerFlags flag) => Flags |= flag; // Use bitwise OR to turn on specific flags

    // Method to disable flags
    public void DisableFlag(ContentContainerFlags flag) =>
        Flags &= ~flag; // Use bitwise AND with NOT to turn off specific flags

    // Check if a specific flag is enabled
    private bool IsFlagEnabled(ContentContainerFlags flag) => (Flags & flag) == flag;

    // Method to print current flags
    public void PrintFlags()
    {
        Console.WriteLine("Current Features Enabled:");
        foreach (ContentContainerFlags flag in Enum.GetValues(typeof(ContentContainerFlags)))
            if (IsFlagEnabled(flag))
                Console.WriteLine("- " + flag);
    }

    // Adding internal methods to set private properties
    internal void SetContentType(string type) => ContentType = type;

    internal void SetHash(string? hash) => Hash = hash;

    public override bool Equals(object? obj)
    {
        if (obj is ContentContainer other)
            return Hash == other.Hash && Equals(Data, other.Data);

        return false;
    }

    public override int GetHashCode() => HashCode.Combine(Hash.GetReadOnlyVersion(), Data.GetReadOnlyVersion());
}
