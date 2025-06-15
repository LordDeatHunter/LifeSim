using System.Diagnostics;
using LifeSim.Utils;
using LifeSim.World;

namespace LifeSim.Network;

public class SimulationLoop(WorldStorage world)
{
    private float _dbUpdateDelta;

    public async Task Start()
    {
        var stopwatch = Stopwatch.StartNew();
        var lastTicks = stopwatch.ElapsedTicks;
        var tickFrequency = (float)Stopwatch.Frequency;

        while (!Program.Cts.IsCancellationRequested)
        {
            try
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

                _dbUpdateDelta += delta;
                if (_dbUpdateDelta >= 1.0F)
                {
                    _dbUpdateDelta -= 1.0F;
                    await Program.World.UpdateDbEntitiesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in simulation loop: " + ex);
            }

            await Task.Delay(16);
        }
    }
}