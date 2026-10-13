namespace ArenaStorm.WebAPI.Entities;

/// <summary>
/// Represents a leaderboard ranking entry for a player in a specific category.
/// </summary>
public class LeaderboardEntry
{
    /// <summary>
    /// Gets or sets the unique identifier for the leaderboard entry.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the foreign key referencing the ranked user.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the ranked user.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the leaderboard category ("kills", "wins", "kd").
    /// </summary>
    public string LeaderboardType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the metric value for this ranking category.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Gets or sets the current ordinal rank of the player in this category.
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the leaderboard entry was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
