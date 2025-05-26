namespace LifeSim.Data;

public class PendingBet(string clientId, int amount, string betType, int initialCount, DateTime expiresAt)
{
    public readonly Guid Id = Guid.NewGuid();
    public readonly string ClientId = clientId;
    public readonly int Amount = amount;
    public readonly string BetType = betType;
    public readonly int InitialCount = initialCount;
    public readonly DateTime ExpiresAt = expiresAt;
    public BetStatus Status { get; set; } = BetStatus.Pending;
}
