using System.Drawing;
using System.Numerics;

namespace LifeSim.Entities;

public abstract class Entity
{
    public float X { get; set; }
    public float Y { get; set; }
    public Color Color { get; set; }

    protected Entity(float x, float y, Color color)
    {
        X = x;
        Y = y;
        Color = color;

        var position = new Vector2(x, y);
        Program.Chunks[position.ToChunkPosition()].Entities.Add(this);
    }

    public abstract void Update(float deltaTime);
}
