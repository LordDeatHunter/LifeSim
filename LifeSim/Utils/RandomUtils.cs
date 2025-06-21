namespace LifeSim.Utils;

public static class RandomUtils
{
    public static Random RNG { get; } = new();

    public static float GenerateChaosFloat(this Random random, float baseValue, float chaos, float minMult = 1F, float maxMult = 1F)
    {
        if (chaos <= 0f) return baseValue;

        var min = baseValue / (float)Math.Pow(2, minMult * chaos);
        var max = baseValue * (float)Math.Pow(2, maxMult * chaos);

        double range = max - min;
        var sample = random.NextDouble();
        var result = (float)(min + sample * range);
        return result;
    }
}