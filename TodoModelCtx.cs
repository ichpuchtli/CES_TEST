using SQLite;

namespace CES_TEST;

public class TodoModelCtx
{
    [PrimaryKey]
    public string Id { get; set; }
    
    public string Title { get; set; }
    
    public bool IsCompleted { get; set; }
    
    public bool IsPendingSync { get; set; }
}