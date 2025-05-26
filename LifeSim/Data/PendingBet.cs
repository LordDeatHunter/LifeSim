namespace LifeSim.Data;

internal record PendingBet(string ClientId, int Amount, string BetType, int InitialCount, DateTime ExpiresAt);
