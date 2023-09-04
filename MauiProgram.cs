using Microsoft.Extensions.Logging;

namespace CES_TEST;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
        builder.Services.AddTransient<ITodoApiService, TodoApiService>();
        builder.Services.AddTransient<ITodoRepsitory, TodoRepository>();
        
        builder.Services.AddTransient<ITodoService, TodoService>();
        
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<MainPageViewModel>();
        
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
