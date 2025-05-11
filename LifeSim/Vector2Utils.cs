using System.Numerics;
using LifeSim.World;

namespace LifeSim;

public static class Vector2Utils
{
    public static Vector2 ToChunkPosition(this Vector2 position) => new((int)(position.X / Chunk.Size), (int)(position.Y / Chunk.Size));
    public static Vector2 Clamp(this Vector2 vector, Vector2 min, Vector2 max)
    {
        vector.X = float.Clamp(vector.X, min.X, max.X);
        vector.Y = float.Clamp(vector.Y, min.Y, max.Y);
        return vector;
    }
}