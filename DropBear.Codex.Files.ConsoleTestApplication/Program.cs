using Kokuban;

namespace DropBear.Codex.Files.ConsoleTestApplication;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine(Chalk.Blue + "Starting test application");


        Console.WriteLine(Chalk.Blue + "End of test application");
    }

    private record TestFile(string Name, DateTime CreatedAt, string Content);
}