namespace ArenaStorm.WebAPI.Endpoints;

using System.Security.Claims;
using ArenaStorm.WebAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Request payload for creating a game lobby.
/// </summary>
public record CreateLobbyRequest(
    string Name,
    int MaxPlayers,
    string? GameMode,
    string? MapId,
    bool IsPrivate,
    string? Password);

/// <summary>
/// Request payload for joining a private lobby.
/// </summary>
public record JoinLobbyRequest(string? Password);

/// <summary>
/// Request payload for updating lobby settings.
/// </summary>
public record UpdateLobbyRequest(
    string? Name,
    int? MaxPlayers,
    string? GameMode,
    string? MapId);

/// <summary>
/// Exposes endpoints for managing ephemeral game lobbies.
/// </summary>
public static class LobbyEndpoints
{
    /// <summary>
    /// Maps lobby management endpoints to the web application.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    public static void MapLobbyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/lobbies").RequireAuthorization();

        group.MapGet("/", (int? page, int? pageSize, LobbyService lobbyService) =>
        {
            var lobbies = lobbyService.GetPublicLobbies(page ?? 1, pageSize ?? 20);
            return Results.Ok(lobbies);
        });

        group.MapPost("/", (CreateLobbyRequest req, ClaimsPrincipal user, LobbyService lobbyService) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
            {
                return Results.BadRequest(new { error = "ValidationError", message = "Lobby name is required." });
            }

            var userId = GetUserId(user);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var displayName = GetDisplayName(user);
            var lobby = lobbyService.CreateLobby(
                req.Name.Trim(),
                userId.Value,
                displayName,
                req.MaxPlayers <= 0 ? 8 : req.MaxPlayers,
                req.GameMode ?? "Deathmatch",
                req.MapId ?? "arena_01",
                req.IsPrivate,
                req.Password);

            return Results.Created($"/api/v1/lobbies/{lobby.Id}", lobby);
        });

        group.MapGet("/{id}", (string id, LobbyService lobbyService) =>
        {
            var lobby = lobbyService.GetLobby(id);
            return lobby == null
                ? Results.NotFound(new { error = "NotFound", message = $"Lobby '{id}' was not found." })
                : Results.Ok(lobby);
        });

        group.MapPost("/{id}/join", (string id, JoinLobbyRequest? req, ClaimsPrincipal user, LobbyService lobbyService) =>
        {
            var userId = GetUserId(user);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var lobby = lobbyService.GetLobby(id);
            if (lobby == null)
            {
                return Results.NotFound(new { error = "NotFound", message = $"Lobby '{id}' was not found." });
            }

            var displayName = GetDisplayName(user);
            var joined = lobbyService.JoinLobby(id, userId.Value, displayName, req?.Password);
            if (!joined)
            {
                return Results.BadRequest(new { error = "JoinFailed", message = "Could not join lobby. Check status, capacity, or password." });
            }

            return Results.Ok(lobbyService.GetLobby(id));
        });

        group.MapPost("/{id}/leave", (string id, ClaimsPrincipal user, LobbyService lobbyService) =>
        {
            var userId = GetUserId(user);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var left = lobbyService.LeaveLobby(id, userId.Value);
            return !left
                ? Results.NotFound(new { error = "NotFound", message = "Player or lobby not found." })
                : Results.Ok(new { message = "Left lobby successfully." });
        });

        group.MapPost("/{id}/start", (string id, ClaimsPrincipal user, LobbyService lobbyService) =>
        {
            var userId = GetUserId(user);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var lobby = lobbyService.GetLobby(id);
            if (lobby == null)
            {
                return Results.NotFound(new { error = "NotFound", message = $"Lobby '{id}' was not found." });
            }

            if (lobby.HostId != userId.Value)
            {
                return Results.Json(new { error = "Forbidden", message = "Only the lobby host can start the match." }, statusCode: StatusCodes.Status403Forbidden);
            }

            var started = lobbyService.StartMatch(id, userId.Value);
            return !started
                ? Results.BadRequest(new { error = "StartFailed", message = "Lobby cannot be started in its current status." })
                : Results.Ok(new { message = "Match starting.", lobby = lobbyService.GetLobby(id) });
        });

        group.MapPatch("/{id}", (string id, UpdateLobbyRequest req, ClaimsPrincipal user, LobbyService lobbyService) =>
        {
            var userId = GetUserId(user);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var lobby = lobbyService.GetLobby(id);
            if (lobby == null)
            {
                return Results.NotFound(new { error = "NotFound", message = $"Lobby '{id}' was not found." });
            }

            if (lobby.HostId != userId.Value)
            {
                return Results.Json(new { error = "Forbidden", message = "Only the lobby host can update lobby settings." }, statusCode: StatusCodes.Status403Forbidden);
            }

            var updated = lobbyService.UpdateLobby(id, userId.Value, req.Name, req.MaxPlayers, req.GameMode, req.MapId);
            return !updated
                ? Results.BadRequest(new { error = "UpdateFailed", message = "Could not update lobby." })
                : Results.Ok(lobbyService.GetLobby(id));
        });
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var val = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(val, out var id) ? id : null;
    }

    private static string GetDisplayName(ClaimsPrincipal user)
    {
        return user.FindFirstValue("displayName") ?? user.FindFirstValue(ClaimTypes.Name) ?? "Player";
    }
}
