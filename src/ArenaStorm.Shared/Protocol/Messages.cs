using MessagePack;

namespace ArenaStorm.Shared.Protocol;

/// <summary>
/// Bitmask flags representing player movement and combat input states.
/// </summary>
public static class InputFlags
{
    /// <summary>No input flags active.</summary>
    public const byte None = 0;

    /// <summary>Move upward.</summary>
    public const byte Up = 1;

    /// <summary>Move downward.</summary>
    public const byte Down = 2;

    /// <summary>Move left.</summary>
    public const byte Left = 4;

    /// <summary>Move right.</summary>
    public const byte Right = 8;

    /// <summary>Primary attack action.</summary>
    public const byte Fire = 16;

    /// <summary>Melee or secondary attack action.</summary>
    public const byte Melee = 32;
}

/// <summary>
/// Represents client movement and action inputs sent to the server.
/// </summary>
[MessagePackObject]
public struct ClientInputPacket
{
    /// <summary>Client input sequence number for reconciliation.</summary>
    [Key(0)]
    public uint SequenceNumber { get; set; }

    /// <summary>Bitfield of active input keys.</summary>
    [Key(1)]
    public byte InputFlags { get; set; }

    /// <summary>Aim direction in radians.</summary>
    [Key(2)]
    public float AimAngle { get; set; }

    /// <summary>Client timestamp in milliseconds.</summary>
    [Key(3)]
    public uint Timestamp { get; set; }
}

/// <summary>
/// Signals that the client is ready for match commencement.
/// </summary>
[MessagePackObject]
public struct ClientReadyPacket
{
    /// <summary>Gets or sets a value indicating whether the client is ready.</summary>
    [Key(0)]
    public bool IsReady { get; set; }
}

/// <summary>
/// Compressed state of a single player at a specific tick.
/// </summary>
[MessagePackObject]
public struct PlayerSnapshot
{
    /// <summary>Unique session player identifier.</summary>
    [Key(0)]
    public byte PlayerId { get; set; }

    /// <summary>X coordinate in world space.</summary>
    [Key(1)]
    public float X { get; set; }

    /// <summary>Y coordinate in world space.</summary>
    [Key(2)]
    public float Y { get; set; }

    /// <summary>Aim angle in radians.</summary>
    [Key(3)]
    public float AimAngle { get; set; }

    /// <summary>Current health points.</summary>
    [Key(4)]
    public short Health { get; set; }

    /// <summary>Selected character class identifier.</summary>
    [Key(5)]
    public byte ClassId { get; set; }

    /// <summary>Current animation or movement state identifier.</summary>
    [Key(6)]
    public byte AnimationState { get; set; }

    /// <summary>Current shield points absorbing damage.</summary>
    [Key(7)]
    public short Shield { get; set; }
}

/// <summary>
/// Compressed state of a projectile at a specific tick.
/// </summary>
[MessagePackObject]
public struct ProjectileSnapshot
{
    /// <summary>Unique projectile identifier.</summary>
    [Key(0)]
    public ushort Id { get; set; }

    /// <summary>X coordinate in world space.</summary>
    [Key(1)]
    public float X { get; set; }

    /// <summary>Y coordinate in world space.</summary>
    [Key(2)]
    public float Y { get; set; }

    /// <summary>Movement direction angle in radians.</summary>
    [Key(3)]
    public float Angle { get; set; }

    /// <summary>Identifier of the player who launched the projectile.</summary>
    [Key(4)]
    public byte OwnerId { get; set; }

    /// <summary>Type category of the projectile.</summary>
    [Key(5)]
    public byte ProjectileType { get; set; }
}

/// <summary>
/// State of a collectible pickup in the arena.
/// </summary>
[MessagePackObject]
public struct PickupSnapshot
{
    /// <summary>Unique pickup identifier.</summary>
    [Key(0)]
    public ushort Id { get; set; }

    /// <summary>X coordinate in world space.</summary>
    [Key(1)]
    public float X { get; set; }

    /// <summary>Y coordinate in world space.</summary>
    [Key(2)]
    public float Y { get; set; }

    /// <summary>Type identifier of the pickup item.</summary>
    [Key(3)]
    public byte PickupType { get; set; }

