namespace ArenaStorm.WebAPI.Endpoints;

using System.Security.Claims;
using ArenaStorm.WebAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Exposes competitive leaderboard REST endpoints.
/// </summary>
public static class LeaderboardEndpoints
{
    private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "kills",
        "wins",
        "kd"
    };

    /// <summary>
    /// Maps leaderboard endpoints to the web application.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    public static void MapLeaderboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/leaderboards").RequireAuthorization();

        group.MapGet("/", () => Results.Ok(new[] { "kills", "wins", "kd" }));

        group.MapGet("/{type}", async (string type, int? page, int? pageSize, LeaderboardService leaderboardService) =>
        {
            if (!ValidTypes.Contains(type))
            {
                return Results.BadRequest(new { error = "InvalidType", message = "Leaderboard type must be 'kills', 'wins', or 'kd'." });
            }

            var entries = await leaderboardService.GetLeaderboardAsync(type, page ?? 1, pageSize ?? 50);
            return Results.Ok(entries);
        });

        group.MapGet("/{type}/me", async (string type, ClaimsPrincipal user, LeaderboardService leaderboardService) =>
        {
            if (!ValidTypes.Contains(type))
            {
                return Results.BadRequest(new { error = "InvalidType", message = "Leaderboard type must be 'kills', 'wins', or 'kd'." });
            }

            var userId = GetUserId(user);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var rank = await leaderboardService.GetPlayerRankAsync(type, userId.Value);
            return rank == null
                ? Results.NotFound(new { error = "NotFound", message = "No leaderboard entry found for current player." })
                : Results.Ok(rank);
        });
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var val = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(val, out var id) ? id : null;
    }
}
