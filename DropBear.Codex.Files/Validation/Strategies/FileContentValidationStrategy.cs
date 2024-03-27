using DropBear.Codex.Files.Models.FileComponents;
using DropBear.Codex.Validation.ReturnTypes;
using DropBear.Codex.Validation.StrategyValidation.Interfaces;

namespace DropBear.Codex.Files.Validation.Strategies;

public class FileContentValidationStrategy : IValidationStrategy<FileContent>
{
    public ValidationResult Validate(FileContent context) => throw new NotImplementedException();
}
