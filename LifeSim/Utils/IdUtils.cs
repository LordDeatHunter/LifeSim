namespace LifeSim.Utils;

public static class IdUtils
{
    private const int MaxId = ushort.MaxValue + 1;
    private static readonly bool[] UsedIds = new bool[MaxId];
    private static readonly Lock IdsLock = new();
    private static ushort _counter;

    static IdUtils()
    {
        for (var i = 0; i < MaxId; i++)
        {
            UsedIds[i] = false;
        }
    }

    public static ushort GenerateId()
    {
        ushort id;
        lock (IdsLock)
        {
            // If stuck in an infinite loop, it's because all ids are used.
            // Try a bigger array (make maxId bigger) and refactor some logic because the counter is unsigned
            while (UsedIds[_counter]) _counter++;
            id = _counter;
            UsedIds[_counter] = true;
        }

        return id;
    }

    public static void FreeId(ushort id)
    {
        UsedIds[id] = false;
    }
}