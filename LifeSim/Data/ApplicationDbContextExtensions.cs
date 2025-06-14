using LifeSim.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeSim.Data;

public static class ApplicationDbContextExtensions
{
    public static async Task<Balance> GetOrCreateBalanceAsync(
        this ApplicationDbContext db,
        string clientId,
        ulong initialAmount = 100
    )
    {
        var bal = await db.Balances.SingleOrDefaultAsync(b => b.ClientId == clientId);

        if (bal != null) return bal;

        bal = new Balance
        {
            ClientId = clientId,
            Amount = initialAmount
        };
        await db.Balances.AddAsync(bal);

        return bal;
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
          
    public static async Task<Dictionary<string, ulong>> GetBalancesAsync(this ApplicationDbContext db)
    {
        return await db.Balances
            .ToDictionaryAsync(b => b.ClientId, b => b.Amount);
    }
}