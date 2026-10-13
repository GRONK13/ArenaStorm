using ArenaStorm.Shared.Protocol;

namespace ArenaStorm.Shared.Models;

/// <summary>
/// Represents the authoritative live gameplay state of an active player.
/// </summary>
public class PlayerState
{
    /// <summary>Gets or sets the unique session player identifier.</summary>
    public byte PlayerId { get; set; }

    /// <summary>Gets or sets the X position coordinate in world space.</summary>
    public float X { get; set; }

    /// <summary>Gets or sets the Y position coordinate in world space.</summary>
    public float Y { get; set; }

    /// <summary>Gets or sets the player's aim orientation angle in radians.</summary>
    public float AimAngle { get; set; }

    /// <summary>Gets or sets the current hit points remaining.</summary>
    public short Health { get; set; }

    /// <summary>Gets or sets the maximum health capacity.</summary>
    public short MaxHealth { get; set; }

    /// <summary>Gets or sets the character archetype class.</summary>
    public CharacterClass CharacterClass { get; set; }

    /// <summary>Gets or sets a value indicating whether the player is currently alive.</summary>
    public bool IsAlive { get; set; } = true;

    /// <summary>Gets or sets the elapsed respawn countdown timer in seconds.</summary>
    public float RespawnTimer { get; set; }

    /// <summary>Gets or sets the total kills scored in the current match.</summary>
    public int Kills { get; set; }

    /// <summary>Gets or sets the total deaths recorded in the current match.</summary>
    public int Deaths { get; set; }

    /// <summary>Gets or sets the human-readable display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the current visual animation state identifier.</summary>
    public byte AnimationState { get; set; }

    /// <summary>Gets or sets the current energy shield points absorbing incoming damage.</summary>
    public short Shield { get; set; }

    /// <summary>Gets or sets the remaining duration of the speed boost pickup in seconds.</summary>
    public float SpeedBoostTimer { get; set; }

    /// <summary>Gets or sets the remaining duration of the damage boost pickup in seconds.</summary>
    public float DamageBoostTimer { get; set; }

    /// <summary>
    /// Inflicts damage upon this player, updating health, shield, and life status.
    /// </summary>
    /// <param name="damage">The amount of health to deduct.</param>
    public void ApplyDamage(short damage)
    {
        if (!IsAlive)
        {
            return;
        }

        if (Shield > 0)
        {
            short absorbed = (short)Math.Min((int)Shield, damage);
            Shield -= absorbed;
            damage -= absorbed;
        }

        if (damage <= 0)
        {
            return;
        }

        Health = (short)Math.Max(0, Health - damage);
        if (Health <= 0)
        {
            IsAlive = false;
            Health = 0;
            Shield = 0;
            SpeedBoostTimer = 0f;
            DamageBoostTimer = 0f;
        }
    }

    /// <summary>
    /// Respawns the player at the specified coordinate with restored health.
    /// </summary>
    /// <param name="x">Target X coordinate.</param>
    /// <param name="y">Target Y coordinate.</param>
    public void Respawn(float x, float y)
    {
        X = x;
        Y = y;
        Health = MaxHealth;
        Shield = 0;
        SpeedBoostTimer = 0f;
        DamageBoostTimer = 0f;
        IsAlive = true;
        RespawnTimer = 0f;
    }

    /// <summary>
    /// Projects this state into a serializable network snapshot.
    /// </summary>
    /// <returns>A new <see cref="PlayerSnapshot"/> representing this player.</returns>
    public PlayerSnapshot ToSnapshot()
    {
        return new PlayerSnapshot
        {
            PlayerId = PlayerId,
            X = X,
            Y = Y,
            AimAngle = AimAngle,
            Health = Health,
            ClassId = (byte)CharacterClass,
            AnimationState = AnimationState,
            Shield = Shield
        };
    }
}
