using System.Collections.Concurrent;
using System.Text.Json;
using LifeSim.Data;
using LifeSim.Utils;
using Microsoft.AspNetCore.Mvc;

namespace LifeSim.Network;

public class LifeSimApi
{
    // TODO: Implement proper classes & storage for these
    private readonly ConcurrentDictionary<string, ulong> _balances = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, PendingBet>> _bets = new();
    private readonly ConcurrentQueue<PendingBet> _pendingBets = new();
    private readonly ConcurrentDictionary<string, string> _names = new();

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
                foreach (var clientId in _balances.Keys)
                {
                    if (!_balances.TryGetValue(clientId, out var balance))
                    {
                        _balances[clientId] = 0;
                    }

                    if (balance > 0) continue;
                    _balances[clientId] = balance + 100;
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
                        lock (_balances)
                        {
                            if (_balances.ContainsKey(bet.ClientId))
                                _balances[bet.ClientId] += bet.Amount;
                        }

                        bet.Status = BetStatus.Expired;
                        continue;
                    }

                    var won = bet.BetType == "increase"
                        ? finalCount > bet.InitialCount
                        : finalCount < bet.InitialCount;

                    bet.Status = won ? BetStatus.Won : BetStatus.Lost;

                    if (!won) continue;

                    lock (_balances)
                    {
                        if (_balances.ContainsKey(bet.ClientId))
                            _balances[bet.ClientId] += bet.Amount * 2;
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

    public async Task GetLeaderboards(HttpContext context)
    {
        var wonBets = _bets
            .SelectMany(kvp => kvp.Value.Values.Where(bet => bet.Status == BetStatus.Won))
            .GroupBy(bet => bet.ClientId)
            .ToDictionary(g => g.Key, g =>
                new
                {
                    count = g.Count(),
                    sum = g.Aggregate(0UL, (acc, bet) => acc + bet.Amount)
                });

        var topBets = wonBets
            .OrderByDescending(kvp => kvp.Value.sum)
            .Take(10)
            .Select(kvp => new
            {
                name = _names.GetValueOrDefault(kvp.Key, "Unnamed"),
                score = kvp.Value.sum,
                betCount = kvp.Value.count
            })
            .ToList();

        var topBalances = _balances
            .OrderByDescending(kvp => kvp.Value)
            .Take(10)
            .Select(kvp => new
            {
                name = _names.GetValueOrDefault(kvp.Key, "Unnamed"),
                score = kvp.Value
            })
            .ToList();

        var response = new
        {
            topBets,
            topBalances
        };

        await context.Response.WriteAsJsonAsync(response);
    }

    public Task ReigniteLifeHandler()
    {
        if (!Program.World.Animals.IsEmpty) return Task.CompletedTask;

        var animalCount = RandomUtils.RNG.Next(4, 16);

        Program.World.SpawnAnimals(animalCount, 350, 650);
        Program.ReignitionCount += 1;

        return Task.CompletedTask;
    }

    public async Task GetBalance(HttpContext context)
    {
        var clientId = ClientId.GetClientId(context);
        if (string.IsNullOrEmpty(clientId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (!_balances.TryGetValue(clientId, out var balance))
        {
            balance = 100;
            _balances[clientId] = balance;
        }

        context.Response.ContentType = "application/json";
        var response = new
        {
            balance
        };
        await context.Response.WriteAsJsonAsync(response);
    }

    public async Task PlaceBet(HttpContext context)
    {
        var clientId = ClientId.GetClientId(context);
        if (string.IsNullOrEmpty(clientId) || !_balances.TryGetValue(clientId, out var balance) || balance <= 0)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        ulong amount;
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

            if (json.TryGetValue("amount", out var amtElem) && amtElem.TryGetUInt64(out var val))
            {
                amount = val;
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            betType = json.TryGetValue("betType", out var typeElem) && typeElem.ValueKind == JsonValueKind.String
                ? typeElem.GetString() ?? string.Empty
                : string.Empty;

            if (amount < 1 || amount > balance || string.IsNullOrEmpty(betType) ||
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

        _balances[clientId] -= amount;

        var bet = new PendingBet(
            clientId,
            amount,
            betType,
            Program.World.Animals.Count,
            DateTime.UtcNow.AddSeconds(30)
        );

        _pendingBets.Enqueue(bet);
        if (!_bets.TryGetValue(clientId, out var bets))
        {
            bets = new ConcurrentDictionary<Guid, PendingBet>();
            _bets[clientId] = bets;
        }

        bets[bet.Id] = bet;

        context.Response.ContentType = "application/json";
        var response = new
        {
            balance = _balances[clientId],
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

        if (!_bets.TryGetValue(clientId, out var bets))
        {
            bets = new ConcurrentDictionary<Guid, PendingBet>();
            _bets[clientId] = bets;
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
        if (string.IsNullOrEmpty(clientId) || !_bets.TryGetValue(clientId, out var bets) ||
            !bets.TryGetValue(id, out var bet))
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

    public async Task SetName(HttpContext context)
    {
        var clientId = ClientId.GetClientId(context);
        if (string.IsNullOrEmpty(clientId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        string name;

        try
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);

            if (json == null || !json.TryGetValue("name", out var nameElem) ||
                nameElem.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(nameElem.GetString()))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            name = nameElem.GetString()!.Trim().Replace(" ", "_");

            if (_names.TryGetValue(clientId, out var value) && value == name)
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }

            var originalNameLength = name.Length;
            if (originalNameLength is < 3 or > 20)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            for (var i = 1; _names.Values.Contains(name); i++)
            {
                name = $"{name[..(originalNameLength)]}_{i}";
            }
        }
        catch
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return;
        }

        _names[clientId] = name;
        context.Response.StatusCode = StatusCodes.Status204NoContent;
    }
}