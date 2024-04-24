using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using DropBear.Codex.Files.Enums;
using DropBear.Codex.Files.Extensions;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Files.StorageManagers;

namespace DropBear.Codex.Files.Services;

[SupportedOSPlatform("windows")]
public class FileManager
{
    private readonly BlobStorageManager? _blobStorageManager;
    private readonly bool _isWindows = OperatingSystem.IsWindows();
    private readonly LocalStorageManager? _localStorageManager;
    private readonly StorageStrategy _storageStrategy;

    internal FileManager(StorageStrategy storageStrategy,
        LocalStorageManager? localStorageManager = null, BlobStorageManager? blobStorageManager = null)
    {
        if (!_isWindows)
            throw new PlatformNotSupportedException("FileManager is only supported on Windows.");

        _blobStorageManager = blobStorageManager;
        _localStorageManager = localStorageManager;
        _storageStrategy = storageStrategy;
    }

    #region Public Methods

    public async Task WriteToFileAsync(DropBearFile file, string fullPath)
    {
        ValidateFilePath(fullPath);
        var stream = await file.ToStreamAsync().ConfigureAwait(false);

        switch (_storageStrategy)
        {
            case StorageStrategy.BlobOnly:
                await WriteToBlobStorage(fullPath, stream).ConfigureAwait(false);
                break;
            case StorageStrategy.LocalOnly:
                await WriteToLocalStorage(fullPath, stream).ConfigureAwait(false);
                break;
            case StorageStrategy.Both:
                await WriteToBlobStorage(fullPath, stream).ConfigureAwait(false);
                stream.Position = 0;  // Reset position before next write
                await WriteToLocalStorage(fullPath, stream).ConfigureAwait(false);
                break;
        }
    }

    public async Task<DropBearFile> ReadFromFileAsync(string fullPath)
    {
        ValidateFilePath(fullPath);
        Stream? stream = null;
        stream = _storageStrategy switch
        {
            StorageStrategy.BlobOnly => await ReadFromBlobStorage(fullPath).ConfigureAwait(false),
            StorageStrategy.LocalOnly => await ReadFromLocalStorage(fullPath).ConfigureAwait(false),
            StorageStrategy.Both => await ReadFromBlobStorage(fullPath) // Default to blob if both
                .ConfigureAwait(false),
            _ => null
        };

        if (stream is null)
            throw new InvalidOperationException("Failed to read file.");

        return await DropBearFileExtensions.FromStreamAsync(stream).ConfigureAwait(false);
    }

    public async Task UpdateFileAsync(DropBearFile file, string fullPath)
    {
        ValidateFilePath(fullPath);
        var stream = await file.ToStreamAsync().ConfigureAwait(false);

        switch (_storageStrategy)
        {
            case StorageStrategy.BlobOnly:
                await UpdateBlobStorage(fullPath, stream).ConfigureAwait(false);
                break;
            case StorageStrategy.LocalOnly:
                await UpdateLocalStorage(fullPath, stream).ConfigureAwait(false);
                break;
            case StorageStrategy.Both:
                await Task.WhenAll(UpdateBlobStorage(fullPath, stream), UpdateLocalStorage(fullPath, stream))
                    .ConfigureAwait(false);
                break;
        }

    }
    
    public async Task DeleteFileAsync(string fullPath)
    {
        ValidateFilePath(fullPath);

        switch (_storageStrategy)
        {
            case StorageStrategy.BlobOnly:
                await DeleteFromBlobStorage(fullPath).ConfigureAwait(false);
                break;
            case StorageStrategy.LocalOnly:
                await DeleteFromLocalStorage(fullPath).ConfigureAwait(false);
                break;
            case StorageStrategy.Both:
                await Task.WhenAll(DeleteFromBlobStorage(fullPath), DeleteFromLocalStorage(fullPath))
                    .ConfigureAwait(false);
                break;
        }
    }

    #endregion


    #region Private Methods

    private static void ValidateFilePath(string fullPath)
    {
        var directoryPath = Path.GetDirectoryName(fullPath) ??
                            throw new InvalidOperationException("Invalid file path.");

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            Console.WriteLine("Directory created successfully.");
        }

