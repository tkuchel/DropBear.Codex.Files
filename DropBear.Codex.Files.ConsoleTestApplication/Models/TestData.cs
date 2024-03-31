using MessagePack;

namespace DropBear.Codex.Files.ConsoleTestApplication.Models;

[MessagePackObject]
public class TestData
{
    [SerializationConstructor]
    public TestData()
    {
        
    }
    [Key(0)]
    public int Id { get; set; }
    [Key(1)]
    public string Name { get; set; } = string.Empty;
    [Key(2)]
    public string Description { get; set; } = string.Empty;
}