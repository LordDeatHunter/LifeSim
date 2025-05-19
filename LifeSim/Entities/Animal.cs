using System.Drawing;
using System.Numerics;
using LifeSim.Components;

namespace LifeSim.Entities;

public class Animal : Entity
{
    private float Speed { get; set; } = 16F;
    private Entity? _target;
    private readonly float _maxSaturation = 20f;
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
            _currentSaturation = float.Min(_maxSaturation, value);
            if (_currentSaturation <= 0) MarkForDeletion();
        }
    }
    public float HungerRate { get; } = 2F;

    private Animal(Vector2 position, float size, Color color) : base(position, color, size)
    {
        Size = size;

        Program.Chunks[position.ToChunkPosition()].Animals.Add(this);

        var lifespan = 24F + Program.RNG.NextSingle() * 16F + Size / 4F;
        Components.Add(new LifespanComponent(lifespan));

        HungerRate *= 1 / MathF.Sqrt(Size);
        _maxSaturation += Program.RNG.NextSingle() * 10F;
    }

    public Animal(Vector2 position) : this(position, 8F, Color.CornflowerBlue) { }

    public override void Update(float deltaTime)
    {
        Components.ForEach(c => c.Update(this, deltaTime));

        Saturation -= HungerRate * deltaTime;
        ReproductionCooldown += deltaTime;

        if (MarkedForDeletion) return;

        var prevPosition = Position;
        var prevChunkPosition = prevPosition.ToChunkPosition();

        var needFood = Saturation < 5;

        if (_target == null || _target.MarkedForDeletion || (_target is Animal && needFood))
        {
            _target = CanReproduce(15) && !needFood
                ? FindNearestAnimal()
                : FindNearestFood();
        }

        if (_target != null)
        {
            var toTarget = _target.Position - prevPosition;
            var direction = toTarget.LengthSquared() > 0
                ? Vector2.Normalize(toTarget)
                : Vector2.Zero;

            Position += direction * Speed * deltaTime;
        }

        HandleCollision();
        HandleReproductionTarget();

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
    private Animal? FindNearestAnimal() => Program.Animals.Values
        .Where(CanMate)
        .OrderBy(a => Vector2.Distance(Position, a.Position))
        .FirstOrDefault();

    public override void MarkForDeletion()
    {
        base.MarkForDeletion();
        Program.Chunks[Position.ToChunkPosition()].Animals.Remove(this);
        Program.Animals.Remove(Id);
    }

    private bool IsColliding(Entity other) => Vector2.Distance(Position, other.Position) < (Size + other.Size) / 2;

    private void HandleReproductionTarget()
    {
        if (_target is not Animal animal || animal == this) return;
        if (!CanMate(animal)) return;
        if (Vector2.Distance(Position, animal.Position) > 16F) return;

        Saturation -= 5;
        animal.Saturation -= 5;

        var position = (Position + animal.Position) / 2;
        var size = (Size + animal.Size) / 2 + Program.RNG.NextSingle() * 4 - 2;
        var color = GetOffspringColor(animal);

        var newAnimal = new Animal(position, size, color);

        Program.Animals[newAnimal.Id] = newAnimal;
        _target = null;
    }

    public bool CanMate(Animal animal) => this != animal && CanReproduce() && animal.CanReproduce() && MathF.Abs(Size - animal.Size) < 8F;

    public bool CanReproduce(float requiredSaturation = 5F) => !MarkedForDeletion && Saturation >= requiredSaturation && ReproductionCooldown >= MaxReproductionCooldown;

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
            foreach (var food in chunk.Food.Where(IsColliding))
            {
                Consume(food);
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

    private void Consume(Food food)
    {
        Saturation += food.Size / 2;
        food.MarkForDeletion();

        if (_target == food)
        {
            _target = null;
        }
    }
}
