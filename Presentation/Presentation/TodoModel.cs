using System.Text.Json.Serialization;

namespace CES_TEST;

public record TodoModel : IComparable<TodoModel>
{
    public int Id { get; set; }
    
    public string Title { get; set; }
    
    [JsonPropertyName("completed")]
    public bool IsCompleted { get; set; }

    public int CompareTo(TodoModel other)
    {
        return Id.CompareTo(other.Id);
    }
}