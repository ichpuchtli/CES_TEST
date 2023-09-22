namespace CES_TEST;

public interface ITodoRepository
{
    Task<List<TodoModel>> GetStoredTodos();
    
    Task MergeWithServerTodos(List<TodoModel> todos);
    
    Task AddLocalTodo(List<TodoModel> todos);
    
    Task UpdateLocalTodo(TodoModel item);
    
    Task<List<TodoModel>> GetPendingTodos();
    
    Task MarkAsSynchronised(List<TodoModel> pending);
}