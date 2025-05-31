using Microsoft.AspNetCore.DataProtection;

namespace LifeSim.Network;

public static class ServerSetup
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddDataProtection().SetApplicationName("LifeSim");
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.IdleTimeout = TimeSpan.FromDays(365);
            options.Cookie.IsEssential = true;
        });
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