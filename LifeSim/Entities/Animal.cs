using System.Drawing;
using System.Numerics;
using LifeSim.Data;
using LifeSim.States;
using LifeSim.Utils;

namespace LifeSim.Entities;

public class Animal : Entity
{
    private const float DefaultSpeed = 16F;
    private const float DefaultHungerRate = 0.5F;
    private const float DefaultMaxSat = 20F;

    private const float SizeDifferenceThreshold = 5F;
    private const float MatePadding = 4F;

    private const float DefaultHerbivoreReproCooldown = 4F;
    private const float DefaultCarnivoreReproCooldown = 5F;
    private const float DefaultHerbivoreHungerThreshold = 4F;
    private const float DefaultCarnivoreHungerThreshold = 6F;
    private const float DefaultHerbivoreReproCost = 3F;
    private const float DefaultCarnivoreReproCost = 4F;

    private float Speed { get; set; }
    private readonly float _maxSaturation = DefaultMaxSat;
    private float _currentSaturation = DefaultMaxSat / 2;

    private readonly float _lifespan = 24F;
    private float _age;

    private float _predationInclination;
    public float PredationInclination
    {
        get => _predationInclination;
        set => _predationInclination = float.Clamp(value, 0F, 1F);
    }

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

    public float Age
    {
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
        _lifespan += RandomUtils.RNG.NextSingle() * 16F + Size / 4F;

        HungerRate = DefaultHungerRate * MathF.Sqrt(Size);
        Speed = DefaultSpeed * (2.5F / MathF.Pow(Size, 0.4F));

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

        var foodPreferenceOffset = CompareFoodTypes();

        PredationInclination += foodPreferenceOffset * 0.1F * deltaTime;

        if (MarkedForDeletion) return;

        var prevPosition = Position;
        var prevChunkPosition = prevPosition.ToChunkPosition();

        StateMachine.Update(deltaTime);

        var newChunkPosition = Position.ToChunkPosition();
        if (prevChunkPosition.Equals(newChunkPosition)) return;
        Program.World.Chunks[prevChunkPosition].Animals.Remove(this);
        Program.World.Chunks[newChunkPosition].Animals.Add(this);
    }

    private int CompareFoodTypes()
    {
        var foodCount = 0;
        var animalCount = 0;
        for (var dx = -1; dx <= 1; dx++)
        for (var dy = -1; dy <= 1; dy++)
        {
            var chunkPos = Position.ToChunkPosition() + new Vector2(dx, dy);
            if (!Program.World.Chunks.TryGetValue(chunkPos, out var chunk)) continue;
            
            foodCount += chunk.Food.Count;
            animalCount += chunk.Animals.Count(other => other != this && !other.MarkedForDeletion && Size >= other.Size);
        }

        if (foodCount > animalCount * 2) return -1;
        if (animalCount > foodCount * 2) return 1;

        return 0;
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
        {
            var prevPosition = Position;
            var finalDestination = Position + direction * Speed * deltaTime;
            var stepSize = Size / 2F;
            var steps = (int)Math.Ceiling(Speed * deltaTime / stepSize);
            for (var i = 1; i <= steps; i++)
            {
                Position = Vector2.Lerp(prevPosition, finalDestination, (float)i / steps);
                HandleCollision();
            }
        }

        HandleCollision();
    }

    public Entity? FindNearestTarget()
    {
        Entity? best = null;
        var bestScore = float.MaxValue;

        var herbWeight = 1 - PredationInclination;
        if (herbWeight > 0f)
        {
            foreach (var food in Program.World.Foods.Values.Where(f => !f.MarkedForDeletion))
            {
                var score = FoodValue(food) / herbWeight;
                if (score >= bestScore) continue;
                bestScore = score;
                best = food;
            }
        }

        var carnWeight = PredationInclination;
        if (carnWeight > 0f)
        {
            foreach (var ani in Program.World.Animals.Values.Where(CanEatAnimal))
            {
                var score = FoodValue(ani) / carnWeight;
                if (score >= bestScore) continue;
                bestScore = score;
                best = ani;
            }
        }

        return best;
    }

    public float FoodValue(Entity entity) =>
        Vector2.Distance(entity.Position, Position) / Speed + entity.Size / 2F / Saturation;

    public Animal? FindNearestMate() => Program.World.Animals.Values
        .Where(AreCompatibleForMating)
        .OrderBy(a => Vector2.Distance(Position, a.Position))
        .FirstOrDefault();

