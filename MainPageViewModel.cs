using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Windows.Input;

namespace CES_TEST;

public class MainPageViewModel : BaseViewModel
{
    private readonly ITodoService _service;

    public MainPageViewModel(ITodoService service)
    {
        _service = service;
        RefreshCommand = new Command(Refresh);
        TodoItems = new ObservableCollection<TodoModel>();
    }

    private async void Refresh(object obj)
    {
        TodoItems.Clear();

        var content = await _service.GetTodos();
        
        foreach (var todo in content)
        {
            TodoItems.Add(todo);
        }
    }

    public ObservableCollection<TodoModel> TodoItems { get; set; }
	
    public ICommand RefreshCommand { get; set; } 
}

public interface ITodoService
{
    Task<List<TodoModel>> GetTodos();
    
    Task CreateTodo(TodoModel item);
}

public class TodoService : ITodoService
{
    private readonly IConnectivity _connectivity;
    private readonly ITodoRepsitory _repository;
    private readonly ITodoApiService _apiService;

    public TodoService(
        IConnectivity connectivity,
        ITodoRepsitory repository,
        ITodoApiService apiService)
    {
        _connectivity = connectivity;
        _repository = repository;
        _apiService = apiService;
    }
    
    public async Task<List<TodoModel>> GetTodos()
    {
        return await ExecuteStrategy(() => _apiService.GetTodos());
    }

    private async Task<List<TodoModel>> ExecuteStrategy(Func<Task<List<TodoModel>>> func)
    {
        try
        {
            if (_connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                return await func();
            }
            else
            {
                return await _repository.GetStoredTodos();
            }
        }
        catch (Exception e)
        {
            return await _repository.GetStoredTodos();
        }
    }

    public Task CreateTodo(TodoModel item)
    {
        //await CreateTodoOnApi(item);
        return Task.CompletedTask;
    }
}

public interface ITodoApiService
{
    Task<List<TodoModel>> GetTodos();
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
}

public class OfflineCacheInterceptor : DelegatingHandler
{
    private readonly IConnectivity _connectivity;
    private readonly ITodoRepsitory _repository;

    public OfflineCacheInterceptor(IConnectivity connectivity,
        ITodoRepsitory repository)
    {
        _connectivity = connectivity;
        _repository = repository;
    }
}

public interface ITodoRepsitory
{
    Task<List<TodoModel>> GetStoredTodos();
    
    Task UpdateStoredTodos(List<TodoModel> todos);
}

public class TodoRepository : ITodoRepsitory
{
    public static ICollection<TodoModelCtx> Todos { get; set; } = new List<TodoModelCtx>();
    
    public TodoRepository()
    {
    }
    
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

    public Task UpdateStoredTodos(List<TodoModel> todos)
    {
        throw new NotImplementedException();
    }
}

public class TodoModelCtx
{
    public string Title { get; set;}
    
    public bool IsCompleted { get; set;}
}