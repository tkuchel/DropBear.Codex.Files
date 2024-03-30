using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.MessagePackChecker;
using DropBear.Codex.Serialization.Services;

namespace DropBear.Codex.Files.PreflightTasks;

public static class MessagePackCompatibilityAggregator
{
    private static readonly MessagePackCompatibilityChecker MessagePackCompatibilityChecker = new();

    public static MessagePackCompatibilityResults CheckTypes(IEnumerable<Type> types)
    {
        var results = new MessagePackCompatibilityResults();

        foreach (var type in types)
            try
            {
                // Use reflection to call the generic IsSerializable method
                var method =
                    typeof(MessagePackCompatibilityChecker).GetMethod(nameof(MessagePackCompatibilityChecker
                        .IsSerializable));
                var genericMethod = method?.MakeGenericMethod(type);
                dynamic result = genericMethod?.Invoke(MessagePackCompatibilityChecker, null) ??
                                 Result.Failure("Failed to invoke compatibility check.");

                if (result.IsSuccess)
                {
                    results.SuccessTypes.Add(type.Name);
                }
                else
                {
                    // Assuming IsSuccess is a property of the result, and result contains more detailed failure information
                    // Adjust this part based on the actual structure of your result object
                    string reason = result.ErrorMessage ?? "Failed compatibility check without specific reason.";
                    results.FailedTypes.Add(type.Name, reason);
                }
            }
            catch (Exception ex)
            {
                results.FailedTypes.Add(type.Name, ex.Message);
            }

        return results;
    }
}
