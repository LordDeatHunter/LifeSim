using System.Collections.Concurrent;
using System.Numerics;
using LifeSim.Data;
using LifeSim.Entities;
using LifeSim.Utils;

namespace LifeSim.World;

public class WorldStorage
{
    public ConcurrentDictionary<ushort, Food> Foods { get; } = new();
    public ConcurrentDictionary<ushort, Animal> Animals { get; } = new();

    public ConcurrentDictionary<Vector2, Chunk> Chunks { get; } = new();

    // helper for merging foods and animals
    public ConcurrentDictionary<ushort, Entity> AllEntities => new(Animals.Values.ToDictionary(a => a.Id, Entity (a) => a)
            .Concat(Foods.Values.ToDictionary(f => f.Id, Entity (f) => f))
            .ToDictionary(e => e.Key, e => e.Value));

    public ConcurrentDictionary<string, AnimalDto> GetAnimalDtos() => new(Animals.Values.ToList().Select(a => (AnimalDto)a.ToDTO()).ToDictionary(a => a.id, a => a));
    public ConcurrentDictionary<string, FoodDto> GetFoodDtos() => new(Foods.Values.ToList().Select(f => (FoodDto)f.ToDTO()).ToDictionary(f => f.id, f => f));

    public void SpawnFood(int amount, Vector2 startPosition, Vector2 endPosition)
    {
        for (var i = 0; i < amount; i++)
        {
            var x = RandomUtils.RNG.Next((int)startPosition.X, (int)endPosition.X);
            var y = RandomUtils.RNG.Next((int)startPosition.Y, (int)endPosition.Y);

            var food = new Food(new Vector2(x, y));
            Foods[food.Id] = food;
        }
    }

    public void SpawnAnimals(int amount, Vector2 startPosition, Vector2 endPosition)
    {
        for (var i = 0; i < amount; i++)
        {
            var x = RandomUtils.RNG.Next((int)startPosition.X, (int)endPosition.X);
            var y = RandomUtils.RNG.Next((int)startPosition.Y, (int)endPosition.Y);

            var animal = new Animal(new Vector2(x, y));
            Animals[animal.Id] = animal;
        }
    }

    public void SpawnFood(int amount, float x, float y) => SpawnFood(amount, Vector2.One * x, Vector2.One * y);
    public void SpawnAnimals(int amount, float x, float y) => SpawnAnimals(amount, Vector2.One * x, Vector2.One * y);
}