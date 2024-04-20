namespace DropBear.Codex.Files.Extensions;

public static class StreamExtensions
{
    public static async Task CopyToFileAsync(this Stream input, string filePath)
    {
        var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await using (fileStream.ConfigureAwait(false))
        {
            await input.CopyToAsync(fileStream).ConfigureAwait(false);
        }
    }
}

