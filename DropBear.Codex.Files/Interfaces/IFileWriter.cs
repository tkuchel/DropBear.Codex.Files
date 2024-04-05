using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Interfaces;

public interface IFileWriter
{
    IFileWriter WithJsonSerialization(bool serializeToJson);
    IFileWriter WithMessagePackSerialization(bool serializeToMessagePack);
    Task<Result> WriteFileAsync(DropBearFile file, string filePath);
}
