using System.Collections.Immutable;
using System.Drawing;
using System.Numerics;
using LifeSim.Data;
using LifeSim.States;
using LifeSim.Utils;

namespace LifeSim.Entities;

public class Animal : Entity
{
    private const float DefaultSpeed = 16F;
    private const float DefaultHungerRate = 0.4F;
    private const float DefaultMaxSat = 20F;
    private const float SizeDifferenceThreshold = 5F;
    private const float MatePadding = 4F;

    public static readonly ImmutableDictionary<FoodType, float> BaseReproductionCooldown =
        ImmutableDictionary<FoodType, float>.Empty
            .Add(FoodType.HERBIVORE, 5F)
            .Add(FoodType.CARNIVORE, 8F)
            .Add(FoodType.OMNIVORE, 6F);

    public static readonly ImmutableDictionary<FoodType, float> HungerThreshold =
        ImmutableDictionary<FoodType, float>.Empty
            .Add(FoodType.HERBIVORE, 4F)
            .Add(FoodType.CARNIVORE, 6F)
            .Add(FoodType.OMNIVORE, 5F);

    public static readonly ImmutableDictionary<FoodType, float> ReproductionCost =
        ImmutableDictionary<FoodType, float>.Empty
            .Add(FoodType.HERBIVORE, 2F)
            .Add(FoodType.CARNIVORE, 5F)
            .Add(FoodType.OMNIVORE, 3.5F);

    private float Speed { get; set; }
    private readonly float _maxSaturation = DefaultMaxSat;
    private float _currentSaturation = DefaultMaxSat / 2;
    private readonly float _lifespan = 24F;
    private float _age = 0F;

    private float MaxReproductionCooldown => BaseReproductionCooldown.GetValueOrDefault(FoodType, 5F);
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

    public Entity? Target { get; set; }
    public AnimalStateMachine StateMachine { get; }

    public float EatRangeSquared =>
        MathF.Pow((Size + (Target?.Size ?? 0F)) / 2F, 2F);

    public float MatingRange => Target == null ? 0 : (Size + Target.Size + MatePadding) / 2F;

    public float HungerRate { get; }
    public FoodType FoodType { get; private init; } = FoodType.HERBIVORE;

    public float Age {
        get => _age;
        set
        {
            _age = value;
            if (_age >= _lifespan) MarkForDeletion();
        }
    }

    private Animal(Vector2 position, float size, Color color) : base(position, color, size)
    {
        Program.World.Chunks[position.ToChunkPosition()].Animals.Add(this);
        _lifespan += RandomUtils.RNG.NextSingle() * 16F + size / 4F;

        HungerRate = DefaultHungerRate * MathF.Sqrt(size);
        Speed = DefaultSpeed * (2.5F / MathF.Pow(size, 0.4F));

        StateMachine = new AnimalStateMachine(this);
    }

    public Animal(Vector2 position) : this(position, 8F, Color.CornflowerBlue)
    {
    }

    public override void Update(float deltaTime)
    {
        Age += deltaTime;
        Saturation -= HungerRate * deltaTime;
        ReproductionCooldown += deltaTime;

        if (MarkedForDeletion) return;

        var prevPosition = Position;
        var prevChunkPosition = prevPosition.ToChunkPosition();

        StateMachine.Update(deltaTime);

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

        var h = (h1 + h2) * 0.5F + (RandomUtils.RNG.NextSingle() * 10F - 5F);
        h = (h % 360F + 360F) % 360F;

        var s = (s1 + s2) * 0.5F + (RandomUtils.RNG.NextSingle() * 0.2F - 0.1F);
        s = Math.Clamp(s, 0.75F, 1F);

        var l = (l1 + l2) * 0.5F + (RandomUtils.RNG.NextSingle() * 0.2F - 0.1F);
        l = Math.Clamp(l, 0.35F, 0.65F);

        return ColorUtils.ColorFromHsla(h, s, l, 1F);
    }

