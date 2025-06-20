using System.Drawing;
using System.Numerics;
using LifeSim.Data;
using LifeSim.Utils;

namespace LifeSim.Entities;

public abstract class Entity(Vector2 position, Color color, float size = 8F)
{
    public int Id { get; } = IdUtils.GenerateId();
    private Vector2 _position = position;

    public Vector2 Position
    {
        get => _position;
        set => _position = value.Clamp(new Vector2(0, 0), new Vector2(2048, 2048));
    }

    public Color Color { get; set; } = color;
    public float Size { get; set; } = float.Clamp(size, 2F, 32F);
    public bool MarkedForDeletion { get; private set; }

    public abstract void Update(float deltaTime);

    public virtual void MarkForDeletion()
    {
        MarkedForDeletion = true;
        IdUtils.FreeId(Id);
    }

    public abstract IEntityDto ToDTO();
    
    public abstract float NutritionValue { get; }
}