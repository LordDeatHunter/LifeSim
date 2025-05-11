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
    public static ConcurrentBag<WebSocket> Clients = new();
    public static Random RNG = new();
    public static Dictionary<int, Entity> Entities = new();
    public static Dictionary<Vector2, Chunk> Chunks = new();

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        for (var i = 0; i < 8; i++)
        for (var j = 0; j < 8; j++)
        {
            var chunkPos = new Vector2(i, j);
            Chunks[chunkPos] = new Chunk(chunkPos);
        }

        for (var i = 0; i < 500; i++)
            Entities[i] = new Animal(RNG.Next(0, 1024), RNG.Next(0, 1024));
        for (var i = 0; i < 100; i++)
            Entities[i] = new Food(RNG.Next(0, 1024), RNG.Next(0, 1024));

        app.UseWebSockets();
        app.Map("/ws", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest) return;

            using var ws = await context.WebSockets.AcceptWebSocketAsync();
            Clients.Add(ws);

            while (ws.State == WebSocketState.Open)
                await Task.Delay(1000);
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

                foreach (var entity in Entities.Keys.Select(id => Entities[id]))
                {
                    entity.Update(delta);
                }

                var json = JsonSerializer.Serialize(Entities);
                var buffer = System.Text.Encoding.UTF8.GetBytes(json);
                var segment = new ArraySegment<byte>(buffer);

                foreach (var client in Clients.Where(c => c.State == WebSocketState.Open))
                {
                    try { await client.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None); }
                    catch { /* dead client */ }
                }

                await Task.Delay(100);
            }
        });

        app.Run("http://localhost:5000");
    }
}