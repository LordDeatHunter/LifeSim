namespace LifeSim.Network;

public class StatisticsTracker
{
    private readonly Lock _lock = new();
    private readonly List<StatisticsSnapshot> _snapshots = [];
    private readonly TimeSpan _retentionPeriod = TimeSpan.FromMinutes(1);

    public void RecordSnapshot(int animalCount, int foodCount, int infectedAnimalCount, int infectedFoodCount)
    {
        var snapshot = new StatisticsSnapshot
        {
            Timestamp = DateTime.UtcNow,
            AnimalCount = animalCount,
            FoodCount = foodCount,
            InfectedAnimalCount = infectedAnimalCount,
            InfectedFoodCount = infectedFoodCount
        };

        lock (_lock)
        {
            _snapshots.Add(snapshot);

            var cutoff = DateTime.UtcNow - _retentionPeriod;
            _snapshots.RemoveAll(s => s.Timestamp < cutoff);
        }
    }

    public List<StatisticsSnapshot> GetAllSnapshots()
    {
        lock (_lock) return [.._snapshots];
    }

    public List<StatisticsSnapshot> GetSnapshotsSince(DateTime since)
    {
        lock (_lock) return _snapshots.Where(s => s.Timestamp > since).ToList();
    }
}

public class StatisticsSnapshot
{
    public DateTime Timestamp { get; init; }
    public int AnimalCount { get; init; }
    public int FoodCount { get; init; }
    public int InfectedAnimalCount { get; init; }
    public int InfectedFoodCount { get; init; }
}
