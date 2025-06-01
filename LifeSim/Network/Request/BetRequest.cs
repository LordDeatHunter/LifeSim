namespace LifeSim.Network.Request;

public class BetRequest
{
    public ulong Amount { get; set; }
    public string BetType { get; set; } = string.Empty;
}