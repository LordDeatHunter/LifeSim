using Microsoft.AspNetCore.DataProtection;

namespace LifeSim.Network;

public static class ClientId
{
    public static string? GetClientId(HttpContext context)
    {
        var protector = context.RequestServices
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("ClientId");

        if (!context.Request.Cookies.TryGetValue("clientId", out var protectedClientId))
            return null;

        try
        {
            return protector.Unprotect(protectedClientId);
        }
        catch
        {
            return null;
        }
    }
}