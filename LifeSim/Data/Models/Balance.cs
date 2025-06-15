namespace LifeSim.Data.Models;

public class Balance
{
    public int Id { get; set; }
    public string ClientId { get; set; } = null!;
    public ulong Amount { get; set; }
}
