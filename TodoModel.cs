using System.Text.Json.Serialization;

namespace CES_TEST;

public class TodoModel
{
    public string Title { get; set; }
    
    [JsonPropertyName("completed")]
    public bool IsCompleted { get; set; }
}