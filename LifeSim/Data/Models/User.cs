using System.ComponentModel.DataAnnotations;

namespace LifeSim.Data.Models;

public class User
{
    [Key]
    public string ClientId { get; set; } = null!;

    [Required, StringLength(20, MinimumLength = 3)]
    public string Name { get; set; } = null!;

    public ulong Balance { get; set; }

    public ICollection<Bet> Bets { get; set; } = new List<Bet>();
}