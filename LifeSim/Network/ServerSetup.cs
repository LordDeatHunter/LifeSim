using LifeSim.Data;
using LifeSim.World;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace LifeSim.Network;

public static class ServerSetup
{
    public static WebApplicationBuilder ConfigureServices(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();
        builder.Services.AddDataProtection().SetApplicationName("LifeSim");
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.IdleTimeout = TimeSpan.FromDays(365);
            options.Cookie.IsEssential = true;
        });
        builder.Services.AddScoped<LifeSimApi>();
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite("Data Source=lifesim.db")
        );
        builder.Services.AddSingleton<WorldStorage>();

        return builder;
    }

    public static void ConfigureMiddleware(WebApplication app, WebApplicationBuilder builder)
    {
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
    }
}