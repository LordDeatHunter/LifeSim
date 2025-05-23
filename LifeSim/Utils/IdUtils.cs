using System.Collections.Concurrent;

namespace LifeSim.Utils;

public static class IdUtils
{
    public static ConcurrentDictionary<UInt16, byte> FreeIds = new();
    static IdUtils()
    {
        for (var i = 0; i < UInt16.MaxValue; i++)
        {
            FreeIds[(UInt16)i] = 0;
        }
    }
    public static ConcurrentDictionary<UInt16, byte> UsedIds = new();

    public static UInt16 GenerateId()
    {
        if (!FreeIds.IsEmpty)
        {
            var id = FreeIds.Keys.First();
            FreeIds.TryRemove(id, out _);
            UsedIds[id] = 0;
            return id;
        }

        var idToUse = (UInt16)UsedIds.Count;
        UsedIds[idToUse] = 0;
        return idToUse;
    }

    public static void FreeId(UInt16 id)
    {
        UsedIds.TryRemove(id, out _);
        FreeIds[id] = 0;
    }

}