using DropBear.Codex.Files.Models;
using DropBear.Codex.Validation.ReturnTypes;
using DropBear.Codex.Validation.StrategyValidation.Interfaces;

namespace DropBear.Codex.Files.Validation.Strategies;

/// <summary>
///     Provides validation for <see cref="DropBearFile" /> instances.
/// </summary>
public class DropBearFileValidationStrategy : IValidationStrategy<DropBearFile>
{
    /// <summary>
    ///     Validates the given <see cref="DropBearFile" /> object.
    /// </summary>
    /// <param name="context">The <see cref="DropBearFile" /> instance to validate.</param>
    /// <returns>A <see cref="ValidationResult" /> indicating success or failure, with details of any validation errors.</returns>
    public ValidationResult Validate(DropBearFile context)
    {
        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            // Perform validation logic here

            // If validation succeeds, return a success result
            return ValidationResult.Success();
        }
        catch (Exception e)
        {
            // If an exception occurs during validation, add it to the errors dictionary and return a failure result
            errors.Add("TryCatchException", e.Message);
            return ValidationResult.Fail(errors);
        }
    }
}
