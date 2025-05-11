using System.Numerics;
using LifeSim.World;

namespace LifeSim;

public static class Vector2Utils
{
    public static Vector2 ToChunkPosition(this Vector2 position) => new((int)(position.X / Chunk.Size), (int)(position.Y / Chunk.Size));
}