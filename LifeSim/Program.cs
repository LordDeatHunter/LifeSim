using System.Runtime.Loader;
using LifeSim.Network;
using LifeSim.World;

namespace LifeSim;

public static class Program
{
    public static WorldStorage World { get; private set; }
    public static int ReignitionCount { get; set; }
    public static readonly CancellationTokenSource Cts = new();

    public static async Task Main(string[] args)
    {
        SocketLogic socketLogic = new();
        var builder = ServerSetup.ConfigureServices(args);
        var app = builder.Build();
        ServerSetup.ConfigureMiddleware(app, builder);
        app.MapControllers();

        World = app.Services.GetRequiredService<WorldStorage>();
        await World.LoadWorldAsync(app.Services);

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