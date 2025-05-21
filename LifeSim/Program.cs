using System.Diagnostics;
using System.Net.WebSockets;
using System.Numerics;
using System.Text;
using System.Text.Json;
using LifeSim.Network;
using LifeSim.Utils;
using LifeSim.World;

namespace LifeSim;

public static class Program
{
    public static WorldStorage World { get; } = new();
    public static void Main(string[] args)
    {
        SocketLogic socketLogic = new();
        LifeSimApi api = new();

        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

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

                var payload = new
                {
                    animals = World.GetAnimalDtos(),
                    foods = World.GetFoodDtos(),
                    timeFromStart,
                    activeClients = activeClients.Count
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