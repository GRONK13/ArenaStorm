namespace ArenaStorm.WebAPI.Entities;

/// <summary>
/// Represents lifetime gameplay statistics for a player.
/// </summary>
public class PlayerStats
{
    /// <summary>
    /// Gets or sets the unique identifier for the stats record.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the foreign key referencing the associated user.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the associated user.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the total number of kills achieved.
    /// </summary>
    public int TotalKills { get; set; }

    /// <summary>
    /// Gets or sets the total number of deaths recorded.
    /// </summary>
    public int TotalDeaths { get; set; }

    /// <summary>
    /// Gets or sets the total number of games played.
    /// </summary>
    public int GamesPlayed { get; set; }

    /// <summary>
    /// Gets or sets the total number of games won.
    /// </summary>
    public int GamesWon { get; set; }

    /// <summary>
    /// Gets the kill-to-death ratio computed from total kills and total deaths.
    /// </summary>
    public double KdRatio => TotalDeaths == 0 ? TotalKills : Math.Round((double)TotalKills / TotalDeaths, 2);

    /// <summary>
    /// Gets or sets the timestamp when stats were last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
