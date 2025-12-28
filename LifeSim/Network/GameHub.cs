using Microsoft.AspNetCore.SignalR;

namespace LifeSim.Network;

public class GameHub(StatisticsTracker statisticsTracker) : Hub
{
    private static readonly HashSet<string> ConnectedIds = [];
    private static readonly Lock Lock = new();

    public override async Task OnConnectedAsync()
    {
        lock (Lock)
        {
            ConnectedIds.Add(Context.ConnectionId);
        }

        var allAnimals = Program.World.GetAnimalDtos();
        var allFoods = Program.World.GetFoodDtos();

        var currentLifeDuration = Program.LastReignitionTime != DateTime.MinValue
            ? (DateTime.UtcNow - Program.LastReignitionTime).TotalMilliseconds
            : 0;

        var initialPayload = new
        {
            animals = new
            {
                added = allAnimals,
                removed = Array.Empty<string>(),
                updated = new Dictionary<string, Dictionary<string, object>>()
            },
            foods = new
            {
                added = allFoods,
                removed = Array.Empty<string>(),
                updated = new Dictionary<string, Dictionary<string, object>>()
            },
            timeFromStart = 0.0,
            activeClients = GetActiveClientCount(),
            reignitions = Program.ReignitionCount,
            currentLifeDuration,
            longestLifeDuration = Program.LongestLifeDuration.TotalMilliseconds,
            statistics = statisticsTracker.GetAllSnapshots()
        };

        await Clients.Caller.SendAsync("ReceiveUpdate", initialPayload);
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        lock (Lock)
        {
            ConnectedIds.Remove(Context.ConnectionId);
        }
        return base.OnDisconnectedAsync(exception);
    }

    public static int GetActiveClientCount()
    {
        lock (Lock)
        {
            return ConnectedIds.Count;
        }
    }
}