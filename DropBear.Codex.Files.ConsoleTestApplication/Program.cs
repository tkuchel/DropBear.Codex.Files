using DropBear.Codex.AppLogger.Extensions;
using DropBear.Codex.AppLogger.Interfaces;
using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.ConsoleTestApplication.Models;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models.FileComponents;
using DropBear.Codex.Files.Utils;
using DropBear.Codex.Serialization;
using DropBear.Codex.Serialization.Enums;
using DropBear.Codex.Serialization.Interfaces;
using DropBear.Codex.Utilities.Helpers;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Text;

namespace DropBear.Codex.Files.ConsoleTestApplication;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Create a new service collection
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        // Build the service provider
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Resolve the services that we need
        var logger = serviceProvider.GetRequiredService<IAppLogger<Program>>();
        var fileManager = serviceProvider.GetRequiredService<IFileManager>();
        var fileContentFactory = serviceProvider.GetRequiredService<IFileContentFactory>();
        var contentContainerFactory = serviceProvider.GetRequiredService<IContentContainerFactory>();
        var dataSerializer = serviceProvider.GetRequiredService<IDataSerializer>();

        logger.LogInformation($"Started at {DateTimeOffset.UtcNow}");

        // Write and read a file
        // await WriteFileAndReadFile(logger, fileManager, fileContentFactory, contentContainerFactory, dataSerializer);

        // Manual Testing
        //await DirectStreamInspectionTestAsync(dataSerializer, logger);

        // Call the new method to test filesystem interaction
        // await FileSystemInteractionTestAsync(dataSerializer, logger, @"C:\Temp\TestFileHeader.dbf");

        // Call the method to test length-prefixed encoding
        //await LengthPrefixEncodingTestAsync(logger);

        // Test all parts together
        //await FullCycleTestAsync(dataSerializer, logger, @"C:\Temp\FullCycleTest.dbf");

