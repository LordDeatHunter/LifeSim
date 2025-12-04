using System.Runtime.Loader;
using DotNetEnv;
using LifeSim.Data;
using LifeSim.Network;
using LifeSim.World;
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

        SocketLogic socketLogic = new();
        var builder = ServerSetup.ConfigureServices(args);
        var app = builder.Build();
        ServerSetup.ConfigureMiddleware(app, builder);
        app.MapControllers();

        using(var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();

            await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;");
        }

        await Task.Delay(100);

        World = app.Services.GetRequiredService<WorldStorage>();

        var retryCount = 0;
        while (retryCount < 3)
        {
            try
            {
                await World.LoadWorldAsync(app.Services);
                break;
            }
            catch (Exception ex) when (retryCount < 2)
            {
                Console.WriteLine($"Failed to load world (attempt {retryCount + 1}/3): {ex.Message}");
                retryCount++;
                await Task.Delay(500 * retryCount);
            }
        }

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

        app.Map("/ws", socketLogic.HandleWebSocket);

        SimulationLoop simulationLoop = new(World);
        BroadcastLoop broadcastLoop = new(socketLogic, World);

        _ = Task.Run(simulationLoop.Start);
        _ = Task.Run(broadcastLoop.Start);

        app.Run("http://localhost:5000");
    }
}