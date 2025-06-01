namespace LifeSim.Data;

public class BetDto(PendingBet bet)
{
    public Guid Id { get; } = bet.Id;
    public string ClientId { get; } = bet.ClientId;
    public ulong Amount { get; } = bet.Amount;
    public string BetType { get; } = bet.BetType;
    public int InitialCount { get; } = bet.InitialCount;
    public DateTime ExpiresAt { get; } = bet.ExpiresAt;
    public string Status { get; set; } = bet.Status.ToString();
}