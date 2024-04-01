using Blake3;
using DropBear.Codex.Files.Exceptions;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.MainComponents;

[MessagePackObject]
public class FileContent
{
    [Key(0)] public List<ContentContainer> Contents { get; set; } = [];

    public void AddContent(ContentContainer content) => Contents.Add(content);

    public void RemoveContent(ContentContainer content) => Contents.Remove(content);

    public void ClearContent() => Contents.Clear();

    private static bool VerifyContentHash(ContentContainer content)
    {
        var computedHash = Hasher.Hash(content.Content).ToString();
        return content.Hash == computedHash;
    }

    // Retrieval methods

    public IEnumerable<ContentContainer> GetAllContents<T>() =>
        Contents.Where(c => c.ContentType.GetContentType() == typeof(T));

    public ContentContainer GetContent<T>() => GetAllContents<T>().FirstOrDefault() ?? throw new FileContentNotFoundException();

    public ContentContainer GetContent() => Contents.FirstOrDefault() ?? throw new FileContentNotFoundException();

    public byte[] GetRawContent<T>()
    {
        var container = GetContent<T>();
        return container?.Content ?? throw new FileContentNotFoundException();
    }

    public byte[] GetRawContent()
    {
        var container = GetContent();
        return container?.Content ?? throw new FileContentNotFoundException();
    }

    // Search methods based on type, name, and hash

    public ContentContainer FindContainerForType<T>() =>
        Contents.Find(c => c.ContentType.GetContentType() == typeof(T)) ?? throw new FileContentNotFoundException();

    public ContentContainer FindContainerForTypeAndName<T>(string name) =>
        Contents.Find(c => c.ContentType.GetContentType() == typeof(T) && c.Name == name) ??
        throw new FileContentNotFoundException();

    public ContentContainer FindContainerForTypeAndNameAndHash<T>(string name, string hash) =>
        Contents.Find(c => c.ContentType.GetContentType() == typeof(T) && c.Name == name && c.Hash == hash) ??
        throw new FileContentNotFoundException();

    public ContentContainer FindContainerForName(string name) =>
        Contents.Find(c => c.Name == name) ?? throw new FileContentNotFoundException();

    public ContentContainer FindContainerForNameAndHash(string name, string hash) =>
        Contents.Find(c => c.Name == name && c.Hash == hash) ?? throw new FileContentNotFoundException();

    public ContentContainer FindContainerForHash(string hash) =>
        Contents.Find(c => c.Hash == hash) ?? throw new FileContentNotFoundException();

    // Method to verify hashes of all content containers
    public bool VerifyAllContentHashes() => Contents.TrueForAll(VerifyContentHash);
}
