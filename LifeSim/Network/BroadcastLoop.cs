using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using LifeSim.Data;
using LifeSim.World;

namespace LifeSim.Network;

public class BroadcastLoop(SocketLogic socketLogic, WorldStorage world)
{
    public async Task Start()
    {
        var previousAnimals = new Dictionary<string, AnimalDto>();
        var previousFoods = new Dictionary<string, FoodDto>();
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            var activeClients = socketLogic.Clients.Keys.Where(c => c.State == WebSocketState.Open).ToList();

            var currentAnimals = world.GetAnimalDtos().ToDictionary();
            var currentFoods = world.GetFoodDtos().ToDictionary();

            var addedAnimals = currentAnimals.Keys.Except(previousAnimals.Keys)
                .ToDictionary(id => id, id => currentAnimals[id]);
            var addedFoods = currentFoods.Keys.Except(previousFoods.Keys)
                .ToDictionary(id => id, id => currentFoods[id]);

            var removedAnimals = previousAnimals.Keys.Except(currentAnimals.Keys).ToList();
            var removedFoods = previousFoods.Keys.Except(currentFoods.Keys).ToList();

            var updatedAnimals = ComputeDiffs(currentAnimals, previousAnimals);
            var updatedFoods = ComputeDiffs(currentFoods, previousFoods);

            previousAnimals = currentAnimals.ToDictionary();
            previousFoods = currentFoods.ToDictionary();

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
                activeClients = activeClients.Count,
                reignitions = Program.ReignitionCount
            };

            var json = JsonSerializer.Serialize(payload);
            var buffer = Encoding.UTF8.GetBytes(json);
            var segment = new ArraySegment<byte>(buffer);

            foreach (var client in activeClients)
            {
                try
                {
                    await client.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch
                {
                    /* dead client */
                }
            }

            await Task.Delay(300);
        }
    }

    private static Dictionary<string, Dictionary<string, object>> ComputeDiffs<T>(
        Dictionary<string, T> current,
        Dictionary<string, T> previous
    ) where T : IEntityDto
    {
        var diffs = new Dictionary<string, Dictionary<string, object>>();

        foreach (var id in current.Keys.Intersect(previous.Keys))
        {
            var old = previous[id];
            var curr = current[id];

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