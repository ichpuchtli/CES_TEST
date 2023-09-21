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

        var sqliteDb = new DatabaseOptions(
            FileSystem.AppDataDirectory,
            "Sqlite.db",
            // open the database in read/write mode
            SQLite.SQLiteOpenFlags.ReadWrite |
            // create the database if it doesn't exist
            SQLite.SQLiteOpenFlags.Create |
            // enable multi-threaded database access
            SQLite.SQLiteOpenFlags.SharedCache);
            
        builder.Services.AddSingleton(sqliteDb);
        
        builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
        builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
        builder.Services.AddTransient<ITodoApiService, TodoApiService>();
        builder.Services.AddTransient<ITodoRepository, TodoRepository>();
        
        builder.Services.AddTransient<ITodoService, TodoService>();
        
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<MainPageViewModel>();
        
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}