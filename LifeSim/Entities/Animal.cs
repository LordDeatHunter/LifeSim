using System.Drawing;
using System.Numerics;

namespace LifeSim.Entities;

public class Animal(float x, float y) : Entity(x, y, Color.CornflowerBlue)
{
    private const float Speed = 10f;

    public override void Update(float deltaTime)
    {
        var prevPosition = new Vector2(X, Y);
        var prevChunkPosition = prevPosition.ToChunkPosition();

        var nearestFood = FindNearestFood();
        if (nearestFood == null) return;
        var foodPosition = new Vector2(nearestFood.X, nearestFood.Y);
        var direction = Vector2.Normalize(foodPosition - prevPosition);

        X += direction.X * Speed * deltaTime;
        Y += direction.Y * Speed * deltaTime;

        X = Math.Clamp(X, 0, 1024);
        Y = Math.Clamp(Y, 0, 1024);

        var newPosition = new Vector2(X, Y);
        var newChunkPosition = newPosition.ToChunkPosition();

        if (prevChunkPosition == newChunkPosition) return;
        Program.Chunks[prevChunkPosition].Entities.Remove(this);
        Program.Chunks[newChunkPosition].Entities.Add(this);
    }

    private Food? FindNearestFood()
    {
        Food nearestFood = null;
        var nearestDistance = float.MaxValue;

        var chunkPosition = new Vector2(X, Y).ToChunkPosition();

        foreach (var food in Program.Chunks[chunkPosition].Entities.OfType<Food>())
        {
            var distance = Vector2.Distance(new Vector2(X, Y), new Vector2(food.X, food.Y));
            if (distance >= nearestDistance) continue;
            nearestDistance = distance;
            nearestFood = food;
        }

        return nearestFood;
    }
}