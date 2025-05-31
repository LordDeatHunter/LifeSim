namespace LifeSim.Data;

public class PendingBet(string clientId, ulong amount, string betType, int initialCount, DateTime expiresAt)
{
    public readonly Guid Id = Guid.NewGuid();
    public readonly string ClientId = clientId;
    public readonly ulong Amount = amount;
    public readonly string BetType = betType;
    public readonly int InitialCount = initialCount;
    public readonly DateTime ExpiresAt = expiresAt;
    public BetStatus Status { get; set; } = BetStatus.Pending;
}
