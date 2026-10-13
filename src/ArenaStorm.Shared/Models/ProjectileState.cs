using ArenaStorm.Shared.Protocol;

namespace ArenaStorm.Shared.Models;

/// <summary>
/// Authoritative state and simulation logic for an in-flight projectile.
/// </summary>
public class ProjectileState
{
    /// <summary>Gets or sets the unique projectile identifier.</summary>
    public ushort Id { get; set; }

    /// <summary>Gets or sets the current X position in world space.</summary>
    public float X { get; set; }

    /// <summary>Gets or sets the current Y position in world space.</summary>
    public float Y { get; set; }

    /// <summary>Gets or sets the movement trajectory angle in radians.</summary>
    public float Angle { get; set; }

    /// <summary>Gets or sets the travel speed in units per second.</summary>
    public float Speed { get; set; }

    /// <summary>Gets or sets the player identifier of the projectile originator.</summary>
    public byte OwnerId { get; set; }

    /// <summary>Gets or sets the projectile archetype category.</summary>
    public byte ProjectileType { get; set; }

    /// <summary>Gets or sets the impact damage dealt to hit targets.</summary>
    public short Damage { get; set; }

    /// <summary>Gets or sets the time elapsed in seconds since creation.</summary>
    public float Lifetime { get; set; }

    /// <summary>Gets or sets the maximum lifespan in seconds before expiration.</summary>
    public float MaxLifetime { get; set; }

    /// <summary>
    /// Gets a value indicating whether the projectile's active lifetime has lapsed.
    /// </summary>
    public bool IsExpired => Lifetime >= MaxLifetime;

    /// <summary>
    /// Advances the projectile position along its heading and increments lifetime.
    /// </summary>
    /// <param name="deltaTime">Elapsed frame time in seconds.</param>
    public void Update(float deltaTime)
    {
        X += MathF.Cos(Angle) * Speed * deltaTime;
        Y += MathF.Sin(Angle) * Speed * deltaTime;
        Lifetime += deltaTime;
    }

    /// <summary>
    /// Converts this runtime projectile state into a lightweight network snapshot.
    /// </summary>
    /// <returns>A new <see cref="ProjectileSnapshot"/> instance.</returns>
    public ProjectileSnapshot ToSnapshot()
    {
        return new ProjectileSnapshot
        {
            Id = Id,
            X = X,
            Y = Y,
            Angle = Angle,
            OwnerId = OwnerId,
            ProjectileType = ProjectileType
        };
    }
}
