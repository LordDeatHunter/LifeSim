using System.Drawing;
using System.Numerics;

namespace LifeSim.Entities;

public abstract class Entity
{
    public Vector2 Position { get; set; }
    public Color Color { get; set; }

    protected Entity(Vector2 position, Color color)
    {
        Position = position;
        Color = color;
        Program.Chunks[position.ToChunkPosition()].Entities.Add(this);
    }

    public abstract void Update(float deltaTime);
}
