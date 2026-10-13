namespace ArenaStorm.WebAPI.Endpoints;

using System.Security.Claims;
using ArenaStorm.WebAPI.Data;
using ArenaStorm.WebAPI.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Request payload for updating the authenticated player's profile.
/// </summary>
public record UpdateProfileRequest(string? DisplayName, string? CharacterClass);

/// <summary>
/// Exposes player profile REST endpoints.
/// </summary>
public static class ProfileEndpoints
{
    /// <summary>
    /// Maps player profile endpoints to the web application.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    public static void MapProfileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/players").RequireAuthorization();

        group.MapGet("/me", async (ClaimsPrincipal userPrincipal, AppDbContext dbContext) =>
        {
            var userId = GetUserId(userPrincipal);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var user = await dbContext.Users
                .AsNoTracking()
                .Include(u => u.Stats)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            return user == null
                ? Results.NotFound(new { error = "NotFound", message = "Player profile was not found." })
                : Results.Ok(ToProfileDto(user));
        });

        group.MapPatch("/me", async (UpdateProfileRequest req, ClaimsPrincipal userPrincipal, AppDbContext dbContext) =>
        {
            var userId = GetUserId(userPrincipal);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var user = await dbContext.Users
                .Include(u => u.Stats)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
            {
                return Results.NotFound(new { error = "NotFound", message = "Player profile was not found." });
            }

            if (!string.IsNullOrWhiteSpace(req.DisplayName))
            {
                var trimmed = req.DisplayName.Trim();
                if (trimmed.Length is < 3 or > 50)
                {
                    return Results.BadRequest(new { error = "ValidationError", message = "DisplayName must be between 3 and 50 characters." });
                }
                user.DisplayName = trimmed;
            }

            if (!string.IsNullOrWhiteSpace(req.CharacterClass))
            {
                user.CharacterClass = req.CharacterClass.Trim();
            }

            await dbContext.SaveChangesAsync();
            return Results.Ok(ToProfileDto(user));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext dbContext) =>
        {
            var user = await dbContext.Users
                .AsNoTracking()
                .Include(u => u.Stats)
                .FirstOrDefaultAsync(u => u.Id == id);

            return user == null
                ? Results.NotFound(new { error = "NotFound", message = $"Player '{id}' was not found." })
                : Results.Ok(ToProfileDto(user));
        });
    }

    private static object ToProfileDto(User user) => new
    {
        id = user.Id,
        username = user.Username,
        displayName = user.DisplayName,
        characterClass = user.CharacterClass,
        createdAt = user.CreatedAt,
        lastLogin = user.LastLogin,
        stats = user.Stats == null ? null : new
        {
            totalKills = user.Stats.TotalKills,
            totalDeaths = user.Stats.TotalDeaths,
            gamesPlayed = user.Stats.GamesPlayed,
            gamesWon = user.Stats.GamesWon,
            kdRatio = user.Stats.KdRatio,
            updatedAt = user.Stats.UpdatedAt
        }
    };

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var val = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(val, out var id) ? id : null;
    }
}
