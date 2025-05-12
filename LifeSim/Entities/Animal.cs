using System.Drawing;
using System.Numerics;

namespace LifeSim.Entities;

public class Animal(Vector2 position) : Entity(position, Color.CornflowerBlue)
{
    private const float Speed = 10f;
    private readonly float EatDistance = 5f;

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
        Program.Chunks[prevChunkPosition].Entities.Remove(this);
        Program.Chunks[newChunkPosition].Entities.Add(this);
    }

    private Food? FindNearestFood()
    {
        Food nearestFood = null;
        var nearestDistance = float.MaxValue;

        var chunkPosition = Position.ToChunkPosition();

        foreach (var food in Program.Chunks[chunkPosition].Entities.OfType<Food>().Where(f => !f.MarkedForDeletion))
        {
            var distance = Vector2.Distance(Position, food.Position);
            if (distance >= nearestDistance) continue;
            nearestDistance = distance;
            nearestFood = food;
        }

        return nearestFood;
    }
}