using System.Collections.Concurrent;
using System.Reflection;
using DropBear.Codex.Core.ReturnTypes;
using MessagePack;

namespace DropBear.Codex.Files.MessagePackChecker;

public class MessagePackCompatibilityChecker
{
    private readonly ConcurrentDictionary<Type, bool> _compatibilityCache = new();

    public Result IsSerializable<T>() where T : class
    {
        var type = typeof(T);
        var isSerializable = _compatibilityCache.GetOrAdd(type, IsSerializableInternal);
        return isSerializable
            ? Result.Success()
            : Result.Failure($"{type.FullName} is not compatible with MessagePack serialization.");
    }

    private bool IsSerializableInternal(Type type)
    {
        // Basic checks that can be extended with more complex logic
        if (!type.IsPublic || type.IsNested) return false;

        var hasMessagePackObjectAttribute = Attribute.IsDefined(type, typeof(MessagePackObjectAttribute));
        if (!hasMessagePackObjectAttribute) return false;

        // Additional checks for members, unions, etc., can be inserted here
        return CheckSerializableMembers(type) && EnsureValidUnionAttributes(type);
    }

    private static bool CheckSerializableMembers(Type type)
    {
        var messagePackObjectAttribute = type.GetCustomAttribute<MessagePackObjectAttribute>();

        if (messagePackObjectAttribute?.KeyAsPropertyName ?? false) return true;

        var members = GetAllSerializableMembers(type);
        return members.All(member =>
            member.GetCustomAttribute<KeyAttribute>() is not null ||
            member.GetCustomAttribute<IgnoreMemberAttribute>() is not null);
    }

    private static IEnumerable<MemberInfo> GetAllSerializableMembers(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Cast<MemberInfo>()
            .Concat(type.GetFields(BindingFlags.Public | BindingFlags.Instance));

    private static bool EnsureValidUnionAttributes(Type type)
    {
        var unionAttributes = type.GetCustomAttributes<UnionAttribute>().ToList();
        if (unionAttributes.Count is 0) return true;

        return unionAttributes.TrueForAll(attr =>
            (attr.SubType.IsSubclassOf(type) || type.IsAssignableFrom(attr.SubType)) &&
            Attribute.IsDefined(attr.SubType, typeof(MessagePackObjectAttribute)));
    }
}
