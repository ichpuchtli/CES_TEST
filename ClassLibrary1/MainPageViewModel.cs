using System.Net.Http.Json;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

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

public class TodoService : ITodoService
{
    private readonly IConnectivityService _connectivity;
    private readonly ITodoRepository _repository;
    private readonly ITodoApiService _apiService;
    private ISubject<List<TodoModel>> _todoListUpdatedEvent = new ReplaySubject<List<TodoModel>>();
    private ISubject<TodoModel> _todoItemChangeEvent = new ReplaySubject<TodoModel>();

    public TodoService(
        IConnectivityService connectivity,
        ITodoRepository repository,
        ITodoApiService apiService)
    {
        _connectivity = connectivity;
        _repository = repository;
        _apiService = apiService;

        _connectivity
            .ConnectivityChanged
            .Where(x => x == NetworkAccess.Internet)
            .Subscribe(_ => FetchServerTodos());

        _todoItemChangeEvent
            .Throttle(TimeSpan.FromSeconds(5))
            .Select(_ => Unit.Default)
            .Merge(connectivity.ConnectivityChanged
                .Where(access => access == NetworkAccess.Internet)
                .Select(_ => Unit.Default))
            .Subscribe(_ => SavePendingChanges());
    }

    private async Task SavePendingChanges()
    {
        try
        {
            var pending = await _repository.GetPendingTodos();
            await _apiService.SaveAsync(pending);
            await _repository.MarkAsSynchronised(pending);
        }
        catch (Exception e)
        {
        }
    }

    public IObservable<List<TodoModel>> GetTodos()
    {
        return _todoListUpdatedEvent
            .Merge(FetchLocalTodos())
            .Merge(FetchServerTodos())
            .Where(x => x.Count > 0)
            .DistinctUntilChanged(new TodoListComparer());
    }

    private IObservable<List<TodoModel>> FetchLocalTodos()
    {
        return Observable.FromAsync(() => _repository.GetStoredTodos());
    }

    public class TodoListComparer : IEqualityComparer<List<TodoModel>>
    {
        public bool Equals(List<TodoModel> x, List<TodoModel> y)
        {
            return x.SequenceEqual(y.OrderBy(t => t));
        }

        public int GetHashCode(List<TodoModel> obj)
        {
            return obj.Aggregate(0, (x,y) => HashCode.Combine(x, y.GetHashCode()));
        }
    }

    private IObservable<List<TodoModel>> FetchServerTodos()
    {
        return Observable.FromAsync(async () =>
        {
            if (_connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                var serverTodos = await _apiService.GetTodos();
                await _repository.MergeWithServerTodos(serverTodos);
                return await _repository.GetStoredTodos();
            }

            return new List<TodoModel>();
        });
    }

    public async Task CreateTodo(TodoModel item)
    {
        await _repository.AddLocalTodo(new List<TodoModel>() { item });

        // Notify list subscribers
        _todoListUpdatedEvent.OnNext(await _repository.GetStoredTodos());
        
        // notify new item to sync
        _todoItemChangeEvent.OnNext(item);
    }

    public async Task UpdateTodo(TodoModel item)
    {
        await _repository.UpdateLocalTodo(item);
        
        // Notify list subscribers
        _todoListUpdatedEvent.OnNext(await _repository.GetStoredTodos());
        
        // notify new item to sync
        _todoItemChangeEvent.OnNext(item);
    }
}

public enum NetworkAccess
{
    Unknown,
    NoNetwork,
    Local,
    ConstrainedInternet,
    Internet,
}

public interface IConnectivityService
{
    public bool IsConnected { get; }
    public NetworkAccess NetworkAccess { get; }
    
    IObservable<NetworkAccess> ConnectivityChanged { get; }
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
    private readonly ITodoRepository _repository;

    public OfflineCacheInterceptor(IConnectivityService connectivity,
        ITodoRepository repository)
    {
        _connectivity = connectivity;
        _repository = repository;
    }
}

public interface ITodoRepository
{
    Task<List<TodoModel>> GetStoredTodos();
    
    Task MergeWithServerTodos(List<TodoModel> todos);
    
    Task AddLocalTodo(List<TodoModel> todos);
    
    Task UpdateLocalTodo(TodoModel item);
    
    Task<List<TodoModel>> GetPendingTodos();
    
    Task MarkAsSynchronised(List<TodoModel> pending);
}