using System.Drawing;
using System.Numerics;
using LifeSim.Data;
using LifeSim.States;
using LifeSim.Utils;

namespace LifeSim.Entities;

public class Animal : Entity
{
    private const float CorpseLifespan = 10F;

    private const float DefaultHungerRate = 0.5F;
    private const float DefaultMaxSat = 30F;

    private const float SizeDifferenceThreshold = 5F;
    private const float MatePadding = 4F;

    private const float DefaultHerbivoreReproCooldown = 4F;
    private const float DefaultCarnivoreReproCooldown = 5F;
    private const float DefaultHerbivoreHungerThreshold = 4F;
    private const float DefaultCarnivoreHungerThreshold = 6F;
    private const float DefaultHerbivoreReproCost = 3F;
    private const float DefaultCarnivoreReproCost = 4F;

    public float Speed { get; set; }
    private float _currentSaturation = DefaultMaxSat / 2;

    private float _lifespan;
    public float Lifespan {
        get => _lifespan;
        set => _lifespan = float.Clamp(value, 10F, 80F);
    }

    private float _predationInclination;

    // Animal's food preference, with 0 being herbivore and 1 being carnivore.
    public float PredationInclination
    {
        get => _predationInclination;
        set => _predationInclination = float.Clamp(value, 0F, 1F);
    }
    
    private readonly float _defaultSpeed;
    private float DefaultSpeed
    {
        get => _defaultSpeed;
        init => _defaultSpeed = float.Clamp(value, 4F, 32F);
    }

    private float _reproductionCooldown;
    public float ReproductionCooldown
    {
        get => _reproductionCooldown;
        set => _reproductionCooldown = float.Max(value, 0F);
    }

    public float Saturation
    {
        get => _currentSaturation;
        set {
            _currentSaturation = float.Min(DefaultMaxSat, value);
            if (_currentSaturation <= 0F) DeathAge = Age;
        }
    }

    public Entity? TargetEntity { get; set; }
    public AnimalStateMachine StateMachine { get; }

    public float EatRangeSquared =>
        MathF.Pow((Size + (TargetEntity?.Size ?? 0F)) / 2F, 2F);

    public float MatingRange => TargetEntity == null ? 0 : (Size + TargetEntity.Size + MatePadding) / 2F;

    public float HungerRate { get; }

    private float _age;
    public float Age {
        get => _age;
        set
        {
            _age = value;
            if (_age >= Lifespan) DeathAge = _age;
        }
    }

    private float _deathAge = -1F;
    private float DeathAge
    {
        get => _deathAge;
        set
        {
            if (_deathAge >= 0F) return;
            _deathAge = value;
            _deathColor = Color;
        }
    }
    private Color _deathColor;
    public bool Dead => DeathAge >= 0F;
    public float RotAmount => Dead ? MathF.Max(0F, Age - DeathAge) / CorpseLifespan : 0F;
    private readonly float _maxHealth = 20F;
    public float MaxHealth
    {
        get => _maxHealth;
        init
        {
            _maxHealth = float.Clamp(value, 5F, 50F);
            Health = _maxHealth;
        }
    }

    private float _health;
    public float Health
    {
        get => _health;
        private set => _health = float.Clamp(value, 0F, MaxHealth);
    }


    public Animal(Vector2 position, float size, Color color, float defaultSpeed) : base(position, color, size)
    {
        DefaultSpeed = defaultSpeed;

        HungerRate = DefaultHungerRate * MathF.Sqrt(Size);
        Speed = DefaultSpeed * (2.5F / MathF.Pow(Size, 0.4F));

        StateMachine = new AnimalStateMachine(this);
    }

    public static Animal CreateWithChaos(Vector2 position, float chaos = 0f)
    {
        var size = RandomUtils.RNG.GenerateChaosFloat(8F, chaos, 1F, 2F);
        var speed = RandomUtils.RNG.GenerateChaosFloat(16F, chaos);

        var predationInclination = RandomUtils.RNG.NextSingle() * chaos;
        var maxHealth = RandomUtils.RNG.GenerateChaosFloat(20F, chaos, 0.25F, 1.5F);
        var lifespan = RandomUtils.RNG.GenerateChaosFloat(40F, chaos, 0.25F, 1.5F);

        return new Animal(position, size, Color.CornflowerBlue, speed)
        {
            PredationInclination = predationInclination,
            MaxHealth = maxHealth,
            Lifespan = lifespan
        };
    }

