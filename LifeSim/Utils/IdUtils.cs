namespace LifeSim.Utils;

public static class IdUtils
{
    // ID shifted from 1 to int.MaxValue (0 is reserved for no entity)
    private const int MaxId = 100_000;
    private static readonly bool[] UsedIds = new bool[MaxId];
    private static readonly Lock IdsLock = new();
    private static int _counter;

    static IdUtils()
    {
        for (var i = 0; i < MaxId; i++)
        {
            UsedIds[i] = false;
        }
    }

    public static int GenerateId()
    {
        int id;
        lock (IdsLock)
        {
            // If stuck in an infinite loop, it's because all ids are used.
            // Try a bigger array (make maxId bigger) and refactor some logic because the counter is unsigned
            while (UsedIds[_counter]) _counter = (_counter + 1) % MaxId;
            id = _counter;
            UsedIds[_counter] = true;
            _counter = (_counter + 1) % MaxId;
        }

        return id + 1;
    }

    public static void FreeId(int id)
    {
        lock (IdsLock) UsedIds[id - 1] = false;
    }
}