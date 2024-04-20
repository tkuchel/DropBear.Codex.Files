using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using DropBear.Codex.Files.Extensions;
using DropBear.Codex.Files.Models;
using FluentStorage;
using FluentStorage.Blobs;
using Microsoft.IO;

namespace DropBear.Codex.Files.Services;

[SupportedOSPlatform("windows")]
public class FileManager
{
    private readonly RecyclableMemoryStreamManager _memoryStreamManager = new();
    private string? _accountKey;
    private string? _accountName;
    private IBlobStorage? _azureBlobStorage;
    private string? _localPath;
    private bool _enableBlobStorage;
    
    public FileManager ConfigureLocalPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Local path cannot be null or empty.", nameof(path));

        if (!Directory.Exists(path))
            try
            {
                Directory.CreateDirectory(path);
                Console.WriteLine("Directory created successfully.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create directory at {path}.", ex);
            }

        if (!HasWritePermissionOnDir(path))
        {
            Console.WriteLine("Attempting to add write permissions to the directory.");
            // Assuming you want to give the current user write access
            var currentUser = WindowsIdentity.GetCurrent().Name;
            AddDirectorySecurity(path, currentUser, FileSystemRights.WriteData, AccessControlType.Allow);
            if (!HasWritePermissionOnDir(path))
                throw new UnauthorizedAccessException("Failed to set necessary write permissions.");
        }

