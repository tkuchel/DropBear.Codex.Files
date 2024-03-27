using DropBear.Codex.Files.Models.FileComponents;
using DropBear.Codex.Validation.ReturnTypes;
using DropBear.Codex.Validation.StrategyValidation.Interfaces;

namespace DropBear.Codex.Files.Validation.Strategies;

public class CompressionSettingsValidationStrategy : IValidationStrategy<CompressionSettings>
{
    public ValidationResult Validate(CompressionSettings context) => throw new NotImplementedException();
}
