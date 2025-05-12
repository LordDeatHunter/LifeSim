using System.Numerics;
using LifeSim.Entities;

namespace LifeSim.World;

public class Chunk(Vector2 position)
{
    public Vector2 Position { get; } = position;
    public const int Size = 32;
    public HashSet<Food> Food { get; } = [];
    public HashSet<Animal> Animals { get; } = [];
}