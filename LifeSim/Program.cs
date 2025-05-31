using System.Diagnostics;
using System.Net.WebSockets;
using System.Numerics;
using System.Text;
using System.Text.Json;
using LifeSim.Data;
using LifeSim.Network;
using LifeSim.Utils;
using LifeSim.World;
using Microsoft.AspNetCore.DataProtection;

namespace LifeSim;

public static class Program
{
    public static WorldStorage World { get; } = new();
    public static int ReignitionCount { get; set; }

    public static void Main(string[] args)
    {
        SocketLogic socketLogic = new();
        LifeSimApi api = new();

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddDataProtection().SetApplicationName("LifeSim");

        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.IdleTimeout = TimeSpan.FromDays(365);
            options.Cookie.IsEssential = true;
        });
        var app = builder.Build();

        var previousAnimals = new Dictionary<string, AnimalDto>();
        var previousFoods = new Dictionary<string, FoodDto>();

        for (var i = 0; i <= 32; i++)
        for (var j = 0; j <= 32; j++)
        {
            var chunkPos = new Vector2(i, j);
            World.Chunks[chunkPos] = new Chunk(chunkPos);
        }

        World.SpawnFood(400, 0, 1024);

        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            Console.WriteLine("Unhandled exception: " + e.ExceptionObject);
        };

        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            Console.WriteLine("Unobserved task exception: " + e.Exception);
            e.SetObserved();
        };

        app.UseSession();
        app.UseWebSockets();

        app.Use(async (context, next) =>
        {
            var protector = builder.Services
                .BuildServiceProvider()
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("ClientId");

            if (!context.Request.Cookies.ContainsKey("clientId"))
            {
                var rawId = Guid.NewGuid().ToString();
                var protectedId = protector.Protect(rawId);
                context.Response.Cookies.Append(
                    "clientId",
                    protectedId,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddYears(1)
                    }
                );
                context.Session.SetString("clientId", rawId);
            }

            await next();
        });

        app.Map("/api/reignite_life", api.ReigniteLifeHandler);
        app.Map("/api/balance", api.GetBalance);
        app.Map("/api/place-bet", api.PlaceBet);
        app.Map("/api/bets", api.GetBets);
        app.Map("/api/bet/{id}", api.GetBetById);
        app.Map("/api/leaderboards", api.GetLeaderboards);
        app.Map("/api/set-name", api.SetName);
        app.Map("/ws", socketLogic.HandleWebSocket);

        var stopwatch = new Stopwatch();

        _ = Task.Run(async () =>
        {
            stopwatch.Start();
            var lastTicks = stopwatch.ElapsedTicks;
            var tickFrequency = (float)Stopwatch.Frequency;

            while (true)
            {
                var currentTicks = stopwatch.ElapsedTicks;
                var delta = (currentTicks - lastTicks) / tickFrequency;
                lastTicks = currentTicks;

                foreach (var entity in World.AllEntities.Values)
                {
                    entity.Update(delta);
                }

                if (World.Foods.Count < 1000)
                {
                    var foodAmount = RandomUtils.RNG.Next(0, 6);
                    World.SpawnFood(foodAmount, 0, 1024);
                }

                await Task.Delay(16);
            }
        });

        _ = Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    var activeClients = socketLogic.Clients.Keys.Where(c => c.State == WebSocketState.Open).ToList();

                    var currentAnimals = World.GetAnimalDtos();
                    var currentFoods = World.GetFoodDtos();

                    var addedAnimals = new Dictionary<string, AnimalDto>();
                    var updatedAnimals = new Dictionary<string, Dictionary<string, object>>();
                    var removedAnimals = new List<string>();

                    var addedFoods = new Dictionary<string, FoodDto>();
                    var updatedFoods = new Dictionary<string, Dictionary<string, object>>();
                    var removedFoods = new List<string>();

                    foreach (var id in currentAnimals.Keys.Except(previousAnimals.Keys))
                        addedAnimals[id] = currentAnimals[id];
                    foreach (var id in currentFoods.Keys.Except(previousFoods.Keys))
                        addedFoods[id] = currentFoods[id];

                    foreach (var id in currentAnimals.Keys.Intersect(previousAnimals.Keys))
                    {
                        var old = previousAnimals[id];
                        var curr = currentAnimals[id];
                        var diff = new Dictionary<string, object>();
                        if (curr.id != old.id) diff["id"] = curr.id;
                        if (curr.x != old.x) diff["x"] = curr.x;
                        if (curr.y != old.y) diff["y"] = curr.y;
                        if (curr.color != old.color) diff["color"] = curr.color;
                        if (curr.size != old.size) diff["size"] = curr.size;
                        if (curr.predationInclanation != old.predationInclanation) diff["predationInclanation"] = curr.predationInclanation;

                        if (diff.Count > 0)
                            updatedAnimals[id] = diff;
                    }

                    foreach (var id in currentFoods.Keys.Intersect(previousFoods.Keys))
                    {
                        var old = previousFoods[id];
                        var curr = currentFoods[id];
                        var diff = new Dictionary<string, object>();
                        if (curr.id != old.id) diff["id"] = curr.id;
                        if (curr.x != old.x) diff["x"] = curr.x;
                        if (curr.y != old.y) diff["y"] = curr.y;
                        if (curr.color != old.color) diff["color"] = curr.color;
                        if (curr.size != old.size) diff["size"] = curr.size;

                        if (diff.Count > 0)
                            updatedFoods[id] = diff;
                    }

                    removedAnimals.AddRange(previousAnimals.Keys.Except(currentAnimals.Keys));
                    removedFoods.AddRange(previousFoods.Keys.Except(currentFoods.Keys));

                    previousAnimals = currentAnimals.ToDictionary();
                    previousFoods = currentFoods.ToDictionary();

                    var payload = new
                    {
                        animals = new
                        {
                            added = addedAnimals,
                            removed = removedAnimals,
                            updated = updatedAnimals
                        },
                        foods = new
                        {
                            added = addedFoods,
                            removed = removedFoods,
                            updated = updatedFoods
                        },
                        timeFromStart = stopwatch.Elapsed.TotalMilliseconds,
                        activeClients = activeClients.Count,
                        reignitions = ReignitionCount
                    };

                    var json = JsonSerializer.Serialize(payload);
                    var buffer = Encoding.UTF8.GetBytes(json);
                    var segment = new ArraySegment<byte>(buffer);

                    foreach (var client in activeClients)
                    {
                        try
                        {
                            await client.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        catch
                        {
                            /* dead client */
                        }
                    }

                    await Task.Delay(300);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in update loop: " + ex);
                throw;
            }
        });

        app.Run("http://localhost:5000");
    }
}