    /// <summary>Indicates whether the pickup is currently active on the map.</summary>
    [Key(4)]
    public bool IsActive { get; set; }
}

/// <summary>
/// State of an AI-controlled enemy entity in PvE mode.
/// </summary>
[MessagePackObject]
public struct EnemySnapshot
{
    /// <summary>Unique enemy instance identifier.</summary>
    [Key(0)]
    public ushort Id { get; set; }

    /// <summary>Type category of the enemy.</summary>
    [Key(1)]
    public byte EnemyType { get; set; }

    /// <summary>X world position.</summary>
    [Key(2)]
    public float X { get; set; }

    /// <summary>Y world position.</summary>
    [Key(3)]
    public float Y { get; set; }

    /// <summary>Remaining health points.</summary>
    [Key(4)]
    public short Health { get; set; }
}

/// <summary>
/// Authoritative full world snapshot sent periodically by the server.
/// </summary>
[MessagePackObject]
public struct ServerSnapshotPacket
{
    /// <summary>Authoritative simulation tick number.</summary>
    [Key(0)]
    public uint Tick { get; set; }

    /// <summary>Last processed client input sequence number.</summary>
    [Key(1)]
    public uint LastProcessedInput { get; set; }

    /// <summary>Collection of active player snapshots.</summary>
    [Key(2)]
    public PlayerSnapshot[] Players { get; set; }

    /// <summary>Collection of active projectile snapshots.</summary>
    [Key(3)]
    public ProjectileSnapshot[] Projectiles { get; set; }

    /// <summary>Collection of arena pickup snapshots.</summary>
    [Key(4)]
    public PickupSnapshot[] Pickups { get; set; }

    /// <summary>Collection of PvE enemy snapshots.</summary>
    [Key(5)]
    public EnemySnapshot[]? Enemies { get; set; }
}

/// <summary>
/// Notification that a new player has joined the game session.
/// </summary>
[MessagePackObject]
public struct ServerPlayerJoinedPacket
{
    /// <summary>Assigned player identifier.</summary>
    [Key(0)]
    public byte PlayerId { get; set; }

    /// <summary>Display name of the joining player.</summary>
    [Key(1)]
    public string DisplayName { get; set; }
}

/// <summary>
/// Notification that a player has disconnected or left the game session.
/// </summary>
[MessagePackObject]
public struct ServerPlayerLeftPacket
{
    /// <summary>Identifier of the player who left.</summary>
    [Key(0)]
    public byte PlayerId { get; set; }
}

/// <summary>
/// Broadcast notification when a match starts.
/// </summary>
[MessagePackObject]
public struct ServerMatchStartPacket
{
    /// <summary>Identifier of the map being played.</summary>
    [Key(0)]
    public string MapId { get; set; }

    /// <summary>Game mode identifier.</summary>
    [Key(1)]
    public byte GameMode { get; set; }

    /// <summary>Simulation tick rate in Hz.</summary>
    [Key(2)]
    public byte TickRate { get; set; }
}

/// <summary>
/// Broadcast notification when a match concludes.
/// </summary>
[MessagePackObject]
public struct ServerMatchEndPacket
{
    /// <summary>Player identifier of the winner.</summary>
    [Key(0)]
    public byte WinnerPlayerId { get; set; }

    /// <summary>Final scores mapped by player identifier.</summary>
    [Key(1)]
    public Dictionary<byte, int> Scores { get; set; }
}

/// <summary>
/// Notification that damage was dealt to an entity.
/// </summary>
[MessagePackObject]
public struct ServerDamageEventPacket
{
    /// <summary>Identifier of the damaged player.</summary>
    [Key(0)]
    public byte TargetId { get; set; }

    /// <summary>Identifier of the attacking player.</summary>
    [Key(1)]
    public byte AttackerId { get; set; }

    /// <summary>Amount of damage inflicted.</summary>
    [Key(2)]
    public short Damage { get; set; }

    /// <summary>X world position of the impact.</summary>
    [Key(3)]
    public float HitX { get; set; }

    /// <summary>Y world position of the impact.</summary>
    [Key(4)]
    public float HitY { get; set; }
}

