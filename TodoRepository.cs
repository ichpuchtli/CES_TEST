using Microsoft.VisualBasic;
using SQLite;

namespace CES_TEST;

public record DatabaseOptions(string Path, string Filename, SQLiteOpenFlags Flags);
    
public class TodoRepository : ITodoRepository
{
    private readonly DatabaseOptions _options;

    public TodoRepository(DatabaseOptions options)
    {
        _options = options;
    }
    
    async Task Init()
    {
        if (Database is not null)
            return;

        Database = new SQLiteAsyncConnection(Path.Combine(_options.Path, _options.Filename), _options.Flags);
        Database.Trace = true;
        Database.Tracer = s => System.Diagnostics.Debug.WriteLine(s);
        await Database.CreateTableAsync<TodoModelCtx>();
        await Database.CreateTableAsync<TodoModelSyncCtx>();
    }

    public SQLiteAsyncConnection Database { get; set; }

    public static ICollection<TodoModelCtx> Todos { get; set; } = new List<TodoModelCtx>()
    {
        new TodoModelCtx {
            Id = 1,
            Title = "Hello Word Offline Todo",
            IsCompleted = false
        }
    };
    
    public async Task<List<TodoModel>> GetStoredTodos()
    {
        await Init();
        return (await Database
                .QueryAsync<TodoModelCtx>("SELECT * FROM [TodoModelCtx] WHERE [IsCompleted] = 0"))
                .Select(MapToView)
                .OrderBy(x => x.Id)
                .ToList();
    }

    private TodoModel MapToView(TodoModelCtx todoModelCtx)
    {
        return new TodoModel
        {
            Title = todoModelCtx.Title,
            IsCompleted = todoModelCtx.IsCompleted
        };
    }
    
    private TodoModelSyncCtx MapToSyncCtx(TodoModel todoModelCtx)
    {
        return new TodoModelSyncCtx
        {
            TodoItemId = todoModelCtx.Id,
        };
    }
    
    private TodoModelCtx MapToCtx(TodoModel todoModelCtx)
    {
        return new TodoModelCtx
        {
            Id = todoModelCtx.Id,
            Title = todoModelCtx.Title,
            IsCompleted = todoModelCtx.IsCompleted,
        };
    }

    public async Task MergeWithServerTodos(List<TodoModel> todos)
    {
        await Init();
        // Merge strategy: insert all, will fail on subsequent inserts until we devise a merge
        // strategy
        try
        {
            await Database.DeleteAllAsync<TodoModelCtx>();
            await Database.InsertAllAsync(todos.Select(MapToCtx));
        }
        catch (Exception e)
        {
        }
    }

    public async Task AddLocalTodo(List<TodoModel> todos)
    {
        await Init();
        await Database.InsertAllAsync(todos.Select(MapToCtx));
        await Database.InsertAllAsync(todos.Select(MapToSyncCtx));
    }

    public async Task UpdateLocalTodo(TodoModel item)
    {
        await Init();
        await Database.UpdateAsync(MapToCtx(item));
        await Database.InsertAsync(MapToSyncCtx(item));
    }

    public async Task<List<TodoModel>> GetPendingTodos()
    {
        await Init();
        return (await Database
                .QueryAsync<TodoModelCtx>("""
                                          SELECT todo.Id, todo.Title, todo.IsCompleted
                                          from [TodoModelCtx] todo
                                          inner join [TodoModelSyncCtx] sync on
                                          todo.Id = sync.TodoItemId
                                          where sync.SyncComplete = 0
                                          """))
                                           
                .Select(MapToView)
                .ToList();
    }

    public async Task MarkAsSynchronised(List<TodoModel> pending)
    {
        await Init();
        await Database.QueryAsync<int>("""
                                Update [TodoModelSyncCtx]
                                set SyncComplete = 1
                               """);

    }
}

public class TodoModelSyncCtx
{
    [PrimaryKey]
    [AutoIncrement]
    public int Id { get; set; }
    
    public int TodoItemId { get; set; }
    
    public bool SyncComplete { get; set; }
}