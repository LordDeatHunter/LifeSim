using System.Diagnostics;
using LifeSim.Utils;
using LifeSim.World;

namespace LifeSim.Network;

public class SimulationLoop(WorldStorage world)
{
    private bool _wasAlive;

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

                var currentLifeDuration = Program.LastReignitionTime != DateTime.MinValue
                    ? (DateTime.UtcNow - Program.LastReignitionTime).TotalMilliseconds
                    : 0;
                if (currentLifeDuration > Program.LongestLifeDuration.TotalMilliseconds)
                    Program.LongestLifeDuration = TimeSpan.FromMilliseconds(currentLifeDuration);

                var hasAnimals = !world.Animals.IsEmpty;
                if (_wasAlive && !hasAnimals)
                    Program.LastReignitionTime = DateTime.MinValue;

                _wasAlive = hasAnimals;

                if (world.Foods.Count < 4000)
                {
                    var foodAmount = RandomUtils.RNG.Next(0, 6);
                    world.SpawnFood(foodAmount, 0, 2048);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in simulation loop: " + ex);
            }

            await Task.Delay(16, Program.Cts.Token);
        }
    }
}
