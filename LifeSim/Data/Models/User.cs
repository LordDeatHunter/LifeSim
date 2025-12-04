using System.ComponentModel.DataAnnotations;

namespace LifeSim.Data.Models;

public class User
{
    [Key]
    public string DiscordId { get; set; } = null!;

    [Required]
    public string DiscordUsername { get; set; } = null!;

    public string? DiscordAvatar { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public ulong Balance { get; set; }

    public ICollection<Bet> Bets { get; set; } = new List<Bet>();
}