using DropBear.Codex.Files.Models.FileComponents;
using DropBear.Codex.Validation.ReturnTypes;
using DropBear.Codex.Validation.StrategyValidation.Interfaces;

namespace DropBear.Codex.Files.Validation.Strategies;

public class FileHeaderValidationStrategy : IValidationStrategy<FileHeader>
{
    public ValidationResult Validate(FileHeader context) => throw new NotImplementedException();
}
