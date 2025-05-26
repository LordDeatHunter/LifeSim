using System.Collections.Concurrent;
using System.Text.Json;
using LifeSim.Data;
using LifeSim.Utils;

namespace LifeSim.Network;

public class LifeSimApi
{
    private readonly ConcurrentDictionary<string?, int> Currencies = new();
    private readonly ConcurrentQueue<PendingBet> _pendingBets = new();

    public LifeSimApi()
    {

        Task.Run(HandleBets);
    }
    
    public async void HandleBets()
    {
        try
        {
            while (true)
            {
                var now = DateTime.UtcNow;

                while (_pendingBets.TryPeek(out var bet) && bet.ExpiresAt <= now)
                {
                    _pendingBets.TryDequeue(out bet);
                    var finalCount = Program.World.Animals.Count;

                    if (finalCount == bet.InitialCount)
                    {
                        lock (Currencies)
                        {
                            if (Currencies.ContainsKey(bet.ClientId))
                                Currencies[bet.ClientId] += bet.Amount;
                        }
                        continue;
                    }

                    var won = bet.BetType == "increase"
                        ? finalCount > bet.InitialCount
                        : finalCount < bet.InitialCount;

                    if (!won) continue;

                    lock (Currencies)
                    {
                        if (Currencies.ContainsKey(bet.ClientId))
                            Currencies[bet.ClientId] += bet.Amount * 2;
                    }
                }

                await Task.Delay(1000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in HandleBets: " + ex.Message);
        }
    }

    public Task ReigniteLifeHandler()
    {
        if (!Program.World.Animals.IsEmpty) return Task.CompletedTask;

        var animalCount = RandomUtils.RNG.Next(4, 16);

        Program.World.SpawnAnimals(animalCount, 350, 650);
        Program.ReignitionCount += 1;

        return Task.CompletedTask;
    }

    public async Task GetCurrency(HttpContext context)
    {
        var clientId = ClientId.GetClientId(context);
        if (string.IsNullOrEmpty(clientId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (!Currencies.TryGetValue(clientId, out var currency))
        {
            currency = 100;
            Currencies[clientId] = currency;
        }

        context.Response.ContentType = "application/json";
        var response = new
        {
            currency
        };
        await context.Response.WriteAsJsonAsync(response);
    }

    public async Task Bet(HttpContext context)
    {
        var clientId = ClientId.GetClientId(context);
        if (string.IsNullOrEmpty(clientId) || !Currencies.TryGetValue(clientId, out var currency) || currency <= 0)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        int amount;
        string betType;

        try
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);

            if (json == null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            amount = json.TryGetValue("amount", out var amtElem) && amtElem.TryGetInt32(out var val)
                ? val
                : -1;
            betType = json.TryGetValue("betType", out var typeElem) && typeElem.ValueKind == JsonValueKind.String
                ? typeElem.GetString() ?? string.Empty
                : string.Empty;

            if (amount <= 0 || amount > currency || string.IsNullOrEmpty(betType) || (betType != "increase" && betType != "decrease"))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
        }
        catch
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return;
        }

        Currencies[clientId] -= amount;

        _pendingBets.Enqueue(new PendingBet(
            clientId,
            amount,
            betType,
            Program.World.Animals.Count,
            DateTime.UtcNow.AddSeconds(30)
        ));

        context.Response.ContentType = "application/json";
        var response = new { currency = Currencies[clientId] };
        await context.Response.WriteAsJsonAsync(response);
    }
}