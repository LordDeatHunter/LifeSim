using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Numerics;
using System.Text.Json;
using LifeSim.Entities;
using LifeSim.World;

namespace LifeSim;

public static class Program
{
    public static ConcurrentDictionary<WebSocket, byte> Clients = new();
    public static Random RNG = new();
    public static Dictionary<Guid, Food> Foods = new();
    public static Dictionary<Guid, Animal> Animals = new();
    public static Dictionary<Vector2, Chunk> Chunks = new();
    // helper for merging foods and animals
    public static Dictionary<Guid, Entity> AllEntities =>
        Animals.Values.ToDictionary(a => a.Id, Entity (a) => a)
        .Concat(Foods.Values.ToDictionary(f => f.Id, Entity (f) => f))
        .ToDictionary(e => e.Key, e => e.Value);

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        for (var i = 0; i <= 32; i++)
        for (var j = 0; j <= 32; j++)
        {
            var chunkPos = new Vector2(i, j);
            Chunks[chunkPos] = new Chunk(chunkPos);
        }

        for (var i = 0; i < 400; i++)
        {
            var food = new Food(new Vector2(RNG.Next(0, 1024), RNG.Next(0, 1024)));
            Foods[food.Id] = food;
        }

        app.UseWebSockets();

        app.Map("/api/reignite_life", _ =>
        {
            if (Animals.Count > 0) return Task.CompletedTask;

            var animalCount = RNG.Next(4, 16);

            for (var i = 0; i < animalCount; i++)
            {
                var animal = new Animal(new Vector2(RNG.Next(350, 650), RNG.Next(350, 650)));
                Animals[animal.Id] = animal;
            }

            return Task.CompletedTask;
        });

        app.Map("/ws", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest) return;

            using var ws = await context.WebSockets.AcceptWebSocketAsync();
            Clients.TryAdd(ws, 0);

            var buffer = new byte[1];

            try
            {
                while (ws.State == WebSocketState.Open)
                {
                    var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close) break;
                    await Task.Delay(1000);
                }
            }
            catch
            { /* ignore */ }
            finally
            {
                Clients.TryRemove(ws, out _);
                if (ws.State is not (WebSocketState.Closed or WebSocketState.Aborted))
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Inactive socket cleanup.", CancellationToken.None);
            }
        });

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

                foreach (var entity in AllEntities.Values)
                {
                    entity.Update(delta);
                }

                var foodDTOs = Foods.Values.Select(f => f.ToDTO());
                var animalDTOs = Animals.Values.Select(a => a.ToDTO());

                var timeFromStart = stopwatch.Elapsed.TotalMilliseconds;
                var activeClients = Clients.Keys.Where(c => c.State == WebSocketState.Open).ToList();

                var payload = new
                {
                    animals = animalDTOs,
                    foods = foodDTOs,
                    timeFromStart,
                    activeClients = activeClients.Count
                };

                var json = JsonSerializer.Serialize(payload);
                var buffer = System.Text.Encoding.UTF8.GetBytes(json);
                var segment = new ArraySegment<byte>(buffer);

                foreach (var client in activeClients)
                {
                    try { await client.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None); }
                    catch { /* dead client */ }
                }

                if (Foods.Count < 1000)
                {
                    var foodAmount = RNG.Next(0, 10);
                    for (var i = 0; i < foodAmount; i++)
                    {
                        var food = new Food(new Vector2(RNG.Next(0, 1024), RNG.Next(0, 1024)));
                        Foods[food.Id] = food;
                    }
                }

                await Task.Delay(100);
            }
        });

        app.Run("http://localhost:5000");
    }
}