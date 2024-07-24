#region

using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using DropBear.Codex.Files.Converters;
using DropBear.Codex.Files.Models;

#endregion

namespace DropBear.Codex.Files.Extensions;

[SupportedOSPlatform("windows")]
public static class DropBearFileExtensions
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new TypeConverter(), new ContentContainerConverter() }, WriteIndented = true
    };

    public static MemoryStream ToStream(this DropBearFile file)
    {
        if (file is null)
        {
            throw new ArgumentNullException(nameof(file), "Cannot serialize a null DropBearFile object.");
        }

        string jsonString;

        try
        {
            jsonString = JsonSerializer.Serialize(file, Options);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Serialization failed.", ex);
        }

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
        return stream;
    }

    public static async Task<MemoryStream?> ToStreamAsync(this DropBearFile file)
    {
        if (file is null)
        {
            throw new ArgumentNullException(nameof(file), "Cannot serialize a null DropBearFile object.");
        }

        var stream = new MemoryStream();
        try
        {
            await JsonSerializer.SerializeAsync(stream, file, Options).ConfigureAwait(false);
            stream.Position = 0; // Reset position after writing
            return stream;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Serialization failed.", ex);
        }
    }


    public static DropBearFile FromStream(Stream stream)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream), "Cannot deserialize from a null stream.");
        }

        if (!stream.CanRead)
        {
            throw new NotSupportedException("Stream must be readable.");
        }

        try
        {
            stream.Position = 0; // Reset the position to ensure correct reading from start
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var jsonString = reader.ReadToEnd();
            var file = JsonSerializer.Deserialize<DropBearFile>(jsonString, Options);

            if (file is null)
            {
                throw new InvalidOperationException("Deserialization resulted in a null object.");
            }

            return file;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Deserialization failed.", ex);
        }
    }

    public static async Task<DropBearFile> FromStreamAsync(Stream stream)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream), "Cannot deserialize from a null stream.");
        }

        if (!stream.CanRead)
        {
            throw new NotSupportedException("Stream must be readable.");
        }

        try
        {
            stream.Position = 0; // Ensure stream is at the beginning
            var file = await JsonSerializer.DeserializeAsync<DropBearFile>(stream, Options).ConfigureAwait(false);
            if (file is null)
            {
                throw new InvalidOperationException("Deserialization resulted in a null object.");
            }

            return file;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Deserialization failed.", ex);
        }
    }
}
