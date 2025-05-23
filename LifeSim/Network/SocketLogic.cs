using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace LifeSim.Network;

public class SocketLogic
{
    public ConcurrentDictionary<WebSocket, byte> Clients { get; } = new();

    public async Task HandleWebSocket(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest) return;

        using var ws = await context.WebSockets.AcceptWebSocketAsync();
        Clients.TryAdd(ws, 0);

        var allAnimals = Program.World
            .GetAnimalDtos()
            .ToDictionary(a => a.id, a => a);
        var allFoods = Program.World
            .GetFoodDtos()
            .ToDictionary(f => f.id, f => f);

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
            activeClients = Clients.Keys.Count(c => c.State == WebSocketState.Open),
            reignitions = Program.ReignitionCount
        };

        var initJson = JsonSerializer.Serialize(initialPayload);
        var initBuffer = Encoding.UTF8.GetBytes(initJson);
        await ws.SendAsync(
            new ArraySegment<byte>(initBuffer),
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken: CancellationToken.None
        );

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
                await ws.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Inactive socket cleanup.",
                    CancellationToken.None
                );
        }
    }
}