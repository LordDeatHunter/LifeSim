using System.Collections.Concurrent;

namespace LifeSim.Utils;

public static class IdUtils
{
    private static readonly int maxId = 65535;
    public static readonly bool[] UsedIds = new bool[maxId];
    public static UInt16 counter = 0;
    public static Lock idsLock = new();

    static IdUtils()
    {
        for (var i = 0; i < maxId; i++)
        {
            UsedIds[i] = false;
        }
    }

    public static UInt16 GenerateId()
    {
        UInt16 id;
        lock (idsLock)
        {
            // If stuck in an infinite loop it's because all ids are used
            // try bigger array (make maxId bigger) and refactor some logic because counter is unsigned
            while (UsedIds[counter]) counter++;
            id = counter;
            UsedIds[counter] = true;
        }
        return id;
    }

    public static void FreeId(UInt16 id)
    {
        UsedIds[id] = false;
    }
}