using System.Collections.Concurrent;
using LifeSim.Data;

namespace LifeSim.Network;

public class LifeSimApi
{
    // TODO: Implement proper classes & storage for these
    public ConcurrentDictionary<string, ulong> Balances { get; } = new();
    public ConcurrentDictionary<string, ConcurrentDictionary<Guid, PendingBet>> Bets { get; } = new();
    public ConcurrentQueue<PendingBet> PendingBets { get; } = new();
    public ConcurrentDictionary<string, string> Names { get; } = new();

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
                foreach (var clientId in Balances.Keys)
                {
                    if (!Balances.TryGetValue(clientId, out var balance))
                    {
                        Balances[clientId] = 0;
                    }

                    if (balance > 0) continue;
                    Balances[clientId] = balance + 100;
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

                while (PendingBets.TryPeek(out var bet) && bet.ExpiresAt <= now)
                {
                    PendingBets.TryDequeue(out bet);
                    var finalCount = Program.World.Animals.Count;

                    if (finalCount == bet.InitialCount)
                    {
                        lock (Balances)
                        {
                            if (Balances.ContainsKey(bet.ClientId))
                                Balances[bet.ClientId] += bet.Amount;
                        }

                        bet.Status = BetStatus.Expired;
                        continue;
                    }

                    var won = bet.BetType == "increase"
                        ? finalCount > bet.InitialCount
                        : finalCount < bet.InitialCount;

                    bet.Status = won ? BetStatus.Won : BetStatus.Lost;

                    if (!won) continue;

                    lock (Balances)
                    {
                        if (Balances.ContainsKey(bet.ClientId))
                            Balances[bet.ClientId] += bet.Amount * 2;
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

    public object GetLeaderboards()
    {
        var wonBets = Bets
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
                name = Names.GetValueOrDefault(kvp.Key, "Unnamed"),
                score = kvp.Value.sum,
                betCount = kvp.Value.count
            })
            .ToList();

        var topBalances = Balances
            .OrderByDescending(kvp => kvp.Value)
            .Take(10)
            .Select(kvp => new
            {
                name = Names.GetValueOrDefault(kvp.Key, "Unnamed"),
                score = kvp.Value
            })
            .ToList();

        return new
        {
            topBets,
            topBalances
        };
    }

    public PendingBet PlaceBet(string clientId, ulong amount, string betType)
    {
        Balances[clientId] -= amount;

        var bet = new PendingBet(
            clientId,
            amount,
            betType,
            Program.World.Animals.Count,
            DateTime.UtcNow.AddSeconds(30)
        );

        PendingBets.Enqueue(bet);
        if (!Bets.TryGetValue(clientId, out var bets))
        {
            bets = new ConcurrentDictionary<Guid, PendingBet>();
            Bets[clientId] = bets;
        }

        bets[bet.Id] = bet;

        return bet;
    }

    public List<BetDto> GetBets(string clientId)
    {
        if (!Bets.TryGetValue(clientId, out var bets))
        {
            bets = new ConcurrentDictionary<Guid, PendingBet>();
            Bets[clientId] = bets;
        }

        return bets.Values
            .Select(bet => new BetDto(bet))
            .OrderBy(bet => bet.ExpiresAt)
            .ToList();
    }

    public object GetOrCreateBalance(string clientId, ulong amount)
    {
        if (Balances.TryGetValue(clientId, out var balance))
            return balance;

        Balances[clientId] = amount;
        return Balances[clientId];
    }

    public ConcurrentDictionary<Guid, PendingBet> GetOrCreateBetsForUser(string clientId)
    {
        if (Bets.TryGetValue(clientId, out var bets))
            return bets;

        bets = new ConcurrentDictionary<Guid, PendingBet>();
        Bets[clientId] = bets;
        return bets;
    }
}