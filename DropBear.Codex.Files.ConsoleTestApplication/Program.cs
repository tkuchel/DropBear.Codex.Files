using DropBear.Codex.Files.Factory;
using DropBear.Codex.Utilities.Helpers;
using Kokuban;
using Newtonsoft.Json;

namespace DropBear.Codex.Files.ConsoleTestApplication;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine(Chalk.Blue + "Starting test application");

        var fileCreator = FileManagerFactory.FileCreator();
        var testFile = new TestFile("TestFile", DateTime.UtcNow, "This is a test file");

        // Convert the test file to a byte array
        var testFileAsBytes = JsonConvert.SerializeObject(testFile).GetBytes();

        var file = await fileCreator.WithCompression().CreateAsync(testFile.Name, testFileAsBytes);

        if (file.IsSuccess) Console.WriteLine(Chalk.Green + $"File created: {file.Value.Metadata.FileName}");

        var fileWriter = FileManagerFactory.FileWriter();
        var tempFilePath = @"C:\Temp";
        var writeResult = await fileWriter.WriteFileAsync(file.Value, tempFilePath);

        if (writeResult.IsSuccess)
            Console.WriteLine(Chalk.Green + $"File written: {file.Value.Metadata.FileName} at {tempFilePath}");

        var fileReader = FileManagerFactory.FileReader();
        var fullFilePathAndName = Path.Combine(tempFilePath, file.Value.GetFileNameWithExtension());
        var readResult = await fileReader.ReadFileAsync(fullFilePathAndName);

        if (readResult.IsSuccess)
            Console.WriteLine(Chalk.Green +
                              $"File read: {readResult.Value.Metadata.FileName} at {fullFilePathAndName}");

        Console.WriteLine(Chalk.Blue + "End of test application");
    }

    private record TestFile(string Name, DateTime CreatedAt, string Content);
}