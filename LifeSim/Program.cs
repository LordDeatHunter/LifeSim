using System.Text.Json;
using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Diagnostics;
using LifeSim.Entities;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var clients = new ConcurrentBag<WebSocket>();
var rng = new Random();
var entities = new Dictionary<int, Entity>();

for (var i = 0; i < 500; i++)
    entities[i] = new Animal(rng.Next(0, 1000), rng.Next(0, 1000));
for (var i = 0; i < 100; i++)
    entities[i] = new Food(rng.Next(0, 1000), rng.Next(0, 1000));

app.UseWebSockets();
app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest) return;

    using var ws = await context.WebSockets.AcceptWebSocketAsync();
    clients.Add(ws);

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

        foreach (var entity in entities.Keys.Select(id => entities[id]))
        {
            entity.Update(delta);
        }

        var json = JsonSerializer.Serialize(entities);
        var buffer = System.Text.Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(buffer);

        foreach (var client in clients.Where(c => c.State == WebSocketState.Open))
        {
            try { await client.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None); }
            catch { /* dead client */ }
        }

        await Task.Delay(100);
    }
});

app.Run("http://localhost:5000");
