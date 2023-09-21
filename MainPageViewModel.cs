using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CES_TEST;

public class MainPageViewModel : BaseViewModel
{
    private readonly ITodoService _service;

    public MainPageViewModel(ITodoService service)
    {
        _service = service;
        RefreshCommand = new Command(Refresh);
        TodoItems = new ObservableCollection<TodoItemViewModel>();
    }
    

    private async void Refresh(object obj)
    {
    }

    public ObservableCollection<TodoItemViewModel> TodoItems { get; set; }
	
    public ICommand RefreshCommand { get; set; }

    public void PageAppearing()
    {
        _service.GetTodos()
            .Select(x => x.Select(MapToObservableObject))
            // TODO only works if we fully buy in to reactive ui
            .Subscribe(todos =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TodoItems.Clear();

                    foreach (var todo in todos)
                    {
                        TodoItems.Add(todo);
                    }
                });
            });
        
        MessagingCenter.Subscribe<TodoItemViewModel>(
            this,
            "TodoItemUpdate",
            (sender) => _service.UpdateTodo(MapToModel(sender)));
    }

    private TodoModel MapToModel(TodoItemViewModel item)
    {
        return new TodoModel
        {
            Id = item.Id,
            Title = item.Title,
            IsCompleted = item.IsCompleted
        };
    }

    private TodoItemViewModel MapToObservableObject(TodoModel arg)
    {
        return new TodoItemViewModel
        {
            Id = arg.Id,
            Title = arg.Title,
            IsCompleted = arg.IsCompleted
        };
    }
}

public class TodoItemViewModel : BaseViewModel
{
    public string Id { get; set; }
    
    public string Title { get; set; }
    
    public bool IsCompleted { get; set; }
    
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        
        MessagingCenter.Send(this, "TodoItemUpdated", new TodoItemUpdate{ ItemId = Id });
    }
}

public record TodoItemUpdate
{
    public string ItemId { get; init; }
}