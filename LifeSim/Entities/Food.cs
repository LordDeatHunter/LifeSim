using System.Drawing;
using System.Numerics;
using LifeSim.Data;
using LifeSim.Utils;

namespace LifeSim.Entities;

public class Food : Entity
{
    private float _age = 0F;
    private float _lifespan;
    public float Age {
        get => _age;
        set
        {
            _age = value;
            if (_age >= _lifespan) MarkForDeletion();
        }
    }

    public Food(Vector2 position) : base(position, Color.FromArgb(0x485D3C), RandomUtils.RNG.NextSingle() * 14F + 2F)
    {
        Program.World.Chunks[position.ToChunkPosition()].Food.Add(this);
        _lifespan = 24F + RandomUtils.RNG.NextSingle() * 24F + Size;
    }

    public override void Update(float deltaTime)
    {
        Age += deltaTime;
    }

    public override void MarkForDeletion()
    {
        base.MarkForDeletion();
        Program.World.Chunks[Position.ToChunkPosition()].Food.Remove(this);
        Program.World.Foods.TryRemove(Id, out _);
    }

    public override IEntityDto ToDTO() => new FoodDto(Id.ToString(), Position.X, Position.Y, Color.ToHex(), Size);
}