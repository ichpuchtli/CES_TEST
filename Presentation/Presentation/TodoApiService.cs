using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CES_TEST;

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

                    return content.Take(10).ToList();
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
        System.Diagnostics.Debug.WriteLine("Saving Pending Todos..", JsonSerializer.Serialize(todos));
        return Task.CompletedTask;
    }
}