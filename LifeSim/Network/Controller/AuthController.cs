using System.Net.Http.Headers;
using System.Text.Json;
using LifeSim.Data;
using LifeSim.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeSim.Network.Controller;

[ApiController]
[Route("api/auth")]
public class AuthController(IConfiguration configuration, ApplicationDbContext db, IHttpClientFactory httpClientFactory)
    : ControllerBase
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    [HttpGet("discord")]
    public IActionResult DiscordLogin()
    {
        var clientId = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID");
        var redirectUri = Environment.GetEnvironmentVariable("DISCORD_REDIRECT_URI");

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
            return BadRequest(new { message = "Discord OAuth is not configured" });

        var authUrl = $"https://discord.com/api/oauth2/authorize?" +
                      $"client_id={clientId}" +
                      $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                      $"&response_type=code" +
                      $"&scope=identify" +
                      $"&prompt=none";

        return Ok(new { url = authUrl });
    }

    [HttpGet("discord/callback")]
    public async Task<IActionResult> DiscordCallback([FromQuery] string code)
    {
        if (string.IsNullOrEmpty(code))
            return BadRequest(new { message = "Authorization code is required" });

        var clientId = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("DISCORD_CLIENT_SECRET");
        var redirectUri = Environment.GetEnvironmentVariable("DISCORD_REDIRECT_URI");

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(redirectUri))
            return BadRequest(new { message = "Discord OAuth is not configured" });

        try
        {
            var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            });

            var tokenResponse = await _httpClient.PostAsync("https://discord.com/api/oauth2/token", tokenRequest);
            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();

            if (!tokenResponse.IsSuccessStatusCode)
                return BadRequest(new { message = "Failed to exchange code for token", details = tokenContent });

            var tokenData = JsonSerializer.Deserialize<DiscordTokenResponse>(tokenContent);
            if (tokenData == null || string.IsNullOrEmpty(tokenData.access_token))
                return BadRequest(new { message = "Invalid token response" });

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", tokenData.access_token);

            var userResponse = await _httpClient.GetAsync("https://discord.com/api/users/@me");
            var userContent = await userResponse.Content.ReadAsStringAsync();

            if (!userResponse.IsSuccessStatusCode)
                return BadRequest(new { message = "Failed to get user info", details = userContent });

            var discordUser = JsonSerializer.Deserialize<DiscordUser>(userContent);
            if (discordUser == null || string.IsNullOrEmpty(discordUser.id))
                return BadRequest(new { message = "Invalid user response" });

            var user = await db.Users.FirstOrDefaultAsync(u => u.DiscordId == discordUser.id);

            if (user == null)
            {
                user = new User
                {
                    DiscordId = discordUser.id,
                    DiscordUsername = discordUser.username,
                    DiscordAvatar = discordUser.avatar,
                    Name = discordUser.username,
                    Balance = 100
                };

                db.Users.Add(user);
            }
            else
            {
                user.DiscordUsername = discordUser.username;
                user.DiscordAvatar = discordUser.avatar;
            }

            await db.SaveChangesWithRetryAsync();

            Response.Cookies.Append(
                "auth_token",
                user.DiscordId,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(30)
                }
            );

            return Redirect("/");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred during authentication", error = ex.Message });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var authToken = Request.Cookies["auth_token"];

        if (string.IsNullOrEmpty(authToken))
            return Unauthorized(new { message = "Not authenticated" });

        var user = await db.Users.FirstOrDefaultAsync(u => u.DiscordId == authToken);

        if (user == null)
            return Unauthorized(new { message = "Invalid auth token" });

        return Ok(new
        {
            id = user.DiscordId,
            name = user.Name,
            balance = user.Balance,
            discord = new
            {
                id = user.DiscordId,
                username = user.DiscordUsername,
                avatar = user.DiscordAvatar
            }
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("auth_token");
        return Ok(new { message = "Logged out successfully" });
    }

    private class DiscordTokenResponse
    {
        public string access_token { get; set; } = null!;
        public string token_type { get; set; } = null!;
        public int expires_in { get; set; }
        public string? refresh_token { get; set; }
        public string scope { get; set; } = null!;
    }

    private class DiscordUser
    {
        public string id { get; set; } = null!;
        public string? username { get; set; }
        public string? discriminator { get; set; }
        public string? avatar { get; set; }
    }
}