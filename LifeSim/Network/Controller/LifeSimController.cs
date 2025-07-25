﻿using LifeSim.Network.Request;
using LifeSim.Utils;
using Microsoft.AspNetCore.Mvc;

namespace LifeSim.Network.Controller;

[ApiController]
[Route("api")]
public class LifeSimController(LifeSimApi api) : ControllerBase
{
    private const string ClientIdMissingMessage = "Client ID is required";

    [HttpPost("reignite-life")]
    public async Task<IActionResult> ReigniteLife([FromBody] ReigniteRequest request)
    {
        var clientId = ClientId.GetClientId(HttpContext);
        if (string.IsNullOrEmpty(clientId))
            return BadRequest(new { message = ClientIdMissingMessage });

        if (!Program.World.Animals.IsEmpty)
            return BadRequest(new { message = "Life is already thriving" });

        var chaos = float.Clamp(request.Chaos ?? 0F, 0F, 1F);
        var reignitionCost = (long)(25F + 975F * chaos);

        var user = api.GetOrCreateUserAsync(clientId).Result;
        if (user.Balance < (ulong)reignitionCost)
            return BadRequest(new { message = "Insufficient balance to reignite life", required = reignitionCost, balance = user.Balance });

        await api.AddBalanceAsync(clientId, -reignitionCost);

        var animalCount = (int)RandomUtils.RNG.GenerateChaosFloat(10F, chaos, 0.2F, 4F);
        var posOffset = 200F + 400F * chaos;

        Program.World.SpawnAnimals(animalCount, 1000F - posOffset, 1000F + posOffset, chaos);
        Program.ReignitionCount += 1;

        return Ok(new { message = "Life reignited", balance = user.Balance });
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var clientId = ClientId.GetClientId(HttpContext);
        if (string.IsNullOrEmpty(clientId))
            return BadRequest(new { message = ClientIdMissingMessage });

        var user = await api.GetOrCreateUserAsync(clientId);

        return Ok(new { balance = user.Balance });
    }

    [HttpPost("place-bet")]
    public async Task<IActionResult> PlaceBet([FromBody] BetRequest betRequest)
    {
        var clientId = ClientId.GetClientId(HttpContext);
        if (string.IsNullOrEmpty(clientId))
            return BadRequest(new { message = ClientIdMissingMessage });

        var amount = betRequest.Amount;
        var user = await api.GetOrCreateUserAsync(clientId);

        if (amount < 1)
            return BadRequest(new { message = "Bet amount must be greater than zero" });

        if (amount > user.Balance)
            return BadRequest(new { message = "Insufficient balance" });

        var betType = betRequest.BetType.ToLowerInvariant();
        if (betType != "increase" && betType != "decrease")
            return BadRequest(new { message = "Invalid bet type. Use 'increase' or 'decrease'" });

        var bet = await api.PlaceBetAsync(clientId, amount, betType);
        user = await api.GetOrCreateUserAsync(clientId);

        return Ok(new
        {
            balance = user.Balance,
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
    public async Task<IActionResult> GetBets()
    {
        var clientId = ClientId.GetClientId(HttpContext);
        if (string.IsNullOrEmpty(clientId))
            return BadRequest(new { message = ClientIdMissingMessage });

        var bets = await api.GetBetsAsync(clientId);
        return Ok(bets);
    }

    [HttpGet("bet/{id:guid}")]
    public async Task<IActionResult> GetBetById(Guid id)
    {
        var clientId = ClientId.GetClientId(HttpContext);
        if (string.IsNullOrEmpty(clientId))
            return BadRequest(new { message = ClientIdMissingMessage });

        var bet = await api.GetBetByIdAsync(id);
        if (bet == null || bet.ClientId != clientId)
            return NotFound(new { message = "Bet not found" });

        return Ok(bet);
    }

    [HttpGet("leaderboards")]
    public async Task<IActionResult> GetLeaderboards()
    {
        return Ok(await api.GetLeaderboardsAsync());
    }

    [HttpPost("set-name")]
    public async Task<IActionResult> SetName([FromBody] NameRequest nameRequest)
    {
        var clientId = ClientId.GetClientId(HttpContext);
        if (string.IsNullOrEmpty(clientId))
            return BadRequest(new { message = ClientIdMissingMessage });

        if (string.IsNullOrWhiteSpace(nameRequest.Name))
            return BadRequest(new { message = "Name cannot be empty" });

        var name = nameRequest.Name.Trim().Replace(" ", "_");

        var originalNameLength = name.Length;
        if (originalNameLength is < 3 or > 20)
            return BadRequest(new { message = "Name must be between 3 and 20 characters" });

        var user = await api.GetOrCreateUserAsync(clientId);
        if (user.Name == name)
            return NoContent();

        
        var originalName = name;
        var index = 1;
        while (await api.IsNameTakenAsync(name))
            name = $"{originalName}_{index++}";
        
        user.Name = name;
        await api.UpdateUserAsync(user);

        return NoContent();
    }
}