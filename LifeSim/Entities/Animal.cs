using System.Drawing;
using System.Numerics;
using LifeSim.Components;

namespace LifeSim.Entities;

public class Animal : Entity
{
    private float Speed { get; set; } = 24F;
    private Entity? _target;
    private const float MaxSaturation = 20f;
    private float _currentSaturation = 10f;
    private const float MaxReproductionCooldown = 5f;
    private float _reproductionCooldown;
    private float ReproductionCooldown
    {
        get => _reproductionCooldown;
        set => _reproductionCooldown = float.Clamp(value, 0F, MaxReproductionCooldown);
    }
    public float Saturation
    {
        get => _currentSaturation;
        set
        {
            _currentSaturation = float.Min(MaxSaturation, value);
            if (_currentSaturation <= 0) MarkForDeletion();
        }
    }
    public float HungerRate { get; } = 0.5f;

    public Animal(Vector2 position) : base(position, Color.CornflowerBlue)
    {
        Program.Chunks[position.ToChunkPosition()].Animals.Add(this);

        var lifespan = 24F + Program.RNG.NextSingle() * 16F;
        Components.Add(new LifespanComponent(lifespan));
    }

    public override void Update(float deltaTime)
    {
        Components.ForEach(c => c.Update(this, deltaTime));

        Saturation -= HungerRate * deltaTime;
        ReproductionCooldown += deltaTime;

        if (MarkedForDeletion) return;

        var prevPosition = Position;
        var prevChunkPosition = prevPosition.ToChunkPosition();

        if (_target == null || _target.MarkedForDeletion)
        {
            _target = Saturation >= 15 && ReproductionCooldown >= MaxReproductionCooldown 
                ? FindNearestAnimal()
                : FindNearestFood();
        }

        if (_target != null)
        {
            var direction = Vector2.Normalize(_target.Position - prevPosition);
            if (float.IsNaN(direction.X) || float.IsNaN(direction.Y))
            {
                direction = Vector2.Zero;
            }
            else
            {
                direction = Vector2.Normalize(direction);
            }

            Position += direction * Speed * deltaTime;

            if (IsColliding(_target))
            {
                switch (_target)
                {
                    case Animal animal when Saturation >= 5 && ReproductionCooldown >= MaxReproductionCooldown:
                    {
                        Saturation -= 5;
                        var newAnimal = new Animal(Position)
                        {
                            Position = (Position + animal.Position) / 2,
                            Size = (Size + animal.Size) / 2 + Program.RNG.NextSingle() * 4 - 2,
                            Color = GetOffspringColor(animal),
                            Speed = (Size + animal.Size) / 2 + Program.RNG.NextSingle() * 4 - 2,
                        };
                        Program.Animals[newAnimal.Id] = newAnimal;
                        _target = null;
                        break;
                    }
                    case Food food:
                        Saturation += food.Size / 2;
                        _target.MarkForDeletion();
                        break;
                }
            }
            HandleCollision();
        }

        var newChunkPosition = Position.ToChunkPosition();

        if (prevChunkPosition == newChunkPosition) return;
        Program.Chunks[prevChunkPosition].Animals.Remove(this);
        Program.Chunks[newChunkPosition].Animals.Add(this);
    }
    
    private Color GetOffspringColor(Animal other)
    {
        var r = int.Clamp((int)((Color.R + other.Color.R) / 2F + Program.RNG.Next(-20, 20)), 0, 255);
        var g = int.Clamp((int)((Color.G + other.Color.G) / 2F + Program.RNG.Next(-20, 20)), 0, 255);
        var b = int.Clamp((int)((Color.B + other.Color.B) / 2F + Program.RNG.Next(-20, 20)), 0, 255);
        
        return Color.FromArgb(r, g, b);
    }

    private Food? FindNearestFood() => Program.Foods.Values.Where(f => !f.MarkedForDeletion).OrderBy(f => Vector2.Distance(Position, f.Position)).FirstOrDefault();
    private Animal? FindNearestAnimal() => Program.Animals.Values.Where(a => !a.MarkedForDeletion).OrderBy(a => Vector2.Distance(Position, a.Position)).FirstOrDefault();

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
