using System.Net.Http.Json;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CES_TEST;

public interface ITodoService
{
    IObservable<List<TodoModel>> GetTodos();
    
    Task CreateTodo(TodoModel item);
}

public class TodoService : ITodoService
{
    private readonly IConnectivityService _connectivity;
    private readonly ITodoRepsitory _repository;
    private readonly ITodoApiService _apiService;
    private ISubject<List<TodoModel>> _todoListUpdated = new ReplaySubject<List<TodoModel>>();

    public TodoService(
        IConnectivityService connectivity,
        ITodoRepsitory repository,
        ITodoApiService apiService)
    {
        _connectivity = connectivity;
        _repository = repository;
        _apiService = apiService;
    }
    
    public IObservable<List<TodoModel>> GetTodos()
    {
        Task.Factory.StartNew(async () =>
        {
            var storedTodos = await _repository.GetStoredTodos();
            _todoListUpdated.OnNext(storedTodos);
        });
        
        Task.Factory.StartNew(async () =>  await FetchLatestTodos());
        
        return _todoListUpdated;
    }

    private async Task FetchLatestTodos()
    {
        if (_connectivity.NetworkAccess == NetworkAccess.Internet)
        {
            var serverTodos = await _apiService.GetTodos();
            await _repository.MergeWithServerTodos(serverTodos);
            _todoListUpdated.OnNext(serverTodos);
        }
    }

    public Task CreateTodo(TodoModel item)
    {
        _repository.AddLocalTodo(new List<TodoModel>() { item });
        
        if (_connectivity.NetworkAccess == NetworkAccess.Internet)
        {
            _apiService.SaveAsync(new List<TodoModel>() {item});
        }
        
        return Task.CompletedTask;
    }
}

public enum NetworkAccess
{
    Internet
}

public interface IConnectivityService
{
    public NetworkAccess NetworkAccess { get; }
}

public interface ITodoApiService
{
    Task<List<TodoModel>> GetTodos();
    
    Task SaveAsync(List<TodoModel> todos);
}

public class TodoApiService : ITodoApiService
{
    private readonly IHttpClientFactory _clientFactory;

    public TodoApiService(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }
    
    public async Task<List<TodoModel>> GetTodos()
    {
        using (var client = _clientFactory.CreateClient())
        {
            try
            {
                var response = await client.GetAsync("https://jsonplaceholder.typicode.com/todos");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadFromJsonAsync<List<TodoModel>>();

                    return content;
                }

                throw new Exception("Error getting todos");
            }
            catch (Exception e)
            {
                throw;
                //_logger.LogError(e, "Error getting todos");
                // exceptionService.TrackError(e);
            }
        }
    }

    public Task SaveAsync(List<TodoModel> todos)
    {
        return Task.CompletedTask;
    }
}

public class OfflineCacheInterceptor : DelegatingHandler
{
    private readonly IConnectivityService _connectivity;
    private readonly ITodoRepsitory _repository;

    public OfflineCacheInterceptor(IConnectivityService connectivity,
        ITodoRepsitory repository)
    {
        _connectivity = connectivity;
        _repository = repository;
    }
}

public interface ITodoRepsitory
{
    Task<List<TodoModel>> GetStoredTodos();
    
    Task MergeWithServerTodos(List<TodoModel> todos);
    
    Task AddLocalTodo(List<TodoModel> todos);
}

public class TodoRepository : ITodoRepsitory
{
    public static ICollection<TodoModelCtx> Todos { get; set; } = new List<TodoModelCtx>()
    {
        new TodoModelCtx
        {
            Title = "Hello Word Offline Todo",
            IsCompleted = false
        },
    };
    
    public Task<List<TodoModel>> GetStoredTodos()
    {
        var todoModelCtxes = Todos
            .Where(x => !x.IsCompleted)
            .Select(MapToView)
            .ToList();
        
        return Task.FromResult(todoModelCtxes);
    }

    private TodoModel MapToView(TodoModelCtx todoModelCtx)
    {
        return new TodoModel
        {
            Title = todoModelCtx.Title,
            IsCompleted = todoModelCtx.IsCompleted
        };
    }
    
    private TodoModelCtx MapToCtx(TodoModel todoModelCtx)
    {
        return new TodoModelCtx
        {
            Title = todoModelCtx.Title,
            IsCompleted = todoModelCtx.IsCompleted
        };
    }

    public Task MergeWithServerTodos(List<TodoModel> todos)
    {
        Todos = Todos.Union(todos.Select(MapToCtx)).ToList();
        return Task.CompletedTask;
    }

    public Task AddLocalTodo(List<TodoModel> todos)
    {
        return Task.CompletedTask;
    }
}

public record TodoModelCtx
{
    public string Title { get; set;}
    
    public bool IsCompleted { get; set;}
    
    public bool IsSynced { get; set;}
}