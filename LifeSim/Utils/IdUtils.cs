namespace LifeSim.Utils;

public static class IdUtils
{
    // ID shifted from 1 to 65_534 (0 is reserved for no entity)
    private const int MaxId = ushort.MaxValue;
    private static readonly bool[] UsedIds = new bool[MaxId];
    private static readonly Lock IdsLock = new();
    private static ushort _counter;

    static IdUtils()
    {
        for (var i = 0; i < MaxId - 1; i++)
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
            UsedIds[_counter++] = true;
        }

        return (ushort)(id + 1);
    }

    public static void FreeId(ushort id)
    {
        lock (IdsLock) UsedIds[id - 1] = false;
    }
}