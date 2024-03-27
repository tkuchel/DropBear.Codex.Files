using DropBear.Codex.Files.Models.FileComponents.SubComponents;

namespace DropBear.Codex.Files.Helpers;

public static class ContentTypeHelper
{
    public static List<ContentTypeInfo> GenerateContentTypeInfos(params Type[] types) =>
        types.Select(type => new ContentTypeInfo
        {
            AssemblyName = type.Assembly.FullName,
            TypeName = type.FullName,
            Namespace = type.Namespace,
        }).ToList();
}
