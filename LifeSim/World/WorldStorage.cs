using System.Collections.Concurrent;
using System.Numerics;
using EFCore.BulkExtensions;
using LifeSim.Data;
using LifeSim.Data.Models;
using LifeSim.Entities;
using LifeSim.Utils;
using Microsoft.EntityFrameworkCore;

namespace LifeSim.World;

public class WorldStorage
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentQueue<int> _deletedFoods = new();
    private readonly ConcurrentQueue<int> _deletedAnimals = new();
    private readonly ConcurrentQueue<FoodEntity> _addedFoods = new();
    private readonly ConcurrentQueue<AnimalEntity> _addedAnimals = new();

    public ConcurrentDictionary<int, Food> Foods { get; } = new();
    public ConcurrentDictionary<int, Animal> Animals { get; } = new();

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
            Program.World.EnqueueFoodAddition(food);
        }
    }

    public void SpawnAnimals(int amount, Vector2 startPosition, Vector2 endPosition, float chaos = 0f)
    {
        for (var i = 0; i < amount; i++)
        {
            var x = RandomUtils.RNG.Next((int)startPosition.X, (int)endPosition.X);
            var y = RandomUtils.RNG.Next((int)startPosition.Y, (int)endPosition.Y);
            
            var position = new Vector2(x, y);

            var animal = Animal.CreateWithChaos(position, chaos);

            Program.World.EnqueueAnimalAddition(animal);
        }
    }

    public void EnqueueFoodDeletion(Food food)
    {
        Chunks[food.Position.ToChunkPosition()].Food.Remove(food);
        var deleted = Foods.TryRemove(food.Id, out _);
        if (!deleted) return;
        _deletedFoods.Enqueue(food.Id);
    }

    public void EnqueueAnimalDeletion(Animal animal)
    {
        Chunks[animal.Position.ToChunkPosition()].Animals.Remove(animal);
        var deleted = Animals.TryRemove(animal.Id, out _);
        if (!deleted) return;
        _deletedAnimals.Enqueue(animal.Id);
    }
    
    public void EnqueueFoodAddition(Food food)
    {
        Foods[food.Id] = food;
        Chunks[food.Position.ToChunkPosition()].Food.Add(food);
        _addedFoods.Enqueue(FoodEntity.ToDomain(food));
    }
    
    public void EnqueueAnimalAddition(Animal animal)
    {
        Animals[animal.Id] = animal;
        Chunks[animal.Position.ToChunkPosition()].Animals.Add(animal);
        _addedAnimals.Enqueue(AnimalEntity.ToDomain(animal));
    }

    public async Task UpdateDbEntitiesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.ChangeTracker.AutoDetectChangesEnabled = false;
        await using var tx = await db.Database.BeginTransactionAsync();

        var foodIds = new List<int>();
        while (_deletedFoods.TryDequeue(out var foodId))
            foodIds.Add(foodId);

        var animalIds = new List<int>();
        while (_deletedAnimals.TryDequeue(out var animalId))
            animalIds.Add(animalId);

        var addedFoodsList = new List<FoodEntity>();
        while (_addedFoods.TryDequeue(out var food))
            addedFoodsList.Add(food);

        var addedAnimalsList = new List<AnimalEntity>();
        while (_addedAnimals.TryDequeue(out var animal))
            addedAnimalsList.Add(animal);

        if (foodIds.Count > 0)
        {
            await db.Foods
                .Where(f => foodIds.Contains((byte)f.Id))
                .ExecuteDeleteAsync();
        }
        
        if (animalIds.Count > 0)
        {
            await db.Animals
                .Where(a => animalIds.Contains((byte)a.Id))
                .ExecuteDeleteAsync();
        }

        if (addedFoodsList.Count > 0) await db.BulkInsertAsync(addedFoodsList);
        if (addedAnimalsList.Count > 0) await db.BulkInsertAsync(addedAnimalsList);

        try
        {
            await db.SaveChangesWithRetryAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine(ex.StackTrace);
        }
        
        db.ChangeTracker.Clear();
        db.ChangeTracker.AutoDetectChangesEnabled = true;
    }

    public void SpawnFood(int amount, float x, float y) => SpawnFood(amount, Vector2.One * x, Vector2.One * y);
    public void SpawnAnimals(int amount, float x, float y, float chaos = 0F) => SpawnAnimals(amount, Vector2.One * x, Vector2.One * y, chaos);

    public async Task LoadWorldAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var foods = await db.Foods.AsNoTracking().ToListAsync(cts.Token);
        var animals = await db.Animals.AsNoTracking().ToListAsync(cts.Token);

        await using var transaction = await db.Database.BeginTransactionAsync(cts.Token);
        try
        {
            await db.Foods.ExecuteDeleteAsync(cts.Token);
            await db.Animals.ExecuteDeleteAsync(cts.Token);

            await db.SaveChangesWithRetryAsync(cts.Token);
            await transaction.CommitAsync(cts.Token);
        }
        catch
        {
            await transaction.RollbackAsync(cts.Token);
            throw;
        }
    
        foods.Select(FoodEntity.FromDomain).ToList().ForEach(EnqueueFoodAddition);
        animals.Select(AnimalEntity.FromDomain).ToList().ForEach(EnqueueAnimalAddition);
    }
}