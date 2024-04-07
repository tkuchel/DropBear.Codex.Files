using DropBear.Codex.Core.ReturnTypes;

namespace DropBear.Codex.Files.Interfaces;

public interface IFileDeltaUtility
{
    Task<Result<byte[]>> CalculateFileSignatureAsync(byte[]? basisFileData);

    Task<Result<byte[]>> CalculateDeltaBetweenBasisFileAndNewFileAsync(byte[]? signatureFileData,
        byte[]? newFileData);

    Task<Result<byte[]>> ApplyDeltaToBasisFileAsync(byte[]? basisFileData, byte[]? deltaFileData);
}
