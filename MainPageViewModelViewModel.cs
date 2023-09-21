using System.Collections.ObjectModel;
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
    }

    public ObservableCollection<TodoModel> TodoItems { get; set; }
	
    public ICommand RefreshCommand { get; set; }

    public void PageAppearing()
    {
        _service.GetTodos()
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
    }
}