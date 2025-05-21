using System.Numerics;
using LifeSim.Utils;

namespace LifeSim.Network;

public class LifeSimApi
{
    public Task ReigniteLifeHandler()
    {
        if (Program.World.Animals.Count > 0) return Task.CompletedTask;

        var animalCount = RandomUtils.RNG.Next(4, 16);

        Program.World.SpawnAnimals(animalCount, 350, 650);

        return Task.CompletedTask;
    }
}