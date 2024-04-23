using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using DropBear.Codex.Files.Extensions;
using DropBear.Codex.Files.Models;
using FluentStorage.Blobs;
using Microsoft.IO;

namespace DropBear.Codex.Files.Services;

[SupportedOSPlatform("windows")]
public class FileManager
{
    private readonly IBlobStorage? _blobStorage;
    private readonly bool _isWindows = OperatingSystem.IsWindows();
    private readonly RecyclableMemoryStreamManager _memoryStreamManager;

    internal FileManager(RecyclableMemoryStreamManager memoryStreamManager, IBlobStorage? blobStorage = null)
    {
        if (!_isWindows)
            throw new PlatformNotSupportedException("FileManager is only supported on Windows.");

        _memoryStreamManager = memoryStreamManager;
        _blobStorage = blobStorage;
    }

    private bool UseBlobStorage => _blobStorage is not null;

    public static async Task WriteToFileAsync(DropBearFile file)
    {
        ValidateFilePath(file);

        var stream = await file.ToStreamAsync().ConfigureAwait(false);

        var fileStream = new FileStream(file.FullPath, FileMode.Create, FileAccess.Write);
        await using (fileStream.ConfigureAwait(false))
        {
            await stream.CopyToAsync(fileStream).ConfigureAwait(false);
        }
    }

    public static async Task<DropBearFile> ReadFromFileAsync(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
            throw new InvalidOperationException("Local path is not configured.");

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("The specified file does not exist.", fullPath);

        var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        await using (fileStream.ConfigureAwait(false))
        {
            return await DropBearFileExtensions.FromStreamAsync(fileStream).ConfigureAwait(false);
        }
    }

    public async Task WriteToBlobAsync(DropBearFile file, string blobName)
    {
        if (!UseBlobStorage)
            throw new InvalidOperationException("Blob storage is not enabled.");

        if (_blobStorage is null)
            throw new InvalidOperationException("Blob storage is not configured.");

        var stream = await file.ToStreamAsync().ConfigureAwait(false);
        using (stream)
        {
            await _blobStorage.WriteAsync(blobName, stream).ConfigureAwait(false);
        }
    }

    public async Task<DropBearFile> ReadFromBlobAsync(string blobName)
    {
        if (!UseBlobStorage)
            throw new InvalidOperationException("Blob storage is not enabled.");

        if (_blobStorage is null)
            throw new InvalidOperationException("Blob storage is not configured.");

        var stream = _memoryStreamManager.GetStream();
        await using (stream.ConfigureAwait(false))
        {
            var blobStream = await _blobStorage.OpenReadAsync(blobName).ConfigureAwait(false);
            await using (blobStream.ConfigureAwait(false))
            {
                await blobStream.CopyToAsync(stream).ConfigureAwait(false);
                stream.Position = 0; // Reset stream position to read from the beginning
                return await DropBearFileExtensions.FromStreamAsync(stream).ConfigureAwait(false);
            }
        }
    }

    public static Task DeleteFileAsync(DropBearFile file)
    {
        if (string.IsNullOrEmpty(file.FullPath))
            throw new InvalidOperationException("Local path is not configured.");

        if (File.Exists(file.FullPath))
            File.Delete(file.FullPath);

        return Task.CompletedTask;
    }

    public async Task UpdateBlobAsync(DropBearFile file, string blobName, string versionLabel)
    {
        if (!UseBlobStorage)
            throw new InvalidOperationException("Blob storage is not enabled.");

        if (_blobStorage is null)
            throw new InvalidOperationException("Blob storage is not configured.");

        var tempLocalPath = Path.GetTempFileName();
        var newLocalPath = tempLocalPath + ".new";
        var deltaFilePath = tempLocalPath + ".delta";
        var signatureFilePath = tempLocalPath + ".sig";

        // Download current blob to a local file
        await DownloadBlobToLocalAsync(blobName, tempLocalPath).ConfigureAwait(false);

        // Save new version temporarily locally
        var newFileStream = await file.ToStreamAsync().ConfigureAwait(false);
        await using (newFileStream.ConfigureAwait(false))
        {
#pragma warning disable MA0004
            await using var localFileStream = new FileStream(newLocalPath, FileMode.Create, FileAccess.Write);
#pragma warning restore MA0004
            await newFileStream.CopyToAsync(localFileStream).ConfigureAwait(false);
        }

        // Create a new FileVersion object, generate delta, apply it
        var fileVersion = file.CreateFileVersion(versionLabel, DateTimeOffset.UtcNow);
        fileVersion.CreateDelta();
        fileVersion.ApplyDelta();

        // Upload updated file back to blob
        await UploadFileToBlobAsync(newLocalPath, blobName).ConfigureAwait(false);

        // Cleanup: Delete temporary files
        File.Delete(tempLocalPath);
        File.Delete(newLocalPath);
        File.Delete(deltaFilePath);
        File.Delete(signatureFilePath);
    }

