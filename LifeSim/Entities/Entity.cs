using System.Drawing;
using System.Numerics;
using LifeSim.Components;

namespace LifeSim.Entities;

public abstract class Entity(Vector2 position, Color color, float size = 8F)
{
    public Guid Id { get; } = Guid.NewGuid();
    private Vector2 _position = position;
    public Vector2 Position
    {
        get => _position;
        set => _position = value.Clamp(new Vector2(0, 0), new Vector2(1024, 1024));
    }
    public Color Color { get; set; } = color;
    public float Size { get; set; } = float.Clamp(size, 2F, 64F);
    public bool MarkedForDeletion { get; private set; }
    protected readonly List<IComponent> Components = [];

    public abstract void Update(float deltaTime);
    
    public virtual void MarkForDeletion()
    {
        MarkedForDeletion = true;
    }
}
