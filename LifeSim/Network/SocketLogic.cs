using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace LifeSim.Network;

public class SocketLogic
{
    public ConcurrentDictionary<WebSocket, byte> Clients { get; } = new();
    public async Task HandleWebSocket(HttpContext context)
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
    }
}