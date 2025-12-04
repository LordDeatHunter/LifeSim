using LifeSim.Data.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LifeSim.Data;

public static class ApplicationDbContextExtensions
{
    private const int MaxRetries = 5;
    private const int RetryDelayMs = 100;

    public static async Task SaveChangesWithRetryAsync(this ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < MaxRetries; i++)
        {
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 5 or 6 }) // 5 = locked, 6 = busy
            {
                if (i == MaxRetries - 1)
                    throw;

                await Task.Delay(RetryDelayMs * (i + 1), cancellationToken);
            }
        }
    }

    public static async Task<User?> GetUserAsync(this ApplicationDbContext db, string discordId) => await db.Users.FindAsync(discordId);

    public static async Task<Bet> PlaceBetAsync(
        this ApplicationDbContext db,
        string discordId,
        ulong amount,
        string betType,
        int initialCount,
        DateTime expiresAt
    )
    {
        var bet = new Bet
        {
            Id = Guid.NewGuid(),
            DiscordId = discordId,
            Amount = amount,
            BetType = betType,
            InitialCount = initialCount,
            ExpiresAt = expiresAt,
            Status = nameof(BetStatus.Pending)
        };
        await db.Bets.AddAsync(bet);
        return bet;
    }

    public static IQueryable<Bet> PendingBets(this ApplicationDbContext db) =>
        db.Bets.Where(b => b.Status == "Pending");

    public static Task<List<Bet>> ExpiredPendingBetsAsync(this ApplicationDbContext db)
    {
        var now = DateTime.UtcNow;

        return db.PendingBets()
            .Where(b => b.ExpiresAt <= now && b.Status == nameof(BetStatus.Pending))
            .ToListAsync();
    }
}