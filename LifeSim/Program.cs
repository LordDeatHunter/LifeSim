using System.Runtime.Loader;
using DotNetEnv;
using LifeSim.Data;
using LifeSim.Network;
using LifeSim.World;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LifeSim;

public static class Program
{
    public static WorldStorage World { get; private set; }
    public static int ReignitionCount { get; set; }
    public static DateTime LastReignitionTime { get; set; } = DateTime.MinValue;
    public static TimeSpan LongestLifeDuration { get; set; } = TimeSpan.Zero;
    public static readonly CancellationTokenSource Cts = new();

    public static async Task Main(string[] args)
    {
        Env.Load();

        var builder = ServerSetup.ConfigureServices(args);
        var app = builder.Build();
        app.MapControllers();
        app.MapHub<GameHub>("/hub");

        using(var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            try
            {
                await db.Database.MigrateAsync();

                await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
                await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;");
            }
            finally
            {
                // Ensure all connections are properly closed
                await db.Database.CloseConnectionAsync();
                await db.DisposeAsync();
            }
        }

        // Give SQLite time to release the exclusive migration lock
        Console.WriteLine("Waiting for database lock to be released...");
        await Task.Delay(2000);

        World = app.Services.GetRequiredService<WorldStorage>();
        Console.WriteLine("World initialized successfully!");

        Console.CancelKeyPress += (_, e) =>
        {
            Console.WriteLine("Terminating...");
            Cts.Cancel();
            e.Cancel = true;
        };

        AssemblyLoadContext.Default.Unloading += _ =>
        {
            Console.WriteLine("Terminating...");
            Cts.Cancel();
        };

        AppDomain.CurrentDomain.ProcessExit += (_, _) => Cts.Cancel();

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Console.WriteLine("Unhandled exception: " + e.ExceptionObject);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Console.WriteLine("Unobserved task exception: " + e.Exception);
            e.SetObserved();
        };

        var hubContext = app.Services.GetRequiredService<IHubContext<GameHub>>();
        var statisticsTracker = app.Services.GetRequiredService<StatisticsTracker>();

        SimulationLoop simulationLoop = new(World);
        BroadcastLoop broadcastLoop = new(hubContext, World, statisticsTracker);

        _ = Task.Run(simulationLoop.Start);
        _ = Task.Run(broadcastLoop.Start);

        app.Run("http://localhost:5000");
    }
}