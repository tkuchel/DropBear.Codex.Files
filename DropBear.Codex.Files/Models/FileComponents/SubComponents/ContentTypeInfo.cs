using System.Reflection;
using DropBear.Codex.Files.Exceptions;
using MessagePack;

namespace DropBear.Codex.Files.Models.FileComponents.SubComponents;

[MessagePackObject]
public class ContentTypeInfo
{
    private static readonly Dictionary<string, Type> TypeCache = new(StringComparer.OrdinalIgnoreCase);

    [Obsolete("For MessagePack deserialization. Use parameterized constructor for explicit instantiation.", false)]
    public ContentTypeInfo()
    {
        AssemblyName = string.Empty;
        TypeName = string.Empty;
        NameSpace = string.Empty;
    }

    public ContentTypeInfo(Type type) : this(type.Assembly.GetName().Name ?? string.Empty, type.Name,
        type.Namespace ?? string.Empty)
    {
    }

    public ContentTypeInfo(string assemblyName, string typeName, string nameSpace)
    {
        AssemblyName = string.IsNullOrWhiteSpace(assemblyName)
            ? throw new ArgumentException("Assembly name cannot be null or whitespace.", nameof(assemblyName))
            : assemblyName;
        TypeName = string.IsNullOrWhiteSpace(typeName)
            ? throw new ArgumentException("Type name cannot be null or whitespace.", nameof(typeName))
            : typeName;
        NameSpace = string.IsNullOrWhiteSpace(nameSpace)
            ? throw new ArgumentException("Namespace cannot be null or whitespace.", nameof(nameSpace))
            : nameSpace;
    }

    [Key(0)] public string AssemblyName { get; }

    [Key(1)] public string TypeName { get; }

    [Key(2)] public string NameSpace { get; }

    private string GetFullTypeName() => $"{NameSpace}.{TypeName}";

    public Type GetContentType()
    {
        var fullTypeName = GetFullTypeName();
        if (TypeCache.TryGetValue(fullTypeName, out var type)) return type;

        type = Type.GetType(fullTypeName) ?? Assembly.Load(AssemblyName).GetType(fullTypeName);

        TypeCache[fullTypeName] =
            type ?? throw new ContentTypeNotFoundException($"Content type {fullTypeName} not found.");
        return type;
    }

    public override bool Equals(object? obj) =>
        obj is ContentTypeInfo info &&
        AssemblyName == info.AssemblyName &&
        TypeName == info.TypeName &&
        NameSpace == info.NameSpace;

    public override int GetHashCode() => HashCode.Combine(AssemblyName, TypeName, NameSpace);
}
