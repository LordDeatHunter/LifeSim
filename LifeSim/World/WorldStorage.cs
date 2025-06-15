using System.Collections.Concurrent;
using System.Numerics;
using LifeSim.Data;
using LifeSim.Data.Models;
using LifeSim.Entities;
using LifeSim.Utils;
using Microsoft.EntityFrameworkCore;

namespace LifeSim.World;

public class WorldStorage
{
    private readonly IServiceScopeFactory _scopeFactory;
    public ConcurrentDictionary<ushort, Food> Foods { get; } = new();
    public ConcurrentDictionary<ushort, Animal> Animals { get; } = new();

    public ConcurrentDictionary<Vector2, Chunk> Chunks { get; } = new();

    // helper for merging foods and animals
    public ConcurrentBag<Entity> AllEntities => [.. Foods.Values, .. Animals.Values];

    public ConcurrentDictionary<string, AnimalDto> GetAnimalDtos() => new(Animals.Values.AsEnumerable()
        .Select(a => (AnimalDto)a.ToDTO()).ToDictionary(a => a.id, a => a));

    public ConcurrentDictionary<string, FoodDto> GetFoodDtos() => new(Foods.Values.AsEnumerable()
        .Select(f => (FoodDto)f.ToDTO()).ToDictionary(f => f.id, f => f));

    public WorldStorage(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        for (var i = 0; i <= 64; i++)
        for (var j = 0; j <= 64; j++)
        {
            var chunkPos = new Vector2(i, j);
            Chunks[chunkPos] = new Chunk(chunkPos);
        }
    }

    public void SpawnFood(int amount, Vector2 startPosition, Vector2 endPosition)
    {
        for (var i = 0; i < amount; i++)
        {
            var x = RandomUtils.RNG.Next((int)startPosition.X, (int)endPosition.X);
            var y = RandomUtils.RNG.Next((int)startPosition.Y, (int)endPosition.Y);

            var food = new Food(new Vector2(x, y));
            SaveFood(food);
        }
    }

    public void SpawnAnimals(int amount, Vector2 startPosition, Vector2 endPosition)
    {
        for (var i = 0; i < amount; i++)
        {
            var x = RandomUtils.RNG.Next((int)startPosition.X, (int)endPosition.X);
            var y = RandomUtils.RNG.Next((int)startPosition.Y, (int)endPosition.Y);

            var animal = new Animal(new Vector2(x, y));
            SaveAnimal(animal);
        }
    }

    public void SaveAnimal(Animal animal)
    {
        Animals[animal.Id] = animal;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Animals.Add(AnimalEntity.ToDomain(animal));

        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine(ex.StackTrace);
        }
    }

    public void SaveFood(Food food)
    {
        Foods[food.Id] = food;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Foods.Add(FoodEntity.ToDomain(food));

        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine(ex.StackTrace);
        }
    }

    public void DeleteAnimal(Animal animal)
    {
        Chunks[animal.Position.ToChunkPosition()].Animals.Remove(animal);
        var deleted = Animals.TryRemove(animal.Id, out _);
        if (!deleted) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Animals.Remove(new AnimalEntity { Id = animal.Id });
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine(ex.StackTrace);
        }
    }

    public void DeleteFood(Food food)
    {
        Chunks[food.Position.ToChunkPosition()].Food.Remove(food);
        var deleted = Foods.TryRemove(food.Id, out _);
        if (!deleted) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Foods.Remove(new FoodEntity { Id = food.Id });
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine(ex.StackTrace);
        }
    }

    public void SpawnFood(int amount, float x, float y) => SpawnFood(amount, Vector2.One * x, Vector2.One * y);
    public void SpawnAnimals(int amount, float x, float y) => SpawnAnimals(amount, Vector2.One * x, Vector2.One * y);

    public async Task LoadWorldAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        await using var tx = await db.Database.BeginTransactionAsync();

        var foods = await db.Foods.ToListAsync();
        var animals = await db.Animals.ToListAsync();

        db.Foods.RemoveRange(foods);
        db.Animals.RemoveRange(animals);

        await db.SaveChangesAsync();
        
        var foodEntities   = new List<FoodEntity>();
        var animalEntities = new List<AnimalEntity>();

        foreach (var newFood in foods.Select(FoodEntity.FromDomain))
        {
            foodEntities.Add(FoodEntity.ToDomain(newFood));
            Foods[newFood.Id] = newFood;
        }

        foreach (var newAnimal in animals.Select(AnimalEntity.FromDomain))
        {
            animalEntities.Add(AnimalEntity.ToDomain(newAnimal));
            Animals[newAnimal.Id] = newAnimal;
        }

        db.Foods.AddRange(foodEntities);
        db.Animals.AddRange(animalEntities);

        try
        {
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine(ex.StackTrace);
        }

        db.ChangeTracker.AutoDetectChangesEnabled = true;
    }
}