using Kokuban;
using MessagePack;

namespace DropBear.Codex.Files.ConsoleTestApplication;

internal class Program
{
    private const string publicKeyPath = @"C:\Temp\publicKey.xml";
    private const string privateKeyPath = @"C:\Temp\privateKey.xml";

    private static async Task Main(string[] args)
    {
        Console.WriteLine(Chalk.Blue + "Starting test application");

        // Create a test file to work with
        var testFile = new TestFile("test.txt", DateTime.Now, "Hello, world!");

        // Attempt to 

        Console.WriteLine(Chalk.Blue + "End of test application");
    }

    public static void PrintByteArray(byte[] bytes)
    {
        const int bytesPerLine = 16;
        var bytesRemaining = bytes.Length;
        var bytesRead = 0;

        while (bytesRemaining > 0)
        {
            Console.Write("{0:X8}: ", bytesRead);

            var bytesToPrint = Math.Min(bytesPerLine, bytesRemaining);

            for (var i = 0; i < bytesToPrint; i++) Console.Write("{0:X2} ", bytes[bytesRead + i]);

            for (var i = bytesToPrint; i < bytesPerLine; i++) Console.Write("   ");

            Console.Write(" ");

            for (var i = 0; i < bytesToPrint; i++)
            {
                var b = bytes[bytesRead + i];
                var c = b < 32 || b > 126 ? '.' : (char)b;
                Console.Write(c);
            }

            Console.WriteLine();

            bytesRead += bytesToPrint;
            bytesRemaining -= bytesToPrint;
        }
    }
}

[MessagePackObject(true)]
public class TestFile
{
    public TestFile()
    {
    }

    public TestFile(string name, DateTime createdAt, string content)
    {
        Name = name;
        CreatedAt = createdAt;
        Content = content;
    }

    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Content { get; set; }
}