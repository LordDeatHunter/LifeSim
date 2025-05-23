using System.Diagnostics;
using System.Net.WebSockets;
using System.Numerics;
using System.Text;
using System.Text.Json;
using LifeSim.Data;
using LifeSim.Network;
using LifeSim.Utils;
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
        var app = builder.Build();

        var previousAnimals = new Dictionary<string, AnimalDto>();
        var previousFoods = new Dictionary<string, FoodDto>();

        for (var i = 0; i <= 32; i++)
        for (var j = 0; j <= 32; j++)
        {
            var chunkPos = new Vector2(i, j);
            World.Chunks[chunkPos] = new Chunk(chunkPos);
        }

        World.SpawnFood(400, 0, 1024);

        app.UseWebSockets();

        app.Map("/api/reignite_life", api.ReigniteLifeHandler);
        app.Map("/ws", socketLogic.HandleWebSocket);

        _ = Task.Run(async () =>
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var lastTicks = stopwatch.ElapsedTicks;
            var tickFrequency = (float)Stopwatch.Frequency;

            while (true)
            {
                var currentTicks = stopwatch.ElapsedTicks;
                var delta = (currentTicks - lastTicks) / tickFrequency;
                lastTicks = currentTicks;

                foreach (var entity in World.AllEntities.Values)
                {
                    entity.Update(delta);
                }

                var timeFromStart = stopwatch.Elapsed.TotalMilliseconds;
                var activeClients = socketLogic.Clients.Keys.Where(c => c.State == WebSocketState.Open).ToList();

                var currentAnimals = World.GetAnimalDtos().ToDictionary(a => a.id, a => a);
                var currentFoods = World.GetFoodDtos().ToDictionary(f => f.id, f => f);

                var addedAnimals   = new Dictionary<string, AnimalDto>();
                var updatedAnimals = new Dictionary<string, Dictionary<string, object>>();
                var removedAnimals = new List<string>();
                
                var addedFoods   = new Dictionary<string, FoodDto>();
                var updatedFoods = new Dictionary<string, Dictionary<string, object>>();
                var removedFoods = new List<string>();

                foreach (var id in currentAnimals.Keys.Except(previousAnimals.Keys))
                    addedAnimals[id] = currentAnimals[id];
                foreach (var id in currentFoods.Keys.Except(previousFoods.Keys))
                    addedFoods[id] = currentFoods[id];

                foreach (var id in currentAnimals.Keys.Intersect(previousAnimals.Keys))
                {
                    var old = previousAnimals[id];
                    var curr = currentAnimals[id];
                    var diff = new Dictionary<string, object>();
                    if (curr.id != old.id) diff["id"] = curr.id;
                    if (curr.x != old.x) diff["x"] = curr.x;
                    if (curr.y != old.y) diff["y"] = curr.y;
                    if (curr.color != old.color) diff["color"] = curr.color;
                    if (curr.size != old.size) diff["size"] = curr.size;
                    if (curr.foodType != old.foodType) diff["foodType"] = curr.foodType;

                    if (diff.Count > 0)
                        updatedAnimals[id] = diff;
                }
                foreach (var id in currentFoods.Keys.Intersect(previousFoods.Keys))
                {
                    var old = previousFoods[id];
                    var curr = currentFoods[id];
                    var diff = new Dictionary<string, object>();
                    if (curr.id != old.id) diff["id"] = curr.id;
                    if (curr.x != old.x) diff["x"] = curr.x;
                    if (curr.y != old.y) diff["y"] = curr.y;
                    if (curr.color != old.color) diff["color"] = curr.color;
                    if (curr.size != old.size) diff["size"] = curr.size;

                    if (diff.Count > 0)
                        updatedFoods[id] = diff;
                }

                removedAnimals.AddRange(previousAnimals.Keys.Except(currentAnimals.Keys));
                removedFoods.AddRange(previousFoods.Keys.Except(currentFoods.Keys));

                previousAnimals = currentAnimals;
                previousFoods = currentFoods;

                var payload = new
                {
                    animals = new {
                        added = addedAnimals,
                        removed = removedAnimals,
                        updated = updatedAnimals
                    },
                    foods = new {
                        added = addedFoods,
                        removed = removedFoods,
                        updated = updatedFoods
                    },
                    timeFromStart,
                    activeClients = activeClients.Count,
                    reignitions = ReignitionCount
                };

                var json = JsonSerializer.Serialize(payload);
                var buffer = Encoding.UTF8.GetBytes(json);
                var segment = new ArraySegment<byte>(buffer);

                foreach (var client in activeClients)
                {
                    try { await client.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None); }
                    catch { /* dead client */ }
                }

                if (World.Foods.Count < 1000)
                {
                    var foodAmount = RandomUtils.RNG.Next(0, 10);
                    World.SpawnFood(foodAmount, 0, 1024);
                }

                await Task.Delay(100);
            }
        });

        app.Run("http://localhost:5000");
    }
}