using LifeSim.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeSim.Data;

public static class ApplicationDbContextExtensions
{
    public static async Task<User> GetOrCreateUserAsync(this ApplicationDbContext db, string clientId, ulong initialBalance = 100)
    {
        var user = await db.Users.FindAsync(clientId);
        if (user != null) return user;

        user = new User
        {
            ClientId = clientId,
            Name     = clientId,
            Balance  = initialBalance
        };
        db.Users.Add(user);

        await db.SaveChangesAsync();

        return user;
    }

    public static async Task<Bet> PlaceBetAsync(
        this ApplicationDbContext db,
        string clientId,
        ulong amount,
        string betType,
        int initialCount,
        DateTime expiresAt
    )
    {
        var bet = new Bet
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
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