using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using DropBear.Codex.Files.Extensions;
using DropBear.Codex.Files.Models;
using FluentStorage;
using FluentStorage.Blobs;
using Microsoft.IO;
using Newtonsoft.Json;

namespace DropBear.Codex.Files.Services;

[SupportedOSPlatform("windows")]
public class FileManager
{
    private readonly RecyclableMemoryStreamManager _memoryStreamManager = new();
    private string? _accountKey;
    private string? _accountName;
    private IBlobStorage? _azureBlobStorage;
    private bool _enableBlobStorage;

    private bool ValidateFilePath(DropBearFile file)
    {
        var directoryPath = Path.GetDirectoryName(file.FullPath) ??
                            throw new InvalidOperationException("Invalid file path.");

        if (!Directory.Exists(directoryPath))
            try
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine("Directory created successfully.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create directory at {directoryPath}.", ex);
            }

        if (!HasWritePermissionOnDir(directoryPath))
        {
            Console.WriteLine("Attempting to add write permissions to the directory.");
            var currentUser = WindowsIdentity.GetCurrent().Name;
            AddDirectorySecurity(directoryPath, currentUser, FileSystemRights.WriteData, AccessControlType.Allow);
            if (!HasWritePermissionOnDir(directoryPath))
                throw new UnauthorizedAccessException("Failed to set necessary write permissions.");
        }

        return true;
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
        if (!_enableBlobStorage) return this;
        if (string.IsNullOrEmpty(_accountName) || string.IsNullOrEmpty(_accountKey))
            throw new InvalidOperationException("Account name and key must be configured for blob storage.");

        _azureBlobStorage = StorageFactory.Blobs.AzureBlobStorageWithSharedKey(_accountName, _accountKey);
        return this;
    }

    public async Task WriteToFileAsync(DropBearFile file)
    {
        if (string.IsNullOrEmpty(file.FullPath))
            throw new InvalidOperationException("Local path is not configured.");

        if (!ValidateFilePath(file))
            throw new InvalidOperationException("Failed to validate file path.");

        var stream = file.ToStream();

        var fileStream = new FileStream(file.FullPath, FileMode.Create, FileAccess.Write);
        await using (fileStream.ConfigureAwait(false))
        {
            await stream.CopyToAsync(fileStream).ConfigureAwait(false);
        }
    }

    public async Task<DropBearFile> ReadFromFileAsync(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
            throw new InvalidOperationException("Local path is not configured.");
    
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("The specified file does not exist.", fullPath);
    
        var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
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

    public async Task UpdateFileAsync(DropBearFile file, string versionLabel)
    {
        if (!ValidateFilePath(file)) // This already checks if the file exists and path is correct
            throw new InvalidOperationException("Failed to validate file path.");

        var currentFilePath = file.FullPath;
        var newFilePath = currentFilePath + ".new";
        var deltaFilePath = currentFilePath + ".delta";
        var signatureFilePath = currentFilePath + ".sig";

        try
        {
            // Save new version temporarily
            await WriteToFileAsync(file).ConfigureAwait(false);

            // Create a new FileVersion object and perform delta operations
            var fileVersion = file.CreateFileVersion(versionLabel, DateTimeOffset.UtcNow);
            fileVersion.CreateDelta();
            fileVersion.ApplyDelta();

            // Optional: Rename new file to replace old file if all operations succeed
            File.Delete(currentFilePath);
            File.Move(newFilePath, currentFilePath);
        }
        catch (Exception ex)
        {
            // Handle exceptions, log them, and possibly rethrow or handle cleanup
            throw new InvalidOperationException("Failed to update the file.", ex);
        }
        finally
        {
            // Cleanup: Ensure temporary files are deleted
            if (File.Exists(newFilePath)) File.Delete(newFilePath);
            if (File.Exists(deltaFilePath)) File.Delete(deltaFilePath);
            if (File.Exists(signatureFilePath)) File.Delete(signatureFilePath);
        }
    }


    public async Task DeleteFileAsync(DropBearFile file)
    {
        if (string.IsNullOrEmpty(file.FullPath))
            throw new InvalidOperationException("Local path is not configured.");

        if (File.Exists(file.FullPath))
            File.Delete(file.FullPath);
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
        var fileVersion = file.CreateFileVersion(versionLabel, DateTimeOffset.UtcNow);
        fileVersion.CreateDelta();
        fileVersion.ApplyDelta();

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
