using System.Drawing;
using System.Numerics;

namespace LifeSim.Entities;

public class Food : Entity
{
    public Food(Vector2 position) : base(position, Color.Crimson)
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
}