        if (HasWritePermissionOnDir(directoryPath)) return;
        Console.WriteLine("Attempting to add write permissions to the directory.");
        var currentUser = WindowsIdentity.GetCurrent().Name;
        AddDirectorySecurity(directoryPath, currentUser, FileSystemRights.WriteData, AccessControlType.Allow);
        if (!HasWritePermissionOnDir(directoryPath))
            throw new UnauthorizedAccessException("Failed to set necessary write permissions.");
    }

    private static bool HasWritePermissionOnDir(string path)
    {
        var dInfo = new DirectoryInfo(path);
        var dSecurity = dInfo.GetAccessControl();
        var rules = dSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
        foreach (FileSystemAccessRule rule in rules)
            if ((rule.FileSystemRights & FileSystemRights.WriteData) is not FileSystemRights.WriteData)
                if (rule.AccessControlType is AccessControlType.Allow)
                    return true;
        return false;
    }

    private static void AddDirectorySecurity(string path, string account, FileSystemRights rights,
        AccessControlType controlType)
    {
        var dInfo = new DirectoryInfo(path);
        var dSecurity = dInfo.GetAccessControl();
        dSecurity.AddAccessRule(new FileSystemAccessRule(account, rights,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, controlType));
        dInfo.SetAccessControl(dSecurity);
    }

    #endregion

    #region Storage Helper Methods

    // Helper methods for each storage operation, each encapsulating the necessary logic for that storage type
    private async Task WriteToBlobStorage(string fullPath, Stream stream)
    {
        var containerName = Path.GetDirectoryName(fullPath)?.Replace(@"\", "/", StringComparison.OrdinalIgnoreCase);
        var blobName = Path.GetFileName(fullPath);
        if (_blobStorageManager is not null)
            if (containerName is not null)
                await _blobStorageManager.WriteAsync(blobName, stream, containerName).ConfigureAwait(false);
    }

    private async Task WriteToLocalStorage(string fullPath, Stream stream)
    {
        if (_localStorageManager is not null)
            await _localStorageManager.WriteAsync(Path.GetFileName(fullPath), stream, Path.GetDirectoryName(fullPath))
                .ConfigureAwait(false);
    }

    private async Task<Stream> ReadFromBlobStorage(string fullPath)
    {
        var containerName = Path.GetDirectoryName(fullPath)?.Replace(@"\", "/", StringComparison.OrdinalIgnoreCase);
        var blobName = Path.GetFileName(fullPath);
        if (_blobStorageManager is null) throw new InvalidOperationException("Blob storage manager is not set.");
        if (containerName is not null)
            return await _blobStorageManager.ReadAsync(blobName, containerName).ConfigureAwait(false);

        throw new InvalidOperationException("Container name is null.");
    }

    private async Task<Stream> ReadFromLocalStorage(string fullPath)
    {
        if (_localStorageManager is null) throw new InvalidOperationException("Local storage manager is not set.");
        return await _localStorageManager.ReadAsync(Path.GetFileName(fullPath), Path.GetDirectoryName(fullPath))
            .ConfigureAwait(false);
    }

    private async Task UpdateBlobStorage(string fullPath, Stream stream)
    {
        var containerName = Path.GetDirectoryName(fullPath)?.Replace(@"\", "/", StringComparison.OrdinalIgnoreCase);
        var blobName = Path.GetFileName(fullPath);
        if (_blobStorageManager is not null)
            if (containerName is not null)
                await _blobStorageManager.UpdateBlobAsync(blobName, stream, containerName).ConfigureAwait(false);
    }

    private async Task UpdateLocalStorage(string fullPath, Stream stream)
    {
        if (_localStorageManager is not null)
            await _localStorageManager.UpdateAsync(Path.GetFileName(fullPath), stream, Path.GetDirectoryName(fullPath))
                .ConfigureAwait(false);
    }
    

    private async Task DeleteFromBlobStorage(string fullPath)
    {
        var containerName = Path.GetDirectoryName(fullPath)?.Replace(@"\", "/", StringComparison.OrdinalIgnoreCase);
        var blobName = Path.GetFileName(fullPath);
        if (_blobStorageManager is not null)
            if (containerName is not null)
                await _blobStorageManager.DeleteAsync(blobName, containerName).ConfigureAwait(false);
    }

    private async Task DeleteFromLocalStorage(string fullPath)
    {
        if (_localStorageManager is not null)
            await _localStorageManager.DeleteAsync(Path.GetFileName(fullPath), Path.GetDirectoryName(fullPath))
                .ConfigureAwait(false);
    }

    #endregion
}
