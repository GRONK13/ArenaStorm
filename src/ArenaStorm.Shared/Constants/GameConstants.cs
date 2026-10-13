namespace ArenaStorm.Shared.Constants;

/// <summary>
/// Core gameplay simulation constants and default values.
/// </summary>
public static class GameConstants
{
    /// <summary>Authoritative server physics update rate in ticks per second.</summary>
    public const int TickRate = 30;

    /// <summary>Fixed delta time duration per tick in seconds.</summary>
    public const float TickInterval = 1f / TickRate;

    /// <summary>Rate at which full world snapshots are broadcast per second.</summary>
    public const int SnapshotRate = 15;

    /// <summary>Default delay in seconds before an eliminated player respawns.</summary>
    public const float DefaultRespawnTime = 3.0f;

    /// <summary>Default target score required to win a deathmatch.</summary>
    public const int DefaultScoreLimit = 20;

    /// <summary>Default maximum match duration in seconds.</summary>
    public const float DefaultMatchDuration = 300f;

    /// <summary>Standard tile dimension in pixels/world units.</summary>
    public const int TileSize = 32;

    /// <summary>Maximum allowed players in a single match session.</summary>
    public const int MaxPlayersPerMatch = 12;

    /// <summary>Default projectile velocity in units per second.</summary>
    public const float ProjectileDefaultSpeed = 600f;

    /// <summary>Default projectile lifespan in seconds before self-destruction.</summary>
    public const float ProjectileDefaultLifetime = 2.0f;

    /// <summary>Default direct damage dealt on projectile impact.</summary>
    public const short ProjectileDefaultDamage = 15;

    /// <summary>Client entity interpolation buffer delay in seconds.</summary>
    public const float InterpolationDelay = 0.1f;
}
