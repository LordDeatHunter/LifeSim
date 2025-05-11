using System.Drawing;

namespace LifeSim.Entities;

public abstract class Entity(float x, float y, Color color)
{
    public float X { get; set; } = x;
    public float Y { get; set; } = y;
    public Color Color { get; set; } = color;

    public Entity(float x, float y) : this(x, y, Color.CornflowerBlue) { }

    public Entity() : this(0, 0) { }
    
    public abstract void Update(float deltaTime);
}
