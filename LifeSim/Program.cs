using System.Text.Json;
using System.Net.WebSockets;
using System.Collections.Concurrent;
using LifeSim;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var clients = new ConcurrentBag<WebSocket>();
var rng = new Random();
var entities = new Dictionary<int, Entity>();

for (int i = 0; i < 500; i++)
    entities[i] = new Entity(rng.Next(0, 100), rng.Next(0, 100));

app.UseWebSockets();
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var ws = await context.WebSockets.AcceptWebSocketAsync();
        clients.Add(ws);

        while (ws.State == WebSocketState.Open)
            await Task.Delay(1000);
    }
});

_ = Task.Run(async () =>
{
    while (true)
    {
        foreach (var id in entities.Keys.ToList())
        {
            var (x, y) = entities[id];
            x += (float)(rng.NextDouble() - 0.5) * 5;
            y += (float)(rng.NextDouble() - 0.5) * 5;
            entities[id] = new Entity(x, y);
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
