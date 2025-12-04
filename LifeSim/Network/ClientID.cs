using Microsoft.AspNetCore.DataProtection;

namespace LifeSim.Network;

public static class ClientId
{
    public static string? GetClientId(HttpContext context)
    {
        if (context.Items.TryGetValue("clientId", out var clientIdObj) && clientIdObj is string clientId)
            return clientId;

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