using System.Reflection;
using DropBear.Codex.Core.ReturnTypes;

namespace DropBear.Codex.Files.MessagePackChecker;

public static class MessagePackCompatibilityAggregator
{
    private static readonly MessagePackCompatibilityChecker MessagePackCompatibilityChecker = new();

    public static MessagePackCompatibilityResults CheckTypes(IEnumerable<Type> types)
    {
        var results = new MessagePackCompatibilityResults();

        foreach (var type in types)
        {
            var result = CheckTypeCompatibility(type);
            if (result.IsSuccess)
                results.SuccessTypes.Add(type.Name);
            else
                results.FailedTypes.Add(type.Name, result.ErrorMessage ?? "Unknown error occurred.");
        }

        return results;
    }

    private static Result CheckTypeCompatibility(Type type)
    {
        try
        {
            var isSerializableMethod = typeof(MessagePackCompatibilityChecker)
                .GetMethod(nameof(MessagePackCompatibilityChecker.IsSerializable),
                    BindingFlags.Instance | BindingFlags.Public);

            if (isSerializableMethod == null)
                return Result.Failure($"Method {nameof(MessagePackCompatibilityChecker.IsSerializable)} not found.");

            var genericMethod = isSerializableMethod.MakeGenericMethod(type);
            var result = genericMethod.Invoke(MessagePackCompatibilityChecker, null);

            if (result is Result methodResult) return methodResult;

            return Result.Failure("Compatibility check did not return a valid result.");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Exception during compatibility check: {ex.Message}");
        }
    }
}
