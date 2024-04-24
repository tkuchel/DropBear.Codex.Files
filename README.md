# DropBear.Codex.Files

DropBear.Codex.Files is a .NET library designed to enhance file management capabilities, tailored specifically for applications that require robust handling of file operations. This includes streamlined processes for reading, writing, and updating files both locally and in blob storage, with an emphasis on performance and flexibility.

## Features

- **Advanced File Operations**: Supports advanced file operations such as reading, writing, updating (with and without deltas), and deleting files across different storage strategies.
- **Storage Strategy Flexibility**: Incorporates a strategy pattern to allow operations to seamlessly switch between local file systems and blob storage, or use both concurrently.
- **Memory Efficiency**: Utilizes `RecyclableMemoryStreamManager` for improved memory management, reducing large object heap (LOH) fragmentation.
- **Extensible Architecture**: The use of factory and builder patterns facilitates easy customization and extension of storage manager functionalities.

## Getting Started

### Installation

To use DropBear.Codex.Files in your project, add the library via NuGet:

```shell
dotnet add package DropBear.Codex.Files
```

## Usage

Here is a quickstart guide on using the library to manage files effectively:

```csharp
// Initialize the FileManager with a specific storage strategy
var fileManager = new FileManagerBuilder()
    .WithMemoryStreamManager(new RecyclableMemoryStreamManager())
    .WithLocalStorage("C:\\Data")  // Optional: Configure local storage
    .WithBlobStorage("accountName", "accountKey", "containerName")  // Optional: Configure blob storage
    .SetStorageStrategy(StorageStrategy.Both)  // Choose storage strategy: BlobOnly, LocalOnly, or Both
    .Build();

// Use FileManager to write, read, update, and delete files
// Example code to write a file
fileManager.WriteToFileAsync(dropBearFile, "path/to/file").Wait();
```

## Contribution

Contributions are welcome! If you have suggestions or want to contribute code, please feel free to create issues or pull
requests on GitHub.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

---
