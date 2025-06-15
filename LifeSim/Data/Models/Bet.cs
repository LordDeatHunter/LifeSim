using System.ComponentModel.DataAnnotations.Schema;

namespace LifeSim.Data.Models;

public class Bet
{
    public Guid Id { get; set; }
    [ForeignKey("User")]
    public string ClientId { get; set; } = null!;
    public ulong Amount { get; set; }
    public string BetType { get; set; } = null!;
    public int InitialCount { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string Status { get; set; } = null!;
}