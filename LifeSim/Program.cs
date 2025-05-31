using LifeSim.Network;
using LifeSim.World;

namespace LifeSim;

public static class Program
{
    public static WorldStorage World { get; } = new();
    public static int ReignitionCount { get; set; }

    public static void Main(string[] args)
    {
        SocketLogic socketLogic = new();
        LifeSimApi api = new();

        var builder = WebApplication.CreateBuilder(args);
        ServerSetup.ConfigureServices(builder);

        var app = builder.Build();
        ServerSetup.ConfigureMiddleware(app, builder);

        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            Console.WriteLine("Unhandled exception: " + e.ExceptionObject);
        };

        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            Console.WriteLine("Unobserved task exception: " + e.Exception);
            e.SetObserved();
        };

        app.Map("/api/reignite_life", api.ReigniteLifeHandler);
        app.Map("/api/balance", api.GetBalance);
        app.Map("/api/place-bet", api.PlaceBet);
        app.Map("/api/bets", api.GetBets);
        app.Map("/api/bet/{id:guid}", api.GetBetById);
        app.Map("/api/leaderboards", api.GetLeaderboards);
        app.Map("/api/set-name", api.SetName);
        app.Map("/ws", socketLogic.HandleWebSocket);

        SimulationLoop simulationLoop = new(World);
        BroadcastLoop broadcastLoop = new(socketLogic, World);

        _ = Task.Run(simulationLoop.Start);
        _ = Task.Run(broadcastLoop.Start);

        app.Run("http://localhost:5000");
    }
}