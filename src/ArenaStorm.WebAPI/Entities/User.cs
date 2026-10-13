namespace ArenaStorm.WebAPI.Entities;

/// <summary>
/// Represents an application user account.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the unique username for login.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the BCrypt-hashed password.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name shown to other players.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the selected character class. Defaults to "ranger".
    /// </summary>
    public string CharacterClass { get; set; } = "ranger";

    /// <summary>
    /// Gets or sets the timestamp when the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp of the user's last login.
    /// </summary>
    public DateTime LastLogin { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the navigation property for the user's statistics.
    /// </summary>
    public PlayerStats? Stats { get; set; }
}
