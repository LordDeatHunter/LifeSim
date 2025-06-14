using System.Collections.Concurrent;
using LifeSim.Data;
using LifeSim.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeSim.Network;

public class LifeSimApi
{
    public ConcurrentDictionary<string, string> Names { get; } = new();

    private readonly IServiceScopeFactory _scopeFactory;

    public LifeSimApi(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
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
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var zeroBalances = await db.Balances
                    .Where(b => b.Amount == 0)
                    .ToListAsync();

                foreach (var balance in zeroBalances)
                {
                    balance.Amount = 100;
                }

                if (zeroBalances.Count != 0) await db.SaveChangesAsync();

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
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var expiredPendingBets = await db.ExpiredPendingBetsAsync();
                var finalCount = Program.World.Animals.Count;

                foreach (var bet in expiredPendingBets)
                {
                    var balance = await db.GetOrCreateBalanceAsync(bet.ClientId);

                    if (finalCount == bet.InitialCount)
                    {
                        bet.Status = nameof(BetStatus.Expired);

                        balance.Amount += bet.Amount;

                        continue;
                    }

                    var won = bet.BetType == "increase"
                        ? finalCount > bet.InitialCount
                        : finalCount < bet.InitialCount;

                    bet.Status = won ? nameof(BetStatus.Won) : nameof(BetStatus.Lost);

                    if (!won) continue;

                    balance.Amount += bet.Amount * 2;
                }

                if (expiredPendingBets.Count != 0) await db.SaveChangesAsync();

                await Task.Delay(1000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in HandleBets: " + ex.Message);
        }
    }

    public async Task<object> GetLeaderboardsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var topBets = await db.Bets
            .Where(b => b.Status == nameof(BetStatus.Won))
            .GroupBy(b => b.ClientId)
            .Select(g => new
            {
                Name = g.Key,
                BetCount = g.Count(),
                Score = g.Sum(b => (long)b.Amount)
            })
            .OrderByDescending(x => x.Score)
            .Take(10)
            .ToListAsync();

        var topBalances = await db.Balances
            .OrderByDescending(b => (long)b.Amount)
            .Select(b => new
            {
                Name = b.ClientId,
                Score = b.Amount
            })
            .Take(10)
            .ToListAsync();

        return new
        {
            topBets,
            topBalances
        };
    }

    public async Task<Bet> PlaceBetAsync(string clientId, ulong amount, string betType)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var balance = await db.GetOrCreateBalanceAsync(clientId);
        balance.Amount -= amount;

        var bet = await db.PlaceBetAsync(
            clientId,
            amount,
            betType,
            Program.World.Animals.Count,
            DateTime.UtcNow.AddSeconds(30)
        );

        await db.SaveChangesAsync();

        return bet;
    }

    public async Task<List<Bet>> GetBetsAsync(string clientId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await db.Bets
            .Where(bet => bet.ClientId == clientId)
            .OrderBy(bet => bet.ExpiresAt)
            .ToListAsync();
    }

    public async Task<Balance> GetOrCreateBalanceAsync(string clientId, ulong initialAmount)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
        return await db.GetOrCreateBalanceAsync(clientId, initialAmount);
    }

    public Dictionary<Guid, Bet> GetOrCreateBetsForUser(string clientId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var bets = db.PendingBets()
            .Where(bet => bet.ClientId == clientId)
            .ToDictionary(bet => bet.Id, bet => bet);

        return bets;
    }
}