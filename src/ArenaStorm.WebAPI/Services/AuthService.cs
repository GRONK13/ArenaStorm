namespace ArenaStorm.WebAPI.Services;

using ArenaStorm.WebAPI.Data;
using ArenaStorm.WebAPI.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Service providing user registration, authentication, and token generation.
/// </summary>
public class AuthService(AppDbContext dbContext, JwtService jwtService)
{
    /// <summary>
    /// Registers a new user account with hashed password and initialized statistics.
    /// </summary>
    /// <param name="username">Unique username for the account.</param>
    /// <param name="password">Plain text password to be hashed.</param>
    /// <param name="displayName">Public display name for the user.</param>
    /// <returns>A tuple containing the created user and JWT token, or null if username already exists.</returns>
    public async Task<(User user, string token)?> RegisterAsync(string username, string password, string displayName)
    {
        var normalizedUsername = username.Trim();
        var exists = await dbContext.Users
            .AnyAsync(u => u.Username.ToLower() == normalizedUsername.ToLower());

        if (exists)
        {
            return null;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User
        {
            Username = normalizedUsername,
            PasswordHash = passwordHash,
            DisplayName = displayName.Trim(),
            CharacterClass = "ranger",
            CreatedAt = DateTime.UtcNow,
            LastLogin = DateTime.UtcNow
        };

        var stats = new PlayerStats
        {
            UserId = user.Id,
            User = user,
            TotalKills = 0,
            TotalDeaths = 0,
            GamesPlayed = 0,
            GamesWon = 0,
            UpdatedAt = DateTime.UtcNow
        };

        user.Stats = stats;
        dbContext.Users.Add(user);
        dbContext.PlayerStats.Add(stats);
        await dbContext.SaveChangesAsync();

        var token = jwtService.GenerateToken(user);
        return (user, token);
    }

    /// <summary>
    /// Authenticates a user with username and password, updating the last login timestamp.
    /// </summary>
    /// <param name="username">Account username.</param>
    /// <param name="password">Account plain text password.</param>
    /// <returns>A tuple containing the authenticated user and JWT token, or null if credentials are invalid.</returns>
    public async Task<(User user, string token)?> LoginAsync(string username, string password)
    {
        var normalizedUsername = username.Trim();
        var user = await dbContext.Users
            .Include(u => u.Stats)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUsername.ToLower());

        if (user == null)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return null;
        }

        user.LastLogin = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        var token = jwtService.GenerateToken(user);
        return (user, token);
    }
}
