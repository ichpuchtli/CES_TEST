using System.Text.Json.Serialization;

namespace CES_TEST;

public record TodoModel
{
    public string Id { get; set; }
    
    public string Title { get; set; }
    
    [JsonPropertyName("completed")]
    public bool IsCompleted { get; set; }
}