    public void MoveTowards(Vector2 destination, float deltaTime)
    {
        var direction = Vector2.Normalize(destination - Position);
        if (!float.IsNaN(direction.X) && !float.IsNaN(direction.Y))
            Position += direction * Speed * deltaTime;

        HandleCollision();
    }

    public Entity? FindNearestFood() => FoodType switch
    {
        FoodType.HERBIVORE => FindNearestPlant(),
        FoodType.CARNIVORE => FindNearestPrey(),
        FoodType.OMNIVORE => FindNearestFoodEntity(),
        _ => null
    };

    public Food? FindNearestPlant() => Program.World.Foods.Values
        .Where(f => !f.MarkedForDeletion)
        .OrderBy(f => Vector2.Distance(Position, f.Position))
        .FirstOrDefault();

    public Animal? FindNearestMate() => Program.World.Animals.Values
        .Where(AreCompatibleForMating)
        .OrderBy(a => Vector2.Distance(Position, a.Position))
        .FirstOrDefault();

    public Animal? FindNearestPrey() => Program.World.Animals.Values
        .Where(CanEatAnimal)
        .OrderBy(a => Vector2.Distance(Position, a.Position))
        .FirstOrDefault();

    public Entity? FindNearestFoodEntity()
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

    public bool IsColliding(Entity other) => Vector2.Distance(Position, other.Position) <= (Size + other.Size) / 2;

    public void HandleReproductionTarget(Animal animal)
    {
        Saturation -= ReproductionCost[FoodType];
        animal.Saturation -= ReproductionCost[animal.FoodType];

        ReproductionCooldown = 0F;
        animal.ReproductionCooldown = 0F;

        var childCount = RandomUtils.RNG.NextSingle() switch
        {
            < 0.6F => 1,
            < 0.9F => 2,
            _ => 3
        };

        for (var i = 0; i < childCount; i++)
        {
            CreateOffspring(animal);
        }

        if (animal.Target == this) animal.Target = null;
        Target = null;
    }
    
    private void CreateOffspring(Animal other)
    {
        var position = (Position + other.Position) / 2F;
        var foodType = FoodTypeExtensions.GetRandomForOffspring(this, other);
        var size = (Size + other.Size) / 2F + RandomUtils.RNG.NextSingle() * 4F - 2F;
        if (foodType == FoodType.CARNIVORE && (FoodType != FoodType.CARNIVORE || other.FoodType != FoodType.CARNIVORE))
        {
            size += RandomUtils.RNG.NextSingle() * 2F;
        }

        var color = GetOffspringColor(other);

        var newAnimal = new Animal(position, size, color)
        {
            FoodType = foodType,
        };

        Program.World.Animals[newAnimal.Id] = newAnimal;
    }

    public bool CanMateWith(Animal animal) =>
        AreCompatibleForMating(animal) &&
        AnimalIsInMatingRange(animal);

    public bool CanReproduce() => ReproductionCooldown >= MaxReproductionCooldown &&
                                  Saturation >= HungerThreshold[FoodType] &&
                                  Age >= _lifespan * 0.2F;

    public bool AnimalIsInMatingRange(Animal animal) => Vector2.Distance(Position, animal.Position) <= MatingRange;

    public bool AreCompatibleForMating(Animal animal) =>
        this != animal &&
        CanReproduce() &&
        animal.CanReproduce() &&
        MathF.Abs(Size - animal.Size) <= SizeDifferenceThreshold &&
        AreDietsCompatibleForMating(animal);

    public bool AreDietsCompatibleForMating(Animal animal) =>
        FoodType == animal.FoodType ||
        FoodType == FoodType.OMNIVORE ||
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
        if (distance < 0.1F) return;
        var direction = Vector2.Normalize(Position - animal.Position);
        var force = (Size + animal.Size) / distance;
        Position += direction * force;
        animal.Position -= direction * force;
    }

    public void Consume(Entity entity)
    {
        Saturation += entity.Size / 2F;
        entity.MarkForDeletion();

        if (Target == entity)
        {
            Target = null;
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