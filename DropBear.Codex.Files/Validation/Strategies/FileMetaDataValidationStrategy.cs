using DropBear.Codex.Files.Models.FileComponents;
using DropBear.Codex.Validation.ReturnTypes;
using DropBear.Codex.Validation.StrategyValidation.Interfaces;

namespace DropBear.Codex.Files.Validation.Strategies;

public class FileMetaDataValidationStrategy : IValidationStrategy<FileMetaData>
{
    public ValidationResult Validate(FileMetaData context) => throw new NotImplementedException();
}
