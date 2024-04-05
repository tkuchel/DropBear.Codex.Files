using DropBear.Codex.AppLogger.Builders;
using DropBear.Codex.AppLogger.Interfaces;
using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Files.Models.ContentContainers;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace DropBear.Codex.Files.Factory.Implementations;

public sealed class FileCreator : IFileCreator, IDisposable
{
    private static RecyclableMemoryStreamManager? s_streamManager;
    private readonly ILogger<FileCreator> _logger;
    private readonly ILoggingFactory _loggerFactory;
    private bool _disposed;
    private bool _useCompression;

    public FileCreator(RecyclableMemoryStreamManager? streamManager)
    {
        s_streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));

        _loggerFactory = new LoggerConfigurationBuilder()
            .SetLogLevel(LogLevel.Information)
            .EnableConsoleOutput()
            .UseJsonFormatter() // Assuming you want JSON formatted logs
            .ConfigureRollingFile("logs/", 1024) // Configure rolling file and size
            .Build();

        // Create a logger instance for MyClass
        _logger = _loggerFactory.CreateLogger<FileCreator>();
    }
    
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public IFileCreator WithCompression(bool compress)
    {
        _useCompression = compress;
        return this;
    }

    public Task<Result<DropBearFile>> CreateAsync<T>(string name, T content, bool compress = false,
        bool forceCreation = false) where T : class
    {
        // Validate the name
        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult(Result<DropBearFile>.Failure("Name cannot be null or empty"));

        // Remove file extension if present
        name = Path.GetFileNameWithoutExtension(name);

        // Create the container, validate and create the DropBearFile
        try
        {
            // Create a content container based off the name,content,compression and type
            var contentContainer = CreateContentContainer(name, content, compress, typeof(T));

            // Validate the content container
            if (contentContainer is null)
            {
                _logger.LogError($"Error creating content container for {0}-{1}", name, typeof(T));
                return Task.FromResult(Result<DropBearFile>.Failure("Error creating content container"));
            }

            // Create the DropBearFile
            var dropBearFile = new DropBearFile(name, Environment.UserName, _useCompression);
            
            // Add the content container to the DropBearFile
            dropBearFile.AddContent(contentContainer);
            
            return Task.FromResult(Result<DropBearFile>.Success(dropBearFile));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating file");
            return Task.FromResult(Result<DropBearFile>.Failure("Error creating file"));
        }
    }

    private static IContentContainer? CreateContentContainer<T>(string name, T content, bool compress, Type type)
        where T : class =>
        // switch based on the type of content
        type switch
        {
            { } t when t == typeof(string) => new StringContentContainer(s_streamManager, name, content as string,
                compress),
            { } t when t == typeof(byte[]) => new ByteContentContainer(s_streamManager, name, content as byte[],
                compress),
            { } t when t == typeof(Stream) => new StreamContentContainer(s_streamManager, name, content as Stream,
                compress),
            _ => null
        };

    // Protected implementation of Dispose pattern.
    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
            // Dispose managed state (managed objects).
            if (_loggerFactory is IDisposable disposable)
                disposable.Dispose();

        _disposed = true;
    }
}