        _localPath = path;
        return this;
    }

    private static bool HasWritePermissionOnDir(string path)
    {
        var dInfo = new DirectoryInfo(path);
        var dSecurity = dInfo.GetAccessControl();
        var rules = dSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));

        foreach (FileSystemAccessRule rule in rules)
            if ((rule.FileSystemRights & FileSystemRights.WriteData) != 0)
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

    public FileManager ConfigureBlobStorage(string accountName, string accountKey)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name cannot be null or empty.", nameof(accountName));
        if (string.IsNullOrWhiteSpace(accountKey))
            throw new ArgumentException("Account key cannot be null or empty.", nameof(accountKey));

        _accountName = accountName;
        _accountKey = accountKey;
        _enableBlobStorage = true;
        return this;
    }

    public FileManager Build()
    {
        if (_enableBlobStorage)
        {
            if (string.IsNullOrEmpty(_accountName) || string.IsNullOrEmpty(_accountKey))
                throw new InvalidOperationException("Account name and key must be configured for blob storage.");

            _azureBlobStorage = StorageFactory.Blobs.AzureBlobStorageWithSharedKey( _accountName, _accountKey);
        }
        return this;
    }

    public async Task WriteToFileAsync(DropBearFile file, string path)
    {
        if (string.IsNullOrEmpty(_localPath))
            throw new InvalidOperationException("Local path is not configured.");

        var filePath = Path.Combine(_localPath, path);
        var stream = file.ToStream();

        var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await using (fileStream.ConfigureAwait(false))
        {
            await stream.CopyToAsync(fileStream).ConfigureAwait(false);
        }
    }

    public async Task<DropBearFile> ReadFromFileAsync(string path)
    {
        if (string.IsNullOrEmpty(_localPath))
            throw new InvalidOperationException("Local path is not configured.");

        var filePath = Path.Combine(_localPath, path);

        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        await using (fileStream.ConfigureAwait(false))
        {
            return DropBearFileExtensions.FromStream(fileStream);
        }
    }

    public async Task WriteToBlobAsync(DropBearFile file, string blobName)
    {
        if (_azureBlobStorage is null)
            throw new InvalidOperationException("Blob storage is not configured.");

        var stream = file.ToStream();
        await using (stream.ConfigureAwait(false))
        {
            await _azureBlobStorage.WriteAsync(blobName, stream).ConfigureAwait(false);
        }
    }

    public async Task<DropBearFile> ReadFromBlobAsync(string blobName)
    {
        if (_azureBlobStorage is null)
            throw new InvalidOperationException("Blob storage is not configured.");

        await using var stream = _memoryStreamManager.GetStream();
        await using var blobStream = await _azureBlobStorage.OpenReadAsync(blobName);
        await blobStream.CopyToAsync(stream).ConfigureAwait(false);

        stream.Position = 0; // Reset stream position to read from the beginning
        return DropBearFileExtensions.FromStream(stream);
    }

    public async Task UpdateFileAsync(DropBearFile file, string path, string versionLabel)
    {
        var currentFilePath = Path.Combine(_localPath ?? string.Empty, path);
        var newFilePath = currentFilePath + ".new";
        var deltaFilePath = currentFilePath + ".delta";
        var signatureFilePath = currentFilePath + ".sig";

        // Save new version temporarily
        await WriteToFileAsync(file, newFilePath).ConfigureAwait(false);

        // Create a new FileVersion object
        var fileVersion = new FileVersion(versionLabel, DateTime.UtcNow, currentFilePath);

        // Generate delta
        fileVersion.CreateDelta(currentFilePath, newFilePath);

        // Apply delta to create updated file
        fileVersion.ApplyDelta(currentFilePath, currentFilePath);

        // Cleanup: Delete temporary and delta files
        File.Delete(newFilePath);
        File.Delete(deltaFilePath);
        File.Delete(signatureFilePath);
    }


    public async Task DeleteFileAsync(string path)
    {
        if (string.IsNullOrEmpty(_localPath))
            throw new InvalidOperationException("Local path is not configured.");

        var filePath = Path.Combine(_localPath, path);
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public async Task UpdateBlobAsync(DropBearFile file, string blobName, string versionLabel)
    {
        var tempLocalPath = Path.GetTempFileName();
        var newLocalPath = tempLocalPath + ".new";
        var deltaFilePath = tempLocalPath + ".delta";
        var signatureFilePath = tempLocalPath + ".sig";

        // Download current blob to a local file
        await DownloadBlobToLocalAsync(blobName, tempLocalPath).ConfigureAwait(false);

        // Save new version temporarily locally
        var newFileStream = file.ToStream();
        await using (newFileStream.ConfigureAwait(false))
        {
            await newFileStream.CopyToFileAsync(newLocalPath).ConfigureAwait(false);
        }

        // Create a new FileVersion object, generate delta, apply it
        var fileVersion = new FileVersion(versionLabel, DateTime.UtcNow, tempLocalPath);
        fileVersion.CreateDelta(tempLocalPath, newLocalPath);
        fileVersion.ApplyDelta(tempLocalPath, tempLocalPath);

        // Upload updated file back to blob
        await UploadFileToBlobAsync(tempLocalPath, blobName).ConfigureAwait(false);

        // Cleanup: Delete temporary files
        File.Delete(tempLocalPath);
        File.Delete(newLocalPath);
        File.Delete(deltaFilePath);
        File.Delete(signatureFilePath);
    }


    public async Task DeleteBlobAsync(string blobName)
    {
        if (_azureBlobStorage is null)
            throw new InvalidOperationException("Blob storage is not configured.");

        await _azureBlobStorage.DeleteAsync(blobName).ConfigureAwait(false);
    }

    private async Task DownloadBlobToLocalAsync(string blobName, string localPath)
    {
        if (_azureBlobStorage is null)
            throw new InvalidOperationException("Blob storage is not configured.");

        await using var blobStream = await _azureBlobStorage.OpenReadAsync(blobName);
        await using var localFileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write);
        await blobStream.CopyToAsync(localFileStream).ConfigureAwait(false);
    }

    public async Task UploadFileToBlobAsync(string localFilePath, string blobName)
    {
        if (_azureBlobStorage is null)
            throw new InvalidOperationException("Blob storage is not configured.");

        if (string.IsNullOrEmpty(localFilePath))
            throw new ArgumentException("Local file path cannot be null or empty.", nameof(localFilePath));

        if (string.IsNullOrEmpty(blobName))
            throw new ArgumentException("Blob name cannot be null or empty.", nameof(blobName));

        // Ensure the file exists before trying to open it
        if (!File.Exists(localFilePath))
            throw new FileNotFoundException("The specified file does not exist.", localFilePath);

        // Open the local file for reading
        var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);

        // Open the local file for reading
        await using (fileStream.ConfigureAwait(false))
        {
            // Upload the file stream to Azure Blob Storage
            await _azureBlobStorage.WriteAsync(blobName, fileStream).ConfigureAwait(false);
        }
    }
}
