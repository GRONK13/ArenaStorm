using ArenaStorm.Shared.Protocol;

namespace ArenaStorm.Shared.Models;

/// <summary>
/// Authoritative state of a collectible pickup in the arena world.
/// </summary>
public class PickupState
{
    /// <summary>Unique pickup identifier.</summary>
    public ushort Id { get; set; }

    /// <summary>World X coordinate.</summary>
    public float X { get; set; }

    /// <summary>World Y coordinate.</summary>
    public float Y { get; set; }

    /// <summary>Type category of this pickup item.</summary>
    public PickupType Type { get; set; }

    /// <summary>Whether the pickup is active and available for collection.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Remaining respawn cooldown in seconds once collected.</summary>
    public float RespawnTimer { get; set; }

    /// <summary>Total respawn interval in seconds.</summary>
    public float RespawnInterval { get; set; } = 20f;

    /// <summary>
    /// Projects this state into a serializable network snapshot.
    /// </summary>
    public PickupSnapshot ToSnapshot() => new()
    {
        Id = Id,
        X = X,
        Y = Y,
        PickupType = (byte)Type,
        IsActive = IsActive
    };
}
