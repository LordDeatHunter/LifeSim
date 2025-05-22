using System.Drawing;
using System.Numerics;
using LifeSim.Components;
using LifeSim.Data;
using LifeSim.Utils;

namespace LifeSim.Entities;

public class Animal : Entity
{
    private float Speed { get; set; } = 16F;
    private Entity? _target;
    private readonly float _maxSaturation = 20f;
    private float _currentSaturation = 10f;

    private float MaxReproductionCooldown =>
        FoodType switch
        {
            FoodType.HERBIVORE => 3F,
            FoodType.CARNIVORE => 6F,
            FoodType.OMNIVORE => 8F,
            _ => 5F
        };

    private float _reproductionCooldown;

    private float ReproductionCooldown
    {
        get => _reproductionCooldown;
        set => _reproductionCooldown = float.Max(value, 0F);
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

    public float HungerRate { get; } = 0.4F;
    public FoodType FoodType { get; private init; } = FoodType.HERBIVORE;

    private Animal(Vector2 position, float size, Color color) : base(position, color, size)
    {
        Program.World.Chunks[position.ToChunkPosition()].Animals.Add(this);

        var lifespan = 24F + RandomUtils.RNG.NextSingle() * 16F + Size / 4F;
        Components.Add(new LifespanComponent(lifespan));

        HungerRate *= MathF.Sqrt(Size);
        _maxSaturation += RandomUtils.RNG.NextSingle() * 10F;

        Speed *= 1F / MathF.Pow(Size, 0.4F) * 2.5F;
    }

    public Animal(Vector2 position) : this(position, 8F, Color.CornflowerBlue)
    {
    }

    public override void Update(float deltaTime)
    {
        Components.ForEach(c => c.Update(this, deltaTime));

        Saturation -= HungerRate * deltaTime;
        ReproductionCooldown += deltaTime;

        if (MarkedForDeletion) return;

        var prevPosition = Position;
        var prevChunkPosition = prevPosition.ToChunkPosition();

        var needFood = Saturation < (FoodType == FoodType.HERBIVORE ? 5 : 10);

        if (_target == null || _target.MarkedForDeletion || (_target is Animal && needFood))
        {
            var requiredSaturation = FoodType == FoodType.HERBIVORE ? 8F : 13F;

            _target = CanReproduce(requiredSaturation) && !needFood
                ? FindNearestMate()
                : FindNearestFood();

            if (_target is Animal animal)
            {
                animal._target = this;
            }
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
        Program.World.Chunks[prevChunkPosition].Animals.Remove(this);
        Program.World.Chunks[newChunkPosition].Animals.Add(this);
    }

    private Color GetOffspringColor(Animal other)
    {
        var h1 = Color.GetHue();
        var s1 = Color.GetSaturation();
        var l1 = Color.GetBrightness();

        var h2 = other.Color.GetHue();
        var s2 = other.Color.GetSaturation();
        var l2 = other.Color.GetBrightness();

        var h = (h1 + h2) * 0.5F + (RandomUtils.RNG.NextSingle() * 10 - 5);
        h = (h % 360 + 360) % 360;

        var s = (s1 + s2) * 0.5F + (RandomUtils.RNG.NextSingle() * 0.2F - 0.1F);
        s = Math.Clamp(s, 0.75F, 1F);

        var l = (l1 + l2) * 0.5F + (RandomUtils.RNG.NextSingle() * 0.2F - 0.1F);
        l = Math.Clamp(l, 0.35F, 0.65F);

        return ColorUtils.ColorFromHsla(h, s, l, 1F);
    }

    private Entity? FindNearestFood()
    {
        return FoodType switch
        {
            FoodType.HERBIVORE => FindNearestPlant(),
            FoodType.CARNIVORE => FindNearestPrey(),
            FoodType.OMNIVORE => FindNearestFoodEntity(),
            _ => null
        };
    }

    private Food? FindNearestPlant() => Program.World.Foods.Values.Where(f => !f.MarkedForDeletion)
        .OrderBy(f => Vector2.Distance(Position, f.Position)).FirstOrDefault();

    private Animal? FindNearestMate() => Program.World.Animals.Values
        .Where(CanMate)
        .OrderBy(a => Vector2.Distance(Position, a.Position))
        .FirstOrDefault();

    private Animal? FindNearestPrey() => Program.World.Animals.Values
        .Where(CanEatAnimal)
        .OrderBy(a => Vector2.Distance(Position, a.Position))
        .FirstOrDefault();

    private Entity? FindNearestFoodEntity()
    {
        var nearestPlant = FindNearestPlant();
        var nearestPrey = FindNearestPrey();

        if (nearestPlant == null && nearestPrey == null) return null;
        if (nearestPlant == null) return nearestPrey;
        if (nearestPrey == null) return nearestPlant;

        return Vector2.Distance(Position, nearestPlant.Position) < Vector2.Distance(Position, nearestPrey.Position)
            ? nearestPlant
            : nearestPrey;
    }

    public override void MarkForDeletion()
    {
        base.MarkForDeletion();
        Program.World.Chunks[Position.ToChunkPosition()].Animals.Remove(this);
        Program.World.Animals.Remove(Id);
    }

    private bool IsColliding(Entity other) => Vector2.Distance(Position, other.Position) <= (Size + other.Size) / 2;

    private void HandleReproductionTarget()
    {
        if (_target is not Animal animal || animal == this) return;
        if (!CanMate(animal)) return;
        if (Vector2.Distance(Position, animal.Position) > (Size + animal.Size + 4F) / 2F) return;

        Saturation -= FoodType == FoodType.HERBIVORE ? 2 : 5;
        animal.Saturation -= animal.FoodType == FoodType.HERBIVORE ? 2 : 5;

        ReproductionCooldown = 0;
        animal.ReproductionCooldown = 0;

        var position = (Position + animal.Position) / 2;
        var foodType = FoodTypeExtensions.GetRandomForOffspring(this, animal);
        var size = (Size + animal.Size) / 2 + RandomUtils.RNG.NextSingle() * 4 - 2;
        if (foodType == FoodType.CARNIVORE && (FoodType != FoodType.CARNIVORE || animal.FoodType != FoodType.CARNIVORE))
        {
            size += RandomUtils.RNG.NextSingle() * 2;
        }

        var color = GetOffspringColor(animal);

        var newAnimal = new Animal(position, size, color)
        {
            FoodType = foodType,
        };

        Program.World.Animals[newAnimal.Id] = newAnimal;
        if (animal._target == this) animal._target = null;
        _target = null;
    }

    public bool CanMate(Animal animal) => this != animal && CanReproduce() && animal.CanReproduce() &&
                                          MathF.Abs(Size - animal.Size) < 8F && AreCompatible(animal);

    public bool CanReproduce(float requiredSaturation = 5F) => !MarkedForDeletion && Saturation >= requiredSaturation &&
                                                               ReproductionCooldown >= MaxReproductionCooldown;

    public bool AreCompatible(Animal animal) => FoodType == animal.FoodType || FoodType == FoodType.OMNIVORE ||
                                                animal.FoodType == FoodType.OMNIVORE;

    private void HandleCollision()
    {
        var chunkPosition = Position.ToChunkPosition();
        for (var i = -1; i <= 1; i++)
        for (var j = -1; j <= 1; j++)
        {
            var newChunkPos = new Vector2(chunkPosition.X + i, chunkPosition.Y + j);
            if (!Program.World.Chunks.TryGetValue(newChunkPos, out var chunk)) continue;
            foreach (var animal in chunk.Animals.Where(animal => animal != this).Where(IsColliding))
            {
                if (CanEatAnimal(animal))
                {
                    Consume(animal);
                    continue;
                }

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

    private void Consume(Entity entity)
    {
        Saturation += entity.Size / 2;
        entity.MarkForDeletion();

        if (_target == entity)
        {
            _target = null;
        }
    }

    private bool CanEatAnimal(Animal animal) => animal != this && !animal.MarkedForDeletion && Size >= animal.Size &&
                                                ((FoodType == FoodType.CARNIVORE &&
                                                  animal.FoodType != FoodType.CARNIVORE) ||
                                                 (FoodType == FoodType.OMNIVORE &&
                                                  animal.FoodType == FoodType.HERBIVORE));

    public override IEntityDto ToDTO() =>
        new AnimalDto(Id.ToString(), Position.X, Position.Y, Color.ToHex(), Size, FoodType.ToString());
}