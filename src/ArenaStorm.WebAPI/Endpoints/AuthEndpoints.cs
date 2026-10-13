namespace ArenaStorm.WebAPI.Endpoints;

using ArenaStorm.WebAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Request payload for user registration.
/// </summary>
public record RegisterRequest(string? Username, string? Password, string? DisplayName);

/// <summary>
/// Request payload for user login.
/// </summary>
public record LoginRequest(string? Username, string? Password);

/// <summary>
/// Exposes authentication REST endpoints for registration and login.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Maps authentication endpoints to the web application.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth");

        group.MapPost("/register", async (RegisterRequest req, AuthService authService) =>
        {
            if (string.IsNullOrWhiteSpace(req.Username) || req.Username.Trim().Length is < 3 or > 50)
            {
                return Results.BadRequest(new { error = "ValidationError", message = "Username must be between 3 and 50 characters." });
            }

            if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            {
                return Results.BadRequest(new { error = "ValidationError", message = "Password must be at least 6 characters long." });
            }

            if (string.IsNullOrWhiteSpace(req.DisplayName) || req.DisplayName.Trim().Length is < 3 or > 50)
            {
                return Results.BadRequest(new { error = "ValidationError", message = "DisplayName must be between 3 and 50 characters." });
            }

            var result = await authService.RegisterAsync(req.Username.Trim(), req.Password, req.DisplayName.Trim());
            if (result == null)
            {
                return Results.Conflict(new { error = "Conflict", message = "Username already taken." });
            }

            var (user, token) = result.Value;
            var expiresAt = DateTime.UtcNow.AddHours(24);

            return Results.Created($"/api/v1/players/{user.Id}", new
            {
                token,
                expiresAt,
                player = new
                {
                    id = user.Id,
                    username = user.Username,
                    displayName = user.DisplayName
                }
            });
        });

        group.MapPost("/login", async (LoginRequest req, AuthService authService) =>
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            {
                return Results.BadRequest(new { error = "ValidationError", message = "Username and password are required." });
            }

            var result = await authService.LoginAsync(req.Username.Trim(), req.Password);
            if (result == null)
            {
                return Results.Json(new { error = "Unauthorized", message = "Invalid username or password." }, statusCode: StatusCodes.Status401Unauthorized);
            }

            var (user, token) = result.Value;
            var expiresAt = DateTime.UtcNow.AddHours(24);

            return Results.Ok(new
            {
                token,
                expiresAt,
                player = new
                {
                    id = user.Id,
                    username = user.Username,
                    displayName = user.DisplayName
                }
            });
        });
    }
}
