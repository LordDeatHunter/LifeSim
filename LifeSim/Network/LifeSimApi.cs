using LifeSim.Data;
using LifeSim.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeSim.Network;

public class LifeSimApi
{
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
                var brokeUsers = await db.Users
                    .Where(u => u.Balance == 0)
                    .ToListAsync();

                foreach (var user in brokeUsers)
                {
                    user.Balance = 100;
                }

                if (brokeUsers.Count != 0) await db.SaveChangesAsync();

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
                    var user = await db.GetOrCreateUserAsync(bet.ClientId);

                    if (finalCount == bet.InitialCount)
                    {
                        bet.Status = nameof(BetStatus.Expired);

                        user.Balance += bet.Amount;

                        continue;
                    }

                    var won = bet.BetType == "increase"
                        ? finalCount > bet.InitialCount
                        : finalCount < bet.InitialCount;

                    bet.Status = won ? nameof(BetStatus.Won) : nameof(BetStatus.Lost);

                    if (!won) continue;

                    user.Balance += bet.Amount * 2;
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
                ClientId = g.Key,
                BetCount = g.Count(),
                Score = g.Sum(b => (long)b.Amount)
            })
            .OrderByDescending(x => x.Score)
            .Take(10)
            .Join(
                db.Users,
                bet => bet.ClientId,
                user => user.ClientId,
                (bet, user) => new
                {
                    user.Name,
                    bet.BetCount,
                    bet.Score
                }
            )
            .ToListAsync();

        var topBalances = await db.Users
            .Select(u => new
            {
                u.Name,
                Score = u.Balance
            })
            .OrderByDescending(x => (long)x.Score)
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

        var user = await db.GetOrCreateUserAsync(clientId);
        user.Balance -= amount;

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

    public async Task<User> GetOrCreateUserAsync(string clientId, ulong initialBalance = 100)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.GetOrCreateUserAsync(clientId, initialBalance);
    }

    public async Task<Bet?> GetBetByIdAsync(Guid id)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Bets.FindAsync(id);
    }

    public async Task<bool> IsNameTakenAsync(string name)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Users.AnyAsync(u => u.Name == name);
    }

    public async Task UpdateUserAsync(User user)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }
}