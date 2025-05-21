using System.Drawing;
using System.Numerics;
using LifeSim.Data;
using LifeSim.Utils;

namespace LifeSim.Entities;

public class Food : Entity
{
    public Food(Vector2 position) : base(position, Color.FromArgb(0x485D3C), RandomUtils.RNG.NextSingle() * 14F + 2F)
    {
        Program.Chunks[position.ToChunkPosition()].Food.Add(this);
    }

    public override void Update(float deltaTime) { }

    public override void MarkForDeletion()
    {
        base.MarkForDeletion();
        Program.Chunks[Position.ToChunkPosition()].Food.Remove(this);
        Program.Foods.Remove(Id);
    }

    public override IEntityDto ToDTO() => new FoodDto("Food", Id.ToString(), Position.X, Position.Y, Color.ToHex(), Size);
}