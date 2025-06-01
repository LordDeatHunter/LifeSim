using System.Runtime.Loader;
using LifeSim.Network;
using LifeSim.World;

namespace LifeSim;

public static class Program
{
    public static WorldStorage World { get; } = new();
    public static int ReignitionCount { get; set; }
    public static readonly CancellationTokenSource Cts = new();

    public static void Main(string[] args)
    {
        SocketLogic socketLogic = new();
        LifeSimApi api = new();

        var builder = WebApplication.CreateBuilder(args);
        ServerSetup.ConfigureServices(builder);
        builder.Services.AddControllers();
        builder.Services.AddSingleton(api);

        var app = builder.Build();
        ServerSetup.ConfigureMiddleware(app, builder);
        app.MapControllers();

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