using System.Collections.Concurrent;
using System.Text.Json;
using LifeSim.Data;
using LifeSim.Utils;
using Microsoft.AspNetCore.Mvc;

namespace LifeSim.Network;

public class LifeSimApi
{
    private readonly ConcurrentDictionary<string, int> Currencies = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, PendingBet>> Bets = new();
    private readonly ConcurrentQueue<PendingBet> _pendingBets = new();

    public LifeSimApi()
    {
        Task.Run(HandleBets);
        Task.Run(GiveOutWelfare);
    }

    public async void GiveOutWelfare()
    {
        try
        {
            var now = DateTime.UtcNow;
            var nextHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(0.5);
            if (nextHour <= now) nextHour = nextHour.AddHours(0.5);
            var timeToNextHour = nextHour - now;

            await Task.Delay(timeToNextHour);

            while (true)
            {
                foreach (var clientId in Currencies.Keys)
                {
                    if (!Currencies.TryGetValue(clientId, out var currency))
                    {
                        Currencies[clientId] = 0;
                    }

                    if (currency > 0) continue;
                    Currencies[clientId] = currency + 100;
                }

                await Task.Delay(TimeSpan.FromMinutes(30));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in GiveOutWelfare: " + ex.Message);
        }
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

                        bet.Status = BetStatus.Expired;
                        continue;
                    }

                    var won = bet.BetType == "increase"
                        ? finalCount > bet.InitialCount
                        : finalCount < bet.InitialCount;

                    bet.Status = won ? BetStatus.Won : BetStatus.Lost;

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

    public async Task PlaceBet(HttpContext context)
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

            if (amount <= 0 || amount > currency || string.IsNullOrEmpty(betType) ||
                (betType != "increase" && betType != "decrease"))
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

        var bet = new PendingBet(
            clientId,
            amount,
            betType,
            Program.World.Animals.Count,
            DateTime.UtcNow.AddSeconds(30)
        );

        _pendingBets.Enqueue(bet);
        if (!Bets.TryGetValue(clientId, out var bets))
        {
            bets = new ConcurrentDictionary<Guid, PendingBet>();
            Bets[clientId] = bets;
        }

        bets[bet.Id] = bet;

        context.Response.ContentType = "application/json";
        var response = new
        {
            currency = Currencies[clientId],
            bet = new
            {
                id = bet.Id,
                amount = bet.Amount,
                betType = bet.BetType,
                initialCount = bet.InitialCount,
                expiresAt = bet.ExpiresAt,
                status = bet.Status.ToString()
            }
        };
        await context.Response.WriteAsJsonAsync(response);
    }

    public async Task GetBets(HttpContext context)
    {
        var clientId = ClientId.GetClientId(context);
        if (string.IsNullOrEmpty(clientId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (!Bets.TryGetValue(clientId, out var bets))
        {
            bets = new ConcurrentDictionary<Guid, PendingBet>();
            Bets[clientId] = bets;
        }

        var mappedBets = bets.Values
            .Select(bet => new
            {
                id = bet.Id,
                amount = bet.Amount,
                betType = bet.BetType,
                initialCount = bet.InitialCount,
                expiresAt = bet.ExpiresAt,
                status = bet.Status.ToString()
            })
            .OrderBy(bet => bet.expiresAt)
            .ToList();

        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(mappedBets);
    }

    public async Task GetBetById(HttpContext context, Guid id)
    {
        var clientId = ClientId.GetClientId(context);
        if (string.IsNullOrEmpty(clientId) || !Bets.TryGetValue(clientId, out var bets) || !bets.TryGetValue(id, out var bet))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            id = bet.Id,
            amount = bet.Amount,
            betType = bet.BetType,
            initialCount = bet.InitialCount,
            expiresAt = bet.ExpiresAt,
            status = bet.Status.ToString()
        });
    }
}