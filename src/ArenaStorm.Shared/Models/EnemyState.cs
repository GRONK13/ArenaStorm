using ArenaStorm.Shared.Protocol;

namespace ArenaStorm.Shared.Models;

/// <summary>
/// Type classification of AI enemies in PvE co-op mode.
/// </summary>
public enum EnemyType : byte
{
    /// <summary>Standard melee infantry.</summary>
    Grunt = 0,

    /// <summary>Fast, low-health scout.</summary>
    Fast = 1,

    /// <summary>High-health heavy brawler.</summary>
    Brute = 2,

    /// <summary>Ranged shooter enemy.</summary>
    Shooter = 3
}

/// <summary>
/// Authoritative state of an AI enemy in PvE co-op survival mode.
/// </summary>
public class EnemyState
{
    /// <summary>Unique enemy instance identifier.</summary>
    public ushort Id { get; set; }

    /// <summary>Enemy class archetype.</summary>
    public EnemyType Type { get; set; }

    /// <summary>Current world X coordinate.</summary>
    public float X { get; set; }

    /// <summary>Current world Y coordinate.</summary>
    public float Y { get; set; }

    /// <summary>Aim orientation in radians.</summary>
    public float AimAngle { get; set; }

    /// <summary>Remaining health points.</summary>
    public short Health { get; set; }

    /// <summary>Maximum health points.</summary>
    public short MaxHealth { get; set; }

    /// <summary>Movement speed in world units per second.</summary>
    public float MoveSpeed { get; set; }

    /// <summary>Base attack damage dealt.</summary>
    public short Damage { get; set; }

    /// <summary>Attack cooldown timer in seconds.</summary>
    public float AttackCooldown { get; set; }

    /// <summary>Value indicating if the enemy is alive.</summary>
    public bool IsAlive => Health > 0;

    /// <summary>
    /// Applies damage to this enemy.
    /// </summary>
    /// <param name="damage">Amount of damage to inflict.</param>
    public void ApplyDamage(short damage)
    {
        Health = (short)Math.Max(0, Health - damage);
    }

    /// <summary>
    /// Projects this state into a serializable network snapshot.
    /// </summary>
    public EnemySnapshot ToSnapshot() => new()
    {
        Id = Id,
        EnemyType = (byte)Type,
        X = X,
        Y = Y,
        Health = Health
    };
}
