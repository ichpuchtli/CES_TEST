using System.Reactive.Subjects;
using CES_TEST;
using Moq;

namespace TestProject1;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void TestMethod1()
    {
        var connectivity = new Mock<IConnectivityService>();
        var mockApiService = new Mock<ITodoApiService>();
        
        mockApiService
            .Setup(x => x.SaveAsync(It.IsAny<List<TodoModel>>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var todoRepository = new Mock<ITodoRepsitory>();
        
        List<TodoModel> savedTodos = null;
        
        todoRepository
            .Setup(x => x.AddLocalTodo(It.IsAny<List<TodoModel>>()))
            .Returns(Task.CompletedTask)
            .Verifiable();
        
        var todoService = new TodoService(
            connectivity.Object,
            todoRepository.Object,
            mockApiService.Object
        );
        
        connectivity
            .SetupGet(x => x.NetworkAccess)
            .Returns(NetworkAccess.Internet);

        todoService.CreateTodo(new TodoModel() {Title = "Test", IsCompleted = false});
        
        mockApiService.Verify(x => x.SaveAsync(It.IsAny<List<TodoModel>>()), Times.Once);
        todoRepository.Verify(x => x.AddLocalTodo(It.IsAny<List<TodoModel>>()), Times.Once);
    }
}