/// <summary>
/// Notification that a player was eliminated.
/// </summary>
[MessagePackObject]
public struct ServerKillEventPacket
{
    /// <summary>Identifier of the victim.</summary>
    [Key(0)]
    public byte VictimId { get; set; }

    /// <summary>Identifier of the player who scored the kill.</summary>
    [Key(1)]
    public byte KillerId { get; set; }
}

/// <summary>
/// Notification that a pickup has spawned in the arena.
/// </summary>
[MessagePackObject]
public struct ServerSpawnPickupPacket
{
    /// <summary>Unique pickup identifier.</summary>
    [Key(0)]
    public ushort Id { get; set; }

    /// <summary>X world position.</summary>
    [Key(1)]
    public float X { get; set; }

    /// <summary>Y world position.</summary>
    [Key(2)]
    public float Y { get; set; }

    /// <summary>Pickup item category type.</summary>
    [Key(3)]
    public byte PickupType { get; set; }
}

/// <summary>
/// Chat message relayed to clients from the server.
/// </summary>
[MessagePackObject]
public struct ServerChatRelayPacket
{
    /// <summary>Identifier of the player who sent the message.</summary>
    [Key(0)]
    public byte SenderId { get; set; }

    /// <summary>Content of the chat message.</summary>
    [Key(1)]
    public string Message { get; set; }

    /// <summary>Server timestamp when message was processed.</summary>
    [Key(2)]
    public uint Timestamp { get; set; }
}

/// <summary>
/// Notification of player respawn location.
/// </summary>
[MessagePackObject]
public struct ServerRespawnPacket
{
    /// <summary>Identifier of the respawned player.</summary>
    [Key(0)]
    public byte PlayerId { get; set; }

    /// <summary>Spawn X coordinate.</summary>
    [Key(1)]
    public float X { get; set; }

    /// <summary>Spawn Y coordinate.</summary>
    [Key(2)]
    public float Y { get; set; }
}

/// <summary>
/// Client request to broadcast a chat message.
/// </summary>
[MessagePackObject]
public struct ClientChatMessagePacket
{
    /// <summary>Text message content.</summary>
    [Key(0)]
    public string Message { get; set; }
}

/// <summary>
/// Client request to select a character class.
/// </summary>
[MessagePackObject]
public struct ClientClassSelectPacket
{
    /// <summary>Identifier of the chosen character class.</summary>
    [Key(0)]
    public byte ClassId { get; set; }
}

/// <summary>
/// Heartbeat ping packet containing client timestamp.
/// </summary>
[MessagePackObject]
public struct PingPacket
{
    /// <summary>Client timestamp in ticks or milliseconds.</summary>
    [Key(0)]
    public long ClientTimestamp { get; set; }
}

/// <summary>
/// Heartbeat pong packet returning timestamps for RTT calculation.
/// </summary>
[MessagePackObject]
public struct PongPacket
{
    /// <summary>Reflected client timestamp.</summary>
    [Key(0)]
    public long ClientTimestamp { get; set; }

    /// <summary>Server timestamp at response time.</summary>
    [Key(1)]
    public long ServerTimestamp { get; set; }
}

/// <summary>
/// Welcome handshake establishing the player's session parameters.
/// </summary>
[MessagePackObject]
public struct ServerWelcomePacket
{
    /// <summary>Assigned player identifier.</summary>
    [Key(0)]
    public byte PlayerId { get; set; }

    /// <summary>Active game room identifier.</summary>
    [Key(1)]
    public string RoomId { get; set; }

    /// <summary>Active map identifier.</summary>
    [Key(2)]
    public string MapId { get; set; }

    /// <summary>Authoritative simulation tick rate in Hz.</summary>
    [Key(3)]
    public byte TickRate { get; set; }
}

/// <summary>
/// Broadcast event signifying an executed melee attack swing.
/// </summary>
[MessagePackObject]
public struct ServerMeleeAttackPacket
{
    /// <summary>Identifier of the attacking player.</summary>
    [Key(0)]
    public byte AttackerId { get; set; }

    /// <summary>Direction angle of the attack in radians.</summary>
    [Key(1)]
    public float Angle { get; set; }

    /// <summary>Radius/reach of the melee strike.</summary>
    [Key(2)]
    public float Range { get; set; }
}
