namespace ArenaStorm.Shared.Models;

/// <summary>
/// Categories of collectible pickups spawning in the arena.
/// </summary>
public enum PickupType : byte
{
    /// <summary>Restores 50 HP immediately.</summary>
    Health = 0,

    /// <summary>Grants +50% movement speed for 8 seconds.</summary>
    Speed = 1,

    /// <summary>Increases attack damage by 30% for 10 seconds.</summary>
    Damage = 2,

    /// <summary>Grants an energy shield absorbing up to 50 damage.</summary>
    Shield = 3
}
