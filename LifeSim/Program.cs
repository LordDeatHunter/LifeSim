using System.Text.Json;
using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Drawing;
using LifeSim;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var clients = new ConcurrentBag<WebSocket>();
var rng = new Random();
var entities = new Dictionary<int, Entity>();

for (var i = 0; i < 500; i++)
    entities[i] = new Entity(rng.Next(0, 1000), rng.Next(0, 1000), Color.CornflowerBlue);
for (var i = 0; i < 100; i++)
    entities[i] = new Entity(rng.Next(0, 1000), rng.Next(0, 1000), Color.Crimson);

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
    while (true)
    {
        foreach (var id in entities.Keys.ToList())
        {
            entities[id].X += (float)(rng.NextDouble() - 0.5) * 5;
            entities[id].Y += (float)(rng.NextDouble() - 0.5) * 5;

            entities[id].X = float.Clamp(entities[id].X, 0, 1000);
            entities[id].Y = float.Clamp(entities[id].Y, 0, 1000);
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
