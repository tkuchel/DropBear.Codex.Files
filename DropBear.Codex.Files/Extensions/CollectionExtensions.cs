namespace DropBear.Codex.Files.Extensions;

public static class CollectionExtensions
{
    public static byte[] ToArray(this IReadOnlyCollection<byte> collection)
    {
        // Using System.Linq to call the correct ToArray() method
        // ReSharper disable once RemoveToList.1
        return collection.ToList().ToArray();
    }
}

