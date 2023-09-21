namespace CES_TEST;

public interface ITodoApiService
{
    Task<List<TodoModel>> GetTodos();
    
    Task SaveAsync(List<TodoModel> todos);
}