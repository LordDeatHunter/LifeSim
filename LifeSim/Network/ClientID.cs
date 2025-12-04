namespace LifeSim.Network;

public static class ClientId
{
    public static string? GetClientId(HttpContext context)
    {
        if (context.Items.TryGetValue("clientId", out var clientIdObj) && clientIdObj is string clientId)
            return clientId;

        if (!context.Request.Cookies.TryGetValue("auth_token", out var authToken) ||
            string.IsNullOrEmpty(authToken)) return null;

        context.Items["clientId"] = authToken;
        return authToken;
    }
}