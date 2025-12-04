namespace LifeSim.Data.Models;

public class Bet
{
    public Guid Id { get; set; }
    public string DiscordId { get; set; } = null!;
    public ulong Amount { get; set; }
    public string BetType { get; set; } = null!;
    public int InitialCount { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string Status { get; set; } = null!;
}