    public override void Update(float deltaTime)
    {
        if (MarkedForDeletion) return;

        Age += deltaTime;

        if (Dead)
        {
            if (Age >= DeathAge + CorpseLifespan)
            {
                MarkForDeletion();
                return;
            }

            var t = float.Clamp(RotAmount, 0f, 1f);
            var gray = _deathColor.ToGrayscale();
            Color  = ColorUtils.Lerp(_deathColor, gray, t);

            return;
        }

        var amount = PredationInclination / 0.5F;
        if (PredationInclination <= 0.5F)
        {
            Color = ColorUtils.Lerp(Color.FromArgb(0x309898), Color.FromArgb(0xffaf2e), amount);
        }
        else
        {
            amount = (PredationInclination - 0.5F) / 0.5F;
            Color = ColorUtils.Lerp(Color.FromArgb(0xffaf2e), Color.FromArgb(0xcb0404), amount);
        }

        Saturation -= HungerRate * deltaTime;
        ReproductionCooldown += deltaTime;

        var foodPreferenceOffset = CompareFoodTypes();

        PredationInclination += foodPreferenceOffset * 0.1F * deltaTime;

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
            animalCount +=
                chunk.Animals.Count(other => other != this && !other.MarkedForDeletion && Size >= other.Size);
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
                var score = FoodValueHeuristic(food) / herbWeight;
                if (score >= bestScore) continue;
                bestScore = score;
                best = food;
            }
        }

        var carnWeight = PredationInclination;
        if (carnWeight > 0f)
        {
            foreach (var ani in Program.World.Animals.Values.Where(IsPrey))
            {
                var score = FoodValueHeuristic(ani) / carnWeight;
                if (score >= bestScore) continue;
                bestScore = score;
                best = ani;
            }
        }

        return best;
    }

    public Animal? FindNearestPredator(float threatRadius = 60f)
    {
        Animal? nearest = null;
        var bestDistSq = threatRadius * threatRadius;

        for (var dx = -1; dx <= 1; dx++)
        for (var dy = -1; dy <= 1; dy++)
        {
            var chunkPos = Position.ToChunkPosition() + new Vector2(dx, dy);
            if (!Program.World.Chunks.TryGetValue(chunkPos, out var chunk)) continue;

            foreach (var other in chunk.Animals)
            {
                if (other == this || other.MarkedForDeletion) continue;
                if (!other.CanKillAnimal(this)) continue;
                if (!IsAnimalInDangerDistance(other, threatRadius)) continue;

                var d2 = Vector2.DistanceSquared(Position, other.Position);
                if (d2 >= bestDistSq) continue;
                bestDistSq = d2;
                nearest = other;
            }
        }

        return nearest;
    }

    public bool IsAnimalInDangerDistance(Animal other, float safeDistance = 32f)
    {
        if (other == this || other.MarkedForDeletion) return false;
        return Vector2.DistanceSquared(Position, other.Position) <= safeDistance * safeDistance;
    }

    public float FoodValueHeuristic(Entity entity) =>
        Vector2.Distance(entity.Position, Position) / Speed + entity.NutritionValue / Saturation;

    public Animal? FindNearestMate() => Program.World.Animals.Values
        .Where(AreCompatibleForMating)
        .OrderBy(a => Vector2.Distance(Position, a.Position))
        .FirstOrDefault();

    public override void MarkForDeletion()
    {
        base.MarkForDeletion();
        Program.World.EnqueueAnimalDeletion(this);
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

        if (partner.TargetEntity == this) partner.TargetEntity = null;
        TargetEntity = null;
    }

    private void CreateOffspring(Animal partner)
    {
        var position = (Position + partner.Position) / 2F;
        var size = (Size + partner.Size) / 2F + RandomUtils.RNG.NextSingle() * 4F - 2F;
        var color = GetOffspringColor(partner);
        var predationInclination = (PredationInclination + partner.PredationInclination) / 2F +
                                   (RandomUtils.RNG.NextSingle() * 0.1F - 0.05F);
        var maxHealth = (MaxHealth + partner.MaxHealth) / 2F + RandomUtils.RNG.NextSingle() * 5F - 2.5F;
        var speed = (Speed + partner.Speed) / 2F + RandomUtils.RNG.NextSingle() * 2F - 1F;
        var lifespan = (Lifespan + partner.Lifespan) / 2F + RandomUtils.RNG.NextSingle() * 8F - 4F;

        var child = new Animal(position, size, color, speed)
        {
            PredationInclination = float.Clamp(predationInclination, 0F, 1F),
            MaxHealth = maxHealth,
            Lifespan = lifespan,
        };

        Program.World.EnqueueAnimalAddition(child);
    }

    public bool CanMateWith(Animal animal) =>
        AreCompatibleForMating(animal) &&
        AnimalIsInMatingRange(animal);

    public bool CanReproduce() => !Dead &&
                                  ReproductionCooldown >= ReproductionCooldownThreshold &&
                                  Saturation >= HungerThresholdValue &&
                                  Age >= Lifespan * 0F;

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
                if (TargetEntity == animal)
                {
                    if (CanEatAnimal(animal))
                    {
                        Consume(animal);
                    }

                    if (CanKillAnimal(animal))
                    {
                        animal.Health -= (Size - animal.Size) * 0.5F;
                    }
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

    private bool IsPrey(Animal other) => other != this &&
                                         !other.MarkedForDeletion &&
                                         Size >= other.Size &&
                                         PredationInclination - other.PredationInclination >= 0.2F;

    private bool CanEatAnimal(Animal other) => IsPrey(other) && other.Dead;

    private bool CanKillAnimal(Animal other) => IsPrey(other) && !other.Dead;

    public void Consume(Entity entity)
    {
        Saturation += entity.NutritionValue;
        entity.MarkForDeletion();
        if (TargetEntity == entity) TargetEntity = null;
    }

    public override IEntityDto ToDTO() =>
        new AnimalDto(Id.ToString(), Position.X, Position.Y, Color.ToHex(), Size, PredationInclination, Dead, Infected);

    public override float NutritionValue => Size / 2F * (1F - RotAmount);
}