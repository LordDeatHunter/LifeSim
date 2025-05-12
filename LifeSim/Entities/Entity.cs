using System.Drawing;
using System.Numerics;

namespace LifeSim.Entities;

public abstract class Entity
{
    public Guid Id { get; }
    public Vector2 Position { get; set; }
    public Color Color { get; set; }
    public bool MarkedForDeletion { get; private set; }

    protected Entity(Vector2 position, Color color)
    {
        Id = Guid.NewGuid();
        Position = position;
        Color = color;
    }

    public abstract void Update(float deltaTime);
    
    public virtual void MarkForDeletion()
    {
        MarkedForDeletion = true;
    }
}
