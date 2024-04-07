using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Interfaces;

public interface IFileWriter
{
    IFileWriter WithJsonSerialization();
    IFileWriter WithMessagePackSerialization();
    Task<Result> WriteFileAsync(DropBearFile file, string filePath);
    Task<Result<byte[]>> WriteFileToByteArrayAsync(DropBearFile file);
    Task<Result> WriteByteArrayToFileAsync(byte[] bytes,string fileName, string filePath);
}
