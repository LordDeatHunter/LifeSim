using System.Drawing;
using System.Numerics;

namespace LifeSim.Entities;

public class Animal : Entity
{
    private const float Speed = 10f;
    private readonly float EatDistance = 5f;

    public Animal(Vector2 position) : base(position, Color.CornflowerBlue)
    {
        Program.Chunks[position.ToChunkPosition()].Animals.Add(this);
    }

    public override void Update(float deltaTime)
    {
        if (MarkedForDeletion) return;

        var prevPosition = Position;
        var prevChunkPosition = prevPosition.ToChunkPosition();

        var nearestFood = FindNearestFood();
        if (nearestFood == null) return;
        var direction = Vector2.Normalize(nearestFood.Position - prevPosition);

        Position += direction * Speed * deltaTime;
        Position = Position.Clamp(new Vector2(0, 0), new Vector2(1024, 1024));

        if (Vector2.Distance(Position, nearestFood.Position) <= EatDistance)
        {
            nearestFood.MarkForDeletion();
        }

        var newChunkPosition = Position.ToChunkPosition();

        if (prevChunkPosition == newChunkPosition) return;
        Program.Chunks[prevChunkPosition].Animals.Remove(this);
        Program.Chunks[newChunkPosition].Animals.Add(this);
    }

    private Food? FindNearestFood()
    {
        Food nearestFood = null;
        var nearestDistance = float.MaxValue;

        return Program.Foods.Values.Where(f => !f.MarkedForDeletion).OrderBy(f => Vector2.Distance(Position, f.Position)).FirstOrDefault();
    }

    public override void MarkForDeletion()
    {
        base.MarkForDeletion();
        Program.Chunks[Position.ToChunkPosition()].Animals.Remove(this);
        Program.Animals.Remove(Id);
    }
}
