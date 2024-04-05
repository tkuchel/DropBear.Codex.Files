using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Interfaces;

public interface IFileReader
{
    IFileReader WithJsonSerialization(bool fileDatWasSerializedToJson);
    IFileReader WithMessagePackSerialization(bool fileDataWasSerializedToMessagePack);
    Task<Result<DropBearFile>> ReadFileAsync(string filePath, CancellationToken cancellationToken = default);
}
