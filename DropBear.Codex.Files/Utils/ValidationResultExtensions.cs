using DropBear.Codex.Validation.ReturnTypes;

namespace DropBear.Codex.Files.Utils;

public static class ValidationResultExtensions
{
    public static ValidationResult Combine(this ValidationResult first, ValidationResult second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        // Start with a successful ValidationResult instance
        var combinedResult = ValidationResult.Success();

        // Aggregate errors from the first ValidationResult
        foreach (var (parameter, errorMessage) in first.Errors)
        {
            combinedResult.AddError(parameter, errorMessage);
        }

        // Aggregate errors from the second ValidationResult
        foreach (var (parameter, errorMessage) in second.Errors)
        {
            // To avoid duplicating errors with the same parameter and message
            if (!combinedResult.HasErrorFor(parameter))
            {
                combinedResult.AddError(parameter, errorMessage);
            }
        }

        return combinedResult;
    }
}
