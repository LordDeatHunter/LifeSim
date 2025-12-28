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
        builder.Services.AddHttpClient();
        builder.Services.AddDataProtection().SetApplicationName("LifeSim");
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddScoped<LifeSimApi>();
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite("Data Source=lifesim.db;Mode=ReadWriteCreate;Cache=Shared",
                sqliteOptions =>
                {
                    sqliteOptions.CommandTimeout(60);
                })
        );
        builder.Services.AddSingleton<WorldStorage>();

        return builder;
    }

    public static void ConfigureMiddleware(WebApplication app, WebApplicationBuilder builder)
    {
        app.UseWebSockets();
    }
}