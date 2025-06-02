using System.Diagnostics;
using LifeSim.Utils;
using LifeSim.World;

namespace LifeSim.Network;

public class SimulationLoop(WorldStorage world)
{
    public async Task Start()
    {
        var stopwatch = Stopwatch.StartNew();
        var lastTicks = stopwatch.ElapsedTicks;
        var tickFrequency = (float)Stopwatch.Frequency;

        while (!Program.Cts.IsCancellationRequested)
        {
            var currentTicks = stopwatch.ElapsedTicks;
            var delta = (currentTicks - lastTicks) / tickFrequency;
            lastTicks = currentTicks;

            foreach (var entity in world.AllEntities)
            {
                entity.Update(delta);
            }

            if (world.Foods.Count < 4000)
            {
                var foodAmount = RandomUtils.RNG.Next(0, 6);
                world.SpawnFood(foodAmount, 0, 2048);
            }

            await Task.Delay(16);
        }
    }
}