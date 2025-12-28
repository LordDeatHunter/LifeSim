using System.Collections.Concurrent;
using System.Diagnostics;
using LifeSim.Data;
using LifeSim.World;
using Microsoft.AspNetCore.SignalR;

namespace LifeSim.Network;

public class BroadcastLoop(IHubContext<GameHub> hubContext, WorldStorage world, StatisticsTracker statisticsTracker)
{
    public async Task Start()
    {
        var previousAnimals = new Dictionary<string, AnimalDto>();
        var previousFoods = new Dictionary<string, FoodDto>();
        var stopwatch = Stopwatch.StartNew();
        var lastBroadcastTime = DateTime.UtcNow;

        while (!Program.Cts.IsCancellationRequested)
        {
            var currentAnimals = world.GetAnimalDtos();
            var currentFoods = world.GetFoodDtos();

            statisticsTracker.RecordSnapshot(currentAnimals.Count, currentFoods.Count);

            var addedAnimals = new Dictionary<string, AnimalDto>();
            foreach (var kvp in currentAnimals.Where(kvp => !previousAnimals.ContainsKey(kvp.Key)))
                addedAnimals[kvp.Key] = kvp.Value;

            var addedFoods = new Dictionary<string, FoodDto>();
            foreach (var kvp in currentFoods.Where(kvp => !previousFoods.ContainsKey(kvp.Key)))
                addedFoods[kvp.Key] = kvp.Value;

            var removedAnimals = previousAnimals.Keys.Where(key => !currentAnimals.ContainsKey(key)).ToList();
            var removedFoods = previousFoods.Keys.Where(key => !currentFoods.ContainsKey(key)).ToList();

            var updatedAnimals = ComputeDiffs(currentAnimals, previousAnimals);
            var updatedFoods = ComputeDiffs(currentFoods, previousFoods);

            previousAnimals.Clear();
            foreach (var kvp in currentAnimals)
                previousAnimals[kvp.Key] = kvp.Value;

            previousFoods.Clear();
            foreach (var kvp in currentFoods)
                previousFoods[kvp.Key] = kvp.Value;

            var currentLifeDuration = Program.LastReignitionTime != DateTime.MinValue
                ? (DateTime.UtcNow - Program.LastReignitionTime).TotalMilliseconds
                : 0;

            var recentStatistics = statisticsTracker.GetSnapshotsSince(lastBroadcastTime);

            var payload = new
            {
                animals = new
                {
                    added = addedAnimals,
                    removed = removedAnimals,
                    updated = updatedAnimals
                },
                foods = new
                {
                    added = addedFoods,
                    removed = removedFoods,
                    updated = updatedFoods
                },
                timeFromStart = stopwatch.Elapsed.TotalMilliseconds,
                activeClients = GameHub.GetActiveClientCount(),
                reignitions = Program.ReignitionCount,
                currentLifeDuration,
                longestLifeDuration = Program.LongestLifeDuration.TotalMilliseconds,
                statistics = recentStatistics
            };

            await hubContext.Clients.All.SendAsync("ReceiveUpdate", payload, Program.Cts.Token);

            lastBroadcastTime = DateTime.UtcNow;
            await Task.Delay(300, Program.Cts.Token);
        }
    }

    private static Dictionary<string, Dictionary<string, object>> ComputeDiffs<T>(
        ConcurrentDictionary<string, T> current,
        Dictionary<string, T> previous
    ) where T : IEntityDto
    {
        var diffs = new Dictionary<string, Dictionary<string, object>>();

        foreach (var kvp in current)
        {
            var id = kvp.Key;
            if (!previous.TryGetValue(id, out var old))
                continue;

            var curr = kvp.Value;

            var diff = new Dictionary<string, object>();
            if (!curr.Equals(old))
            {
                foreach (var prop in typeof(T).GetProperties())
                {
                    var valOld = prop.GetValue(old);
                    var valNew = prop.GetValue(curr);
                    if (!Equals(valOld, valNew))
                        diff[prop.Name[..1].ToLower() + prop.Name[1..]] = valNew!;
                }
            }

            if (diff.Count > 0)
                diffs[id] = diff;
        }

        return diffs;
    }
}