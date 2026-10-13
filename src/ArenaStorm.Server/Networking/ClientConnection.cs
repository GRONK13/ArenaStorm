using System.Net;
using LiteNetLib;

namespace ArenaStorm.Server.Networking;

/// <summary>
/// Represents a connected game client session and associated metadata.
/// </summary>
/// <param name="playerId">Unique server-assigned player identifier (0-11).</param>
/// <param name="peer">LiteNetLib network peer instance.</param>
/// <param name="displayName">Human-readable player display name.</param>
public class ClientConnection(byte playerId, NetPeer peer, string displayName = "")
{
    /// <summary>
    /// Gets the unique server-assigned player identifier (0-11).
    /// </summary>
    public byte PlayerId { get; } = playerId;

    /// <summary>
    /// Gets the underlying LiteNetLib network peer.
    /// </summary>
    public NetPeer Peer { get; } = peer;

    /// <summary>
    /// Gets or sets the player display name.
    /// </summary>
    public string DisplayName { get; set; } = string.IsNullOrWhiteSpace(displayName) ? $"Player_{playerId}" : displayName;

    /// <summary>
    /// Gets or sets the active room identifier, or null if the client is in lobby/pending state.
    /// </summary>
    public string? RoomId { get; set; }

    /// <summary>
    /// Gets or sets the latest client input sequence number processed by the server.
    /// </summary>
    public uint LastProcessedInput { get; set; }

    /// <summary>
    /// Gets the UTC timestamp when the connection was established.
    /// </summary>
    public DateTime ConnectedAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the round-trip latency (ping) in milliseconds to the client peer.
    /// </summary>
    public int Latency => Peer.Ping;
}
