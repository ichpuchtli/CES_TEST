namespace CES_TEST;

public interface ITodoService
{
    /// <summary>
    ///  Diff
    /// </summary>
    /// <returns></returns>
    IObservable<List<TodoModel>> GetTodos();
    
    Task CreateTodo(TodoModel item);
    
    Task UpdateTodo(TodoModel item);
}