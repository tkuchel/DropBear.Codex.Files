using DropBear.Codex.AppLogger.Interfaces;
using DropBear.Codex.Core.ReturnTypes;
using DropBear.Codex.Files.Interfaces;
using DropBear.Codex.Files.Models;
using DropBear.Codex.Files.Models.ContentContainers;
using DropBear.Codex.Files.Models.FileComponents.SubComponents;

namespace DropBear.Codex.Files.Factory.Implementations;

public class FileCreator : IFileCreator
{
    private bool _useCompression;
    private IAppLogger<FileCreator> _logger;

    public FileCreator( IAppLogger<FileCreator> logger)
    {
        _logger = logger;
    }
    
    public IFileCreator WithCompression(bool compress)
    {
        _useCompression = compress;
        return this;
    }

    public async Task<Result<DropBearFile>> CreateAsync<T>(string name, T content, bool compress = false,
        bool forceCreation = false) where T : class
    {
        // Validate the name
        if(string.IsNullOrWhiteSpace(name))
            return Result<DropBearFile>.Failure("Name cannot be null or empty");
        
        // Remove file extension if present
        name = Path.GetFileNameWithoutExtension(name);

        // Create the container, validate and create the DropBearFile
        try
        {
            // Create a content container based off the name,content,compression and type
            var contentContainer = CreateContentContainer(name, content, compress, typeof(T));
            
            // Validate the content container
            if (contentContainer == null)
                return Result<DropBearFile>.Failure("Error creating file");
            

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating file");
            return Result<DropBearFile>.Failure("Error creating file");
        }
    }

    private static IContentContainer CreateContentContainer<T>(string name, T content, bool compress, Type type) where T : class
    {
        // switch based on the type of content
        return type switch
        {
            { } t when t == typeof(string) => new StringContentContainer(name, content as string, compress),
            { } t when t == typeof(byte[]) => new ByteContentContainer(name, content as byte[], compress),
            { } t when t == typeof(Stream) => new StreamContentContainer(name, content as Stream, compress),
            _ => null
        };
    }
}
