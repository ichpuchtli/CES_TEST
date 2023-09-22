using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;

namespace CES_TEST;

public class TodoService : ITodoService
{
    private readonly IConnectivityService _connectivityService;
    private readonly ITodoRepository _repository;
    private readonly ITodoApiService _apiService;
    
    private ISubject<List<TodoModel>> _todoListUpdatedEvent = new ReplaySubject<List<TodoModel>>(1);
    private ISubject<TodoModel> _todoItemChangeEvent = new ReplaySubject<TodoModel>(1);

    public TodoService(
        IConnectivityService connectivityService,
        ITodoRepository repository,
        ITodoApiService apiService)
    {
        _connectivityService = connectivityService;
        _repository = repository;
        _apiService = apiService;

        _connectivityService
            .ConnectivityChanged
            .Where(x => x == InternetAccess.Internet)
            .Subscribe(_ => FetchServerTodos());

        _todoItemChangeEvent
            .Throttle(TimeSpan.FromSeconds(5))
            .Select(_ => Unit.Default)
            .Merge(connectivityService.ConnectivityChanged
                .Where(access => access == InternetAccess.Internet)
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
            Console.WriteLine(e.ToString());
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

    private IObservable<List<TodoModel>> FetchServerTodos()
    {
        return Observable.FromAsync(async () =>
        {
            if (_connectivityService.IsConnected)
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
        
        // notify new item to sync
        _todoItemChangeEvent.OnNext(item);
    }
}

public class TodoListComparer : IEqualityComparer<List<TodoModel>>
{
    public bool Equals(List<TodoModel> x, List<TodoModel> y)
        => x.SequenceEqual(y);

    public int GetHashCode(List<TodoModel> obj) => obj.Aggregate(0, (x,y)
        => HashCode.Combine(x, y.GetHashCode()));
}