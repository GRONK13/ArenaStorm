namespace ArenaStorm.WebAPI.Endpoints;

using System.Security.Claims;
using ArenaStorm.WebAPI.Data;
using ArenaStorm.WebAPI.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Request payload for sending a chat message in a lobby.
/// </summary>
public record SendMessageRequest(string? Content);

/// <summary>
/// Exposes lobby chat REST endpoints.
/// </summary>
public static class ChatEndpoints
{
    /// <summary>
    /// Maps lobby chat endpoints to the web application.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    public static void MapChatEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/lobbies/{lobbyId}/messages").RequireAuthorization();

        group.MapGet("/", async (string lobbyId, int? page, int? pageSize, AppDbContext dbContext) =>
        {
            var safePage = Math.Max(1, page ?? 1);
            var safePageSize = Math.Clamp(pageSize ?? 50, 1, 100);

            var messages = await dbContext.ChatMessages
                .AsNoTracking()
                .Include(m => m.Sender)
                .Where(m => m.LobbyId == lobbyId)
                .OrderBy(m => m.SentAt)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .Select(m => new
                {
                    id = m.Id,
                    lobbyId = m.LobbyId,
                    senderId = m.SenderId,
                    senderName = m.Sender != null ? m.Sender.DisplayName : "Unknown",
                    content = m.Content,
                    sentAt = m.SentAt
                })
                .ToListAsync();

            return Results.Ok(messages);
        });

        group.MapPost("/", async (string lobbyId, SendMessageRequest req, ClaimsPrincipal userPrincipal, AppDbContext dbContext) =>
        {
            if (string.IsNullOrWhiteSpace(req.Content) || req.Content.Trim().Length > 500)
            {
                return Results.BadRequest(new { error = "ValidationError", message = "Content cannot be empty or exceed 500 characters." });
            }

            var userId = GetUserId(userPrincipal);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var message = new ChatMessage
            {
                LobbyId = lobbyId,
                SenderId = userId.Value,
                Content = req.Content.Trim(),
                SentAt = DateTime.UtcNow
            };

            dbContext.ChatMessages.Add(message);
            await dbContext.SaveChangesAsync();

            return Results.Created($"/api/v1/lobbies/{lobbyId}/messages/{message.Id}", new
            {
                id = message.Id,
                lobbyId = message.LobbyId,
                senderId = message.SenderId,
                content = message.Content,
                sentAt = message.SentAt
            });
        });
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var val = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(val, out var id) ? id : null;
    }
}
