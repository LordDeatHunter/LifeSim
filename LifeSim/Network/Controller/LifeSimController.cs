﻿using LifeSim.Data;
using LifeSim.Network.Request;
using LifeSim.Utils;
using Microsoft.AspNetCore.Mvc;

namespace LifeSim.Network.Controller;

[ApiController]
[Route("api")]
public class LifeSimController(LifeSimApi api) : ControllerBase
{
    private const string ClientIdMissingMessage = "Client ID is required";

    [HttpPost("reignite-life")]
    public IActionResult ReigniteLife()
    {
        if (!Program.World.Animals.IsEmpty)
            return BadRequest(new { message = "Life is already thriving" });

        var animalCount = RandomUtils.RNG.Next(4, 16);

        Program.World.SpawnAnimals(animalCount, 350, 650);
        Program.ReignitionCount += 1;

        return Ok(new { message = "Life reignited" });
    }

    [HttpGet("balance")]
    public IActionResult GetBalance()
    {
        var clientId = ClientId.GetClientId(HttpContext);
        if (string.IsNullOrEmpty(clientId))
            return BadRequest(new { message = ClientIdMissingMessage });

        var balance = api.GetOrCreateBalanceAsync(clientId, 100).Result;

        return Ok(new { balance = balance.Amount });
    }

    [HttpPost("place-bet")]
    public IActionResult PlaceBet([FromBody] BetRequest betRequest)
    {
        var clientId = ClientId.GetClientId(HttpContext);
        if (string.IsNullOrEmpty(clientId))
            return BadRequest(new { message = ClientIdMissingMessage });

        var amount = betRequest.Amount;
        var balance = api.GetOrCreateBalanceAsync(clientId, 100).Result;

        if (amount < 1)
            return BadRequest(new { message = "Bet amount must be greater than zero" });

        if (amount > balance.Amount)
            return BadRequest(new { message = "Insufficient balance" });

        var betType = betRequest.BetType.ToLowerInvariant();
        if (betType != "increase" && betType != "decrease")
            return BadRequest(new { message = "Invalid bet type. Use 'increase' or 'decrease'" });

        var bet = api.PlaceBetAsync(clientId, amount, betType).Result;

        return Ok(new
        {
            balance = balance.Amount,
            bet = new
            {
                id = bet.Id,
                amount = bet.Amount,
                betType = bet.BetType,
                initialCount = bet.InitialCount,
                expiresAt = bet.ExpiresAt,
                status = bet.Status
            }
        });
    }

    [HttpGet("bets")]
    public IActionResult GetBets()
    {
        var clientId = ClientId.GetClientId(HttpContext);
        if (string.IsNullOrEmpty(clientId))
            return BadRequest(new { message = ClientIdMissingMessage });

        var bets = api.GetBetsAsync(clientId).Result;
        return Ok(bets);
    }

    [HttpGet("bet/{id:guid}")]
    public IActionResult GetBetById(Guid id)
    {
        var clientId = ClientId.GetClientId(HttpContext);
        if (string.IsNullOrEmpty(clientId))
            return BadRequest(new { message = ClientIdMissingMessage });

        if (!api.GetOrCreateBetsForUser(clientId).TryGetValue(id, out var bet))
            return NotFound(new { message = "Bet not found" });

        return Ok(bet);
    }

    [HttpGet("leaderboards")]
    public IActionResult GetLeaderboards()
    {
        return Ok(api.GetLeaderboardsAsync().Result);
    }

    [HttpPost("set-name")]
    public IActionResult SetName([FromBody] NameRequest nameRequest)
    {
        var clientId = ClientId.GetClientId(HttpContext);
        if (string.IsNullOrEmpty(clientId))
            return BadRequest(new { message = ClientIdMissingMessage });

        if (string.IsNullOrWhiteSpace(nameRequest.Name))
            return BadRequest(new { message = "Name cannot be empty" });

        var name = nameRequest.Name.Trim().Replace(" ", "_");

        if (api.Names.TryGetValue(clientId, out var value) && value == name)
            return NoContent();

        var originalNameLength = name.Length;
        if (originalNameLength is < 3 or > 20)
            return BadRequest(new { message = "Name must be between 3 and 20 characters" });

        for (var i = 1; api.Names.Values.Contains(name); i++)
            name = $"{name[..(originalNameLength)]}_{i}";

        api.Names[clientId] = name;
        return NoContent();
    }
}