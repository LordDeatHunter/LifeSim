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

    protected void TryInfectNearbyEntities(float deltaTime)
    {
        const float infectionRadius = 24F;
        const float infectionChancePerSecond = 0.5f;

        var chunkPosition = Position.ToChunkPosition();
        for (var dx = -1; dx <= 1; dx++)
        for (var dy = -1; dy <= 1; dy++)
        {
            var newChunkPos = new Vector2(chunkPosition.X + dx, chunkPosition.Y + dy);
            if (!Program.World.Chunks.TryGetValue(newChunkPos, out var chunk)) continue;

            foreach (var animal in chunk.Animals)
            {
                if (animal == this || animal.Infected || animal.MarkedForDeletion) continue;
                var distance = Vector2.Distance(Position, animal.Position);
                if (distance > infectionRadius) continue;

                if (RandomUtils.RNG.NextSingle() < infectionChancePerSecond * deltaTime)
                    animal.Infected = true;
            }

            foreach (var food in chunk.Food)
            {
                if (food.Infected || food.MarkedForDeletion) continue;
                var distance = Vector2.Distance(Position, food.Position);
                if (distance > infectionRadius) continue;

                if (RandomUtils.RNG.NextSingle() < infectionChancePerSecond * deltaTime)
                    food.Infected = true;
            }
        }
    }
}
