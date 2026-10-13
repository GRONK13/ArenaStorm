namespace ArenaStorm.Shared.Protocol;

/// <summary>
/// Identifies the network message type transmitted in packet headers.
/// </summary>
public enum MessageType : byte
{
    // Client -> Server
    /// <summary>Client movement and action input packet.</summary>
    ClientInput = 0x01,

    /// <summary>Client signaling readiness to start the match.</summary>
    ClientReady = 0x02,

    /// <summary>Client text chat message.</summary>
    ClientChatMessage = 0x03,

    /// <summary>Client character class selection.</summary>
    ClientClassSelect = 0x04,

    // Server -> Client
    /// <summary>Full world state snapshot from the server.</summary>
    ServerSnapshot = 0x10,

    /// <summary>Delta compressed world state from the server.</summary>
    ServerDelta = 0x11,

    /// <summary>Notification that a player has joined the session.</summary>
    ServerPlayerJoined = 0x12,

    /// <summary>Notification that a player has left the session.</summary>
    ServerPlayerLeft = 0x13,

    /// <summary>Notification that a match has started.</summary>
    ServerMatchStart = 0x14,

    /// <summary>Notification that a match has concluded.</summary>
    ServerMatchEnd = 0x15,

    /// <summary>Damage event inflicted on an entity.</summary>
    ServerDamageEvent = 0x16,

    /// <summary>Kill event where a player was defeated.</summary>
    ServerKillEvent = 0x17,

    /// <summary>Spawn event for an item/pickup.</summary>
    ServerSpawnPickup = 0x18,

    /// <summary>Relayed chat message broadcast to clients.</summary>
    ServerChatRelay = 0x19,

    /// <summary>Notification of a player respawn.</summary>
    ServerRespawn = 0x1A,

    /// <summary>Welcome handshake establishing player session parameters.</summary>
    ServerWelcome = 0x20,

    /// <summary>Visual and auditory trigger for a melee swing.</summary>
    ServerMeleeAttack = 0x21,

    // Bidirectional
    /// <summary>Heartbeat ping request.</summary>
    Ping = 0xF0,

    /// <summary>Heartbeat pong response.</summary>
    Pong = 0xF1
}
