using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.Utils;

public static class FileUtility
{
    public static bool IsValidFileName(string? name) => !string.IsNullOrWhiteSpace(name);

    public static string GetFilePath(DropBearFile file, string filePath)
    {
        if (!IsValidFileName(file.Metadata.FileName))
            return string.Empty;

        filePath = SanitizeFilePath(filePath);
        EnsureDirectoryExists(filePath);

        return ConstructFilePath(file, filePath);
    }
    
    public static string GetFilePath(string fileName,  string filePath,string fileExtension = "dbf")
    {
        if (!IsValidFileName(fileName))
            return string.Empty;

        filePath = SanitizeFilePath(filePath);
        EnsureDirectoryExists(filePath);

        return ConstructFilePath(fileName, filePath,fileExtension);
    }

    private static string SanitizeFilePath(string filePath) =>
        Path.GetFullPath(filePath);

    private static void EnsureDirectoryExists(string filePath)
    {
        if (Directory.Exists(filePath)) return;
        Directory.CreateDirectory(filePath);
    }

    private static string ConstructFilePath(DropBearFile file, string filePath) =>
        Path.Combine(filePath, $"{file.Metadata.FileName}.{file.Header?.Signature.Extension}");
    
    private static string ConstructFilePath(string fileName, string filePath, string fileExtension) =>
        Path.Combine(filePath, $"{fileName}.{fileExtension}");
}
