using System.Drawing;
using System.Numerics;
using LifeSim.Components;

namespace LifeSim.Entities;

public class Animal : Entity
{
    private const float Speed = 10f;
    private Entity? _target;

    public Animal(Vector2 position) : base(position, Color.CornflowerBlue)
    {
        Program.Chunks[position.ToChunkPosition()].Animals.Add(this);

        var lifespan = 8F + Program.RNG.NextSingle() * 8F;
        Components.Add(new LifespanComponent(lifespan));
    }

    public override void Update(float deltaTime)
    {
        Components.ForEach(c => c.Update(this, deltaTime));

        if (MarkedForDeletion) return;

        var prevPosition = Position;
        var prevChunkPosition = prevPosition.ToChunkPosition();

        if (_target == null || _target.MarkedForDeletion)
        {
            _target = FindNearestFood();
        }

        if (_target != null)
        {
            var direction = Vector2.Normalize(_target.Position - prevPosition);

            Position += direction * Speed * deltaTime;

            if (IsColliding(_target))
            {
                _target.MarkForDeletion();
            }
            HandleCollision();
        }

        var newChunkPosition = Position.ToChunkPosition();

        if (prevChunkPosition == newChunkPosition) return;
        Program.Chunks[prevChunkPosition].Animals.Remove(this);
        Program.Chunks[newChunkPosition].Animals.Add(this);
    }

    private Food? FindNearestFood() => Program.Foods.Values.Where(f => !f.MarkedForDeletion).OrderBy(f => Vector2.Distance(Position, f.Position)).FirstOrDefault();

    public override void MarkForDeletion()
    {
        base.MarkForDeletion();
        Program.Chunks[Position.ToChunkPosition()].Animals.Remove(this);
        Program.Animals.Remove(Id);
    }

    private bool IsColliding(Entity other) => Vector2.Distance(Position, other.Position) < (Size + other.Size) / 2;

    private void HandleCollision()
    {
        var chunkPosition = Position.ToChunkPosition();
        for(var i = -1; i <= 1; i++)
        for(var j = -1; j <= 1; j++)
        {
            var newChunkPos = new Vector2(chunkPosition.X + i, chunkPosition.Y + j);
            if (!Program.Chunks.TryGetValue(newChunkPos, out var chunk)) continue;
            foreach (var animal in chunk.Animals.Where(animal => animal != this).Where(IsColliding))
            {
                PushAway(animal);
            }
        }
    }

    private void PushAway(Animal animal)
    {
        var distance = Vector2.Distance(Position, animal.Position);
        if (distance < 0.1f) return;
        var direction = Vector2.Normalize(Position - animal.Position);
        var force = (Size + animal.Size) / distance;
        Position += direction * force;
        animal.Position -= direction * force;
    }
}
