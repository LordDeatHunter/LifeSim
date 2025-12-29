using System.Drawing;
using System.Numerics;
using LifeSim.Data;
using LifeSim.Utils;

namespace LifeSim.Entities;

public class Food : Entity
{
    private float _age;
    public float Lifespan { get; set; }

    public float Age {
        get => _age;
        set
        {
            _age = value;
            if (_age >= Lifespan) MarkForDeletion();
        }
    }

    public Food(Vector2 position) : base(position, Color.FromArgb(0x485D3C), RandomUtils.RNG.NextSingle() * 14F + 2F)
    {
        Lifespan = 24F + RandomUtils.RNG.NextSingle() * 24F + Size;
    }

    public override void Update(float deltaTime)
    {
        Age += deltaTime;
    }

    public override void MarkForDeletion()
    {
        base.MarkForDeletion();
        Program.World.EnqueueFoodDeletion(this);
    }

    public override IEntityDto ToDTO() => new FoodDto(Id.ToString(), Position.X, Position.Y, Color.ToHex(), Size, Infected);

    public override float NutritionValue => Size / 2F;
}