// Test FileHeader serialization
        await TestFileHeaderSerializationAsync(dataSerializer, logger);

        // Log that we are finished
        logger.LogInformation($"Finished at {DateTimeOffset.UtcNow}");

        // Dispose of the service provider
        serviceProvider.Dispose();
    }


    private static async Task TestFileHeaderSerializationAsync(IDataSerializer dataSerializer,
        IAppLogger<Program> logger)
    {
        // Step 1: Create an instance of the FileHeader to be tested
        var originalFileHeader = new FileHeader
        {
            // Initialize your FileHeader properties here, if any
            // For example: Version = "1.0.0"
        };

        logger.LogInformation("Original FileHeader created for testing.");

        // Step 2: Serialize the FileHeader object
        var serializeResult =
            await dataSerializer.SerializeMessagePackAsync(originalFileHeader, CompressionOption.None, true);
        if (serializeResult.IsFailure)
        {
            logger.LogError($"Serialization failed: {serializeResult.ErrorMessage}");
            return;
        }

        logger.LogInformation("FileHeader serialized successfully.");

        // Optional: Log the serialized byte array for debugging
        logger.LogInformation($"Serialized data: {BitConverter.ToString(serializeResult.Value)}");

        // Step 3: Deserialize the byte array back into a FileHeader object
        var deserializeResult =
            await dataSerializer.DeserializeMessagePackAsync<FileHeader>(serializeResult.Value, CompressionOption.None,
                true);
        if (deserializeResult.IsFailure)
        {
            logger.LogError($"Deserialization failed: {deserializeResult.ErrorMessage}");
            return;
        }

        var deserializedFileHeader = deserializeResult.Value;
        
        // Optional: Log the deserialized object for debugging
        logger.LogInformation($"Deserialized header version object: {deserializedFileHeader.Version.Dump()}");
        logger.LogInformation($"Original file header version object: {originalFileHeader.Version.Dump()}");

        // Step 4: Verify the deserialized object
        // This step depends on what properties your FileHeader class has.
        // For example, you might compare the Version property of the original and deserialized object:
        if (originalFileHeader.Version != deserializedFileHeader.Version)
        {
            logger.LogError("Verification failed: Deserialized object does not match the original.");
            return;
        }

        logger.LogInformation("Deserialization verified successfully. FileHeader object matches the original.");

        // Step 5: Conclude the test
        logger.LogInformation("Serialization and deserialization test of FileHeader completed successfully.");
    }

    private static async Task FullCycleTestAsync(IDataSerializer dataSerializer, IAppLogger<Program> logger,
        string filePath)
    {
        // Create a simple object for testing
        var fileHeader = new FileHeader();

        // Step 1: Serialize the object
        var serializeResult = await dataSerializer.SerializeMessagePackAsync(fileHeader, CompressionOption.None);
        if (serializeResult.IsFailure)
        {
            logger.LogError($"Serialization failed: {serializeResult.ErrorMessage}");
            return;
        }

        // Step 2: Write serialized data to a file with length prefix
        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            LengthPrefixUtils.WriteLengthPrefixedBytes(fileStream, serializeResult.Value);
        }

        // Step 3: Read the data back from the file
        byte[] fileData;
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            fileData = LengthPrefixUtils.ReadLengthPrefixedBytes(fileStream);
        }

        // Step 4: Deserialize the data back into an object
        var deserializeResult =
            await dataSerializer.DeserializeMessagePackAsync<FileHeader>(fileData, CompressionOption.None);
        if (deserializeResult.IsFailure)
        {
            logger.LogError($"Deserialization failed: {deserializeResult.ErrorMessage}");
            return;
        }

        // Verify the deserialized object
        // (Optional: Here you could compare some identifiable property of the original and deserialized object for equality)
        logger.LogInformation("Full cycle test succeeded.");
    }

    private static async Task LengthPrefixEncodingTestAsync(IAppLogger<Program> logger)
    {
        // Example byte array to simulate serialized data
        byte[] simulatedData = { 1, 2, 3, 4, 5 };

        // Use a MemoryStream for testing
        using var memoryStream = new MemoryStream();
        // Write the data to the stream with a length prefix
        LengthPrefixUtils.WriteLengthPrefixedBytes(memoryStream, simulatedData);

        // Reset the stream's position to the beginning for reading
        memoryStream.Position = 0;

        // Read the data back from the stream
        var readData = LengthPrefixUtils.ReadLengthPrefixedBytes(memoryStream);

        // Verify that the original data and read data are identical
        var dataMatches = simulatedData.Length == readData.Length &&
                          simulatedData.SequenceEqual(readData);

        if (dataMatches)
            logger.LogInformation("Length-prefixed encoding test succeeded.");
        else
            logger.LogError("Length-prefixed encoding test failed. The read data does not match the original data.");
    }

    private static async Task FileSystemInteractionTestAsync(IDataSerializer dataSerializer, IAppLogger<Program> logger,
        string filePath)
    {
        var fileHeader = new FileHeader(); // Example object to serialize
        var serializeResult = await dataSerializer.SerializeMessagePackAsync(fileHeader, CompressionOption.None);

        if (serializeResult.IsFailure)
        {
            logger.LogError($"Serialization failed: {serializeResult.ErrorMessage}");
            return;
        }

        // Write serialized data to a file
        await File.WriteAllBytesAsync(filePath, serializeResult.Value);

        // Read the serialized data back from the file
        var fileData = await File.ReadAllBytesAsync(filePath);

        // Attempt to deserialize the data read from the file
        var deserializeResult =
            await dataSerializer.DeserializeMessagePackAsync<FileHeader>(fileData, CompressionOption.None);

        if (deserializeResult.IsFailure)
        {
            logger.LogError($"Deserialization failed: {deserializeResult.ErrorMessage}");
            return;
        }

        logger.LogInformation("Filesystem interaction test succeeded.");
    }

    private static async Task DirectStreamInspectionTestAsync(IDataSerializer dataSerializer,
        IAppLogger<Program> logger)
    {
        // Create an instance of the object to be serialized
        var fileHeader = new FileHeader();

        // Serialize the object to a MemoryStream
        using (var memoryStream = new MemoryStream())
        {
            var serializeResult = await dataSerializer.SerializeMessagePackAsync(fileHeader, CompressionOption.None);
            if (serializeResult.IsFailure)
            {
                logger.LogError($"Serialization failed: {serializeResult.ErrorMessage}");
                return;
            }

            // Write the serialized data to the MemoryStream
            await memoryStream.WriteAsync(serializeResult.Value, 0, serializeResult.Value.Length);

            // Reset the stream's position to the beginning for reading
            memoryStream.Position = 0;

            // Deserialize the object from the MemoryStream
            var deserializeResult =
                await dataSerializer.DeserializeMessagePackAsync<FileHeader>(serializeResult.Value,
                    CompressionOption.None);

            if (deserializeResult.IsFailure)
            {
                logger.LogError($"Deserialization failed: {deserializeResult.ErrorMessage}");
                return;
            }

            logger.LogInformation("Direct stream inspection test succeeded.");
        }
    }

    private static async Task WriteFileAndReadFile(IAppLogger<Program> logger, IFileManager fileManager,
        IFileContentFactory fileContentFactory, IContentContainerFactory contentContainerFactory,
        IDataSerializer dataSerializer)
    {
        // Create some test data
        var testData = CreateTestData();
        logger.LogInformation($"Test data created: {testData}");


        // Write the test data to a file
        var writeResult = await WriteTestData(fileManager, fileContentFactory, contentContainerFactory, dataSerializer,
            testData, "TestAuthor", logger);
        if (writeResult.IsFailure)
        {
            logger.LogError($"Error writing file: {writeResult.ErrorMessage}");
            return;
        }

        logger.LogInformation($"Test data written to file: {writeResult}");

        // Read the test data from the file
        var readResult = await ReadTestData(fileManager, fileContentFactory, contentContainerFactory, dataSerializer,
            "TestAuthor", logger);
        if (readResult.IsFailure)
        {
            logger.LogError($"Error reading file: {readResult.ErrorMessage}");
            return;
        }

        logger.LogInformation($"Test data read from file: {readResult.Value}");
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddAppLogger();
        services.AddDropBearCodexFiles();
        services.AddDataSerializationServices();
    }

    private static TestData CreateTestData()
    {
        return new TestData
            { Id = 1, Name = "TestDataClass", Description = "This is a test data class for file testing" };
    }

    private static async Task<Result> WriteTestData(
        IFileManager fileManager,
        IFileContentFactory fileContentFactory,
        IContentContainerFactory contentContainerFactory,
        IDataSerializer dataSerializer,
        TestData testData,
        string author,
        IAppLogger<Program> logger)
    {
        var fileData =
            await dataSerializer.SerializeJsonAsync(testData, CompressionOption.Compressed, EncodingOption.Base64);
        if (fileData.IsFailure)
        {
            logger.LogError($"Serialization failure: {fileData.ErrorMessage}");
            return Result.Failure(fileData.ErrorMessage);
        }

        var fileBytes = fileData.Value.GetBytes();
        var contentContainer = contentContainerFactory.Create(fileBytes, typeof(TestData));
        var fileContent = fileContentFactory.Create();

        fileContent.ClearContents();
        fileContent.AddContent(contentContainer);

        var filePath = @"C:\Temp\TestData.dbf";
        var createFileResult = await fileManager.CreateFileAsync(author, fileContent, true);
        if (createFileResult.IsFailure)
        {
            logger.LogError($"Create file failure: {createFileResult.ErrorMessage}");
            return Result.Failure(createFileResult.ErrorMessage);
        }

        var serializeResult =
            await dataSerializer.SerializeMessagePackAsync(createFileResult.Value.Header, CompressionOption.Compressed);
        if (serializeResult.IsSuccess)
        {
            // Immediately attempt to deserialize to test serialization correctness
            var immediateDeserializeResult =
                await dataSerializer.DeserializeMessagePackAsync<FileHeader>(serializeResult.Value,
                    CompressionOption.Compressed);
            if (immediateDeserializeResult.IsFailure)
                logger.LogError("Immediate deserialization failed: " + immediateDeserializeResult.ErrorMessage);
            else
                logger.LogInformation("Immediate deserialization succeeded.");
        }

        var writeFileResult = await fileManager.WriteFileAsync(createFileResult.Value, filePath);
        if (writeFileResult.IsFailure)
        {
            logger.LogError($"Write to disk failure: {writeFileResult.ErrorMessage}");
            return Result.Failure(writeFileResult.ErrorMessage);
        }

        logger.LogInformation($"File successfully written to disk at {filePath}.");
        return Result.Success();
    }

    private static async Task<Result<TestData>> ReadTestData(
        IFileManager fileManager,
        IFileContentFactory fileContentFactory,
        IContentContainerFactory contentContainerFactory,
        IDataSerializer dataSerializer,
        string author,
        IAppLogger<Program> logger)
    {
        var filePath = @"C:\Temp\TestData.dbf";
        var readResult = await fileManager.ReadFileAsync(filePath);

        if (readResult.IsFailure)
        {
            logger.LogError($"Read file failure: {readResult.ErrorMessage}");
            return Result<TestData>.Failure(readResult.ErrorMessage);
        }

        var readContent = readResult.Value;
        var readBytes = readContent.GetRawContent(nameof(TestData));
        if (readBytes == null)
        {
            logger.LogError("Read bytes are null.");
            return Result<TestData>.Failure("Read bytes are null.");
        }

        var deserializedData = await dataSerializer.DeserializeJsonAsync<TestData>(Convert.ToBase64String(readBytes),
            CompressionOption.Compressed, EncodingOption.Base64);
        if (deserializedData.IsFailure)
        {
            logger.LogError($"Deserialization failure: {deserializedData.ErrorMessage}");
            return Result<TestData>.Failure(deserializedData.ErrorMessage);
        }

        logger.LogInformation($"Data successfully read and deserialized from file at {filePath}.");
        return Result<TestData>.Success(deserializedData.Value);
    }
}