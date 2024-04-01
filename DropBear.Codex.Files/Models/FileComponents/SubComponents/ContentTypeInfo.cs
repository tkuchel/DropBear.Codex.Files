using System.Reflection;
using DropBear.Codex.Files.Exceptions;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents;

[MessagePackObject]
public class ContentTypeInfo
{
    // Static cache to hold loaded types for performance
    private static readonly Dictionary<string, Type> TypeCache = new(StringComparer.OrdinalIgnoreCase);

    public ContentTypeInfo(string assemblyName, string typeName, string nameSpace)
    {
        AssemblyName = assemblyName;
        TypeName = typeName;
        NameSpace = nameSpace;
    }

    [Key(0)] public string AssemblyName { get; }

    [Key(1)] public string TypeName { get; }

    [Key(2)] private string NameSpace { get; }

    private string GetFullTypeName() => $"{NameSpace}.{TypeName}";

    public Type GetContentType()
    {
        var fullTypeName = GetFullTypeName();
        if (TypeCache.TryGetValue(fullTypeName, out var type)) return type;

        var assembly = Assembly.Load(AssemblyName);
        type = assembly.GetType(fullTypeName);

        if (type != null) TypeCache[fullTypeName] = type;

        return type ?? throw new ContentTypeNotFoundException("Content type not found.");
    }

    public override bool Equals(object? obj) =>
        obj is ContentTypeInfo info &&
        AssemblyName == info.AssemblyName &&
        TypeName == info.TypeName &&
        NameSpace == info.NameSpace;

    public override int GetHashCode() => HashCode.Combine(AssemblyName, TypeName, NameSpace);
}