    public override void MarkForDeletion()
    {
        base.MarkForDeletion();
        Program.World.Chunks[Position.ToChunkPosition()].Animals.Remove(this);
        Program.World.Animals.TryRemove(Id, out _);
    }

    public bool IsColliding(Entity other) => Vector2.Distance(Position, other.Position) <= (Size + other.Size) / 2F;

    public void HandleReproductionTarget(Animal partner)
    {
        Saturation -= ReproductionCost;
        partner.Saturation -= partner.ReproductionCost;

        ReproductionCooldown = 0F;
        partner.ReproductionCooldown = 0F;

        var childCount = RandomUtils.RNG.NextSingle() switch
        {
            < 0.6F => 1,
            < 0.9F => 2,
            _ => 3
        };

        for (var i = 0; i < childCount; i++)
        {
            CreateOffspring(partner);
        }

        if (partner.Target == this) partner.Target = null;
        Target = null;
    }

    private void CreateOffspring(Animal partner)
    {
        var position = (Position + partner.Position) / 2F;
        var size = (Size + partner.Size) / 2F + RandomUtils.RNG.NextSingle() * 4F - 2F;
        var color = GetOffspringColor(partner);
        var predationInclination = (PredationInclination + partner.PredationInclination) / 2F +
                                   (RandomUtils.RNG.NextSingle() * 0.1F - 0.05F);

        var child = new Animal(position, size, color)
        {
            PredationInclination = float.Clamp(predationInclination, 0F, 1F)
        };

        Program.World.Animals[child.Id] = child;
    }

    public bool CanMateWith(Animal animal) =>
        AreCompatibleForMating(animal) &&
        AnimalIsInMatingRange(animal);

    public bool CanReproduce() => ReproductionCooldown >= ReproductionCooldownThreshold &&
                                  Saturation >= HungerThresholdValue &&
                                  Age >= _lifespan * 0F;

    public bool AnimalIsInMatingRange(Animal animal) => Vector2.Distance(Position, animal.Position) <= MatingRange;

    public bool AreCompatibleForMating(Animal animal) =>
        this != animal &&
        CanReproduce() &&
        animal.CanReproduce() &&
        MathF.Abs(Size - animal.Size) <= SizeDifferenceThreshold &&
        AreDietsCompatibleForMating(animal);

    public bool AreDietsCompatibleForMating(Animal other) =>
        MathF.Abs(PredationInclination - other.PredationInclination) <= 0.2F;

    public float HungerThresholdValue =>
        DefaultHerbivoreHungerThreshold +
        PredationInclination * (DefaultCarnivoreHungerThreshold - DefaultHerbivoreHungerThreshold);

    private float ReproductionCost =>
        DefaultHerbivoreReproCost + PredationInclination * (DefaultCarnivoreReproCost - DefaultHerbivoreReproCost);

    private float ReproductionCooldownThreshold =>
        DefaultHerbivoreReproCooldown +
        PredationInclination * (DefaultCarnivoreReproCooldown - DefaultHerbivoreReproCooldown);

    private void HandleCollision()
    {
        var chunkPosition = Position.ToChunkPosition();
        for (var dx = -1; dx <= 1; dx++)
        for (var dy = -1; dy <= 1; dy++)
        {
            var newChunkPos = new Vector2(chunkPosition.X + dx, chunkPosition.Y + dy);
            if (!Program.World.Chunks.TryGetValue(newChunkPos, out var chunk)) continue;

            foreach (var animal in chunk.Animals.Where(animal => animal != this && IsColliding(animal)))
            {
                if (Target == animal && CanEatAnimal(animal))
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

    private void PushAway(Animal other)
    {
        var distance = Vector2.Distance(Position, other.Position);
        if (distance < 0.1F) return;
        var direction = Vector2.Normalize(Position - other.Position);
        var force = (Size + other.Size) / distance;
        Position += direction * force;
        other.Position -= direction * force;
    }

    private bool CanEatAnimal(Animal other) => other != this && !other.MarkedForDeletion && Size >= other.Size &&
                                               PredationInclination - other.PredationInclination >= 0.2F;

    public void Consume(Entity entity)
    {
        Saturation += entity.Size / 2F;
        entity.MarkForDeletion();
        if (Target == entity) Target = null;
    }

    public override IEntityDto ToDTO() =>
        new AnimalDto(Id.ToString(), Position.X, Position.Y, Color.ToHex(), Size, PredationInclination);
}