using System.Numerics;
using LifeSim.Entities;

namespace LifeSim.World;

public class Chunk(Vector2 position)
{
    public Vector2 Position { get; } = position;
    public const int Size = 128;
    public HashSet<Entity> Entities { get; } = [];
}