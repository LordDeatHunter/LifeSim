using System.Drawing;
using System.Numerics;
using LifeSim.Data;
using LifeSim.Utils;

namespace LifeSim.Entities;

public abstract class Entity(Vector2 position, Color color, float size = 8F)
{
    public int Id { get; } = IdUtils.GenerateId();

    public Vector2 Position
    {
        get;
        protected set => field = value.Clamp(new Vector2(0, 0), new Vector2(2048, 2048));
    } = position;

    public Color Color { get; set; } = color;
    public float Size { get; set; } = float.Clamp(size, 2F, 32F);
    public bool MarkedForDeletion { get; private set; }
    public bool Infected { get; set; }

    public abstract void Update(float deltaTime);

    public virtual void MarkForDeletion()
    {
        MarkedForDeletion = true;
        IdUtils.FreeId(Id);
    }

    public abstract IEntityDto ToDTO();
    
    public abstract float NutritionValue { get; }
}