    public async Task DeleteBlobAsync(string blobName)
    {
        if (!UseBlobStorage)
            throw new InvalidOperationException("Blob storage is not enabled.");

        if (_blobStorage is null)
            throw new InvalidOperationException("Blob storage is not configured.");

        await _blobStorage.DeleteAsync(blobName).ConfigureAwait(false);
    }

    private async Task DownloadBlobToLocalAsync(string blobName, string localPath)
    {
        if (!UseBlobStorage)
            throw new InvalidOperationException("Blob storage is not enabled.");

        if (_blobStorage is null)
            throw new InvalidOperationException("Blob storage is not configured.");

        var blobStream = await _blobStorage.OpenReadAsync(blobName).ConfigureAwait(false);
        await using (blobStream.ConfigureAwait(false))
        {
            var localFileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write);
            await using (localFileStream.ConfigureAwait(false))
            {
                await blobStream.CopyToAsync(localFileStream).ConfigureAwait(false);
            }
        }
    }

    public async Task UploadFileToBlobAsync(string localFilePath, string blobName)
    {
        if (!UseBlobStorage)
            throw new InvalidOperationException("Blob storage is not enabled.");

        if (_blobStorage is null)
            throw new InvalidOperationException("Blob storage is not configured.");

        if (string.IsNullOrEmpty(localFilePath))
            throw new ArgumentException("Local file path cannot be null or empty.", nameof(localFilePath));

        if (string.IsNullOrEmpty(blobName))
            throw new ArgumentException("Blob name cannot be null or empty.", nameof(blobName));

        if (!File.Exists(localFilePath))
            throw new FileNotFoundException("The specified file does not exist.", localFilePath);

        var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
        await using (fileStream.ConfigureAwait(false))
        {
            await _blobStorage.WriteAsync(blobName, fileStream).ConfigureAwait(false);
        }
    }

    private static void ValidateFilePath(DropBearFile file)
    {
        var directoryPath = Path.GetDirectoryName(file.FullPath) ??
                            throw new InvalidOperationException("Invalid file path.");

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            Console.WriteLine("Directory created successfully.");
        }

        if (HasWritePermissionOnDir(directoryPath)) return;
#pragma warning disable CA1416
        Console.WriteLine("Attempting to add write permissions to the directory.");
        var currentUser = WindowsIdentity.GetCurrent().Name;
        AddDirectorySecurity(directoryPath, currentUser, FileSystemRights.WriteData, AccessControlType.Allow);
        if (!HasWritePermissionOnDir(directoryPath))
            throw new UnauthorizedAccessException("Failed to set necessary write permissions.");
#pragma warning restore CA1416
    }

    private static bool HasWritePermissionOnDir(string path)
    {
#pragma warning disable CA1416
        var dInfo = new DirectoryInfo(path);
        var dSecurity = dInfo.GetAccessControl();
        var rules = dSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
        foreach (FileSystemAccessRule rule in rules)
            if ((rule.FileSystemRights & FileSystemRights.WriteData) is not FileSystemRights.WriteData)
                if (rule.AccessControlType is AccessControlType.Allow)
                    return true;
#pragma warning restore CA1416
        return false;
    }

    private static void AddDirectorySecurity(string path, string account, FileSystemRights rights,
        AccessControlType controlType)
    {
#pragma warning disable CA1416
        var dInfo = new DirectoryInfo(path);
        var dSecurity = dInfo.GetAccessControl();
        dSecurity.AddAccessRule(new FileSystemAccessRule(account, rights,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, controlType));
        dInfo.SetAccessControl(dSecurity);
#pragma warning restore CA1416
    }
}
