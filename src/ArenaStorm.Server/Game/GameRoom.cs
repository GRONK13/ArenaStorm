using ArenaStorm.Server.Networking;
using ArenaStorm.Shared.Constants;
using ArenaStorm.Shared.Models;
using ArenaStorm.Shared.Protocol;
using LiteNetLib;

namespace ArenaStorm.Server.Game;

/// <summary>
/// Lifecycle state of a match room instance.
/// </summary>
public enum GameRoomState
{
    /// <summary>Waiting for players to connect and signal ready.</summary>
    Waiting,

    /// <summary>Match is actively being simulated.</summary>
    Playing,

    /// <summary>Match concluded with results finalized.</summary>
    Finished
}

/// <summary>
/// Manages a single match session instance, participant connections, and world simulation.
/// </summary>
/// <param name="roomId">Unique room identifier.</param>
/// <param name="settings">Match configuration parameters.</param>
/// <param name="netManager">Underlying network manager transport.</param>
public class GameRoom(string roomId, GameSettings settings, NetworkManager netManager)
{
    private readonly NetworkManager _netManager = netManager;
    private float _elapsedMatchTime;

    /// <summary>
    /// Gets the unique room identifier.
    /// </summary>
    public string RoomId { get; } = roomId;

    /// <summary>
    /// Gets the game configuration settings for this room.
    /// </summary>
    public GameSettings Settings { get; } = settings;

    /// <summary>
    /// Gets the authoritative game world state.
    /// </summary>
    public GameWorld World { get; } = new(settings.MapId);

    /// <summary>
    /// Gets the dictionary of active client connections participating in this room.
    /// </summary>
    public Dictionary<byte, ClientConnection> Players { get; } = new();

    /// <summary>
    /// Gets the current lifecycle state of the room.
    /// </summary>
    public GameRoomState State { get; private set; } = GameRoomState.Waiting;

    /// <summary>
    /// Gets the count of active players in the room.
    /// </summary>
    public int PlayerCount => Players.Count;

    /// <summary>
    /// Adds a connected client to this room and initializes their world avatar.
    /// </summary>
    /// <param name="client">Connected client session.</param>
    public void AddPlayer(ClientConnection client)
    {
        client.RoomId = RoomId;
        Players[client.PlayerId] = client;
        World.AddPlayer(client.PlayerId, client.DisplayName, CharacterClass.Ranger);

        // Dispatch welcome handshake directly to the joining client
        var welcomePacket = new ServerWelcomePacket
        {
            PlayerId = client.PlayerId,
            RoomId = RoomId,
            MapId = Settings.MapId,
            TickRate = (byte)Settings.TickRate
        };
        _netManager.SendToPeer(client.Peer, PacketSerializer.Serialize(MessageType.ServerWelcome, welcomePacket), DeliveryMethod.ReliableOrdered);

        // Notify existing players of the new joiner
        var joinedPacket = new ServerPlayerJoinedPacket
        {
            PlayerId = client.PlayerId,
            DisplayName = client.DisplayName
        };
        byte[] joinedData = PacketSerializer.Serialize(MessageType.ServerPlayerJoined, joinedPacket);
        Broadcast(joinedData, DeliveryMethod.ReliableOrdered);

        // Inform the new player about existing players in this match
        foreach (var existing in Players.Values)
        {
            if (existing.PlayerId != client.PlayerId)
            {
                var existingJoinedPacket = new ServerPlayerJoinedPacket
                {
                    PlayerId = existing.PlayerId,
                    DisplayName = existing.DisplayName
                };
                _netManager.SendToPeer(client.Peer, PacketSerializer.Serialize(MessageType.ServerPlayerJoined, existingJoinedPacket), DeliveryMethod.ReliableOrdered);
            }
        }
    }

    /// <summary>
    /// Removes a player from the room and dispatches player left notification.
    /// </summary>
    /// <param name="playerId">Identifier of player leaving.</param>
    public void RemovePlayer(byte playerId)
    {
        if (Players.Remove(playerId, out var client))
        {
            client.RoomId = null;
            World.RemovePlayer(playerId);

            var leftPacket = new ServerPlayerLeftPacket { PlayerId = playerId };
            byte[] leftData = PacketSerializer.Serialize(MessageType.ServerPlayerLeft, leftPacket);
            Broadcast(leftData, DeliveryMethod.ReliableOrdered);
        }
    }

    /// <summary>
    /// Commences the match simulation and notifies all clients.
    /// </summary>
    public void StartMatch()
    {
        State = GameRoomState.Playing;
        _elapsedMatchTime = 0f;

        var startPacket = new ServerMatchStartPacket
        {
            MapId = Settings.MapId,
            GameMode = (byte)Settings.Mode,
            TickRate = (byte)Settings.TickRate
        };

        Broadcast(PacketSerializer.Serialize(MessageType.ServerMatchStart, startPacket), DeliveryMethod.ReliableOrdered);
    }

    /// <summary>
    /// Updates the room simulation by advancing the world and broadcasting state snapshots.
    /// </summary>
    /// <param name="deltaTime">Delta simulation time in seconds.</param>
    public void Update(float deltaTime)
    {
        if (State != GameRoomState.Playing)
        {
            return;
        }

        _elapsedMatchTime += deltaTime;
        World.Update(deltaTime);

        // Dispatch queued combat damage events
        if (World.PendingDamageEvents.Count > 0)
        {
            foreach (var damageEvent in World.PendingDamageEvents)
            {
                Broadcast(PacketSerializer.Serialize(MessageType.ServerDamageEvent, damageEvent), DeliveryMethod.ReliableOrdered);
            }
            World.PendingDamageEvents.Clear();
        }

        // Dispatch queued player kill events
        if (World.PendingKillEvents.Count > 0)
        {
            foreach (var killEvent in World.PendingKillEvents)
            {
                Broadcast(PacketSerializer.Serialize(MessageType.ServerKillEvent, killEvent), DeliveryMethod.ReliableOrdered);
            }
            World.PendingKillEvents.Clear();
        }

        // Dispatch queued player respawn events
        if (World.PendingRespawnEvents.Count > 0)
        {
            foreach (var respawnEvent in World.PendingRespawnEvents)
            {
                Broadcast(PacketSerializer.Serialize(MessageType.ServerRespawn, respawnEvent), DeliveryMethod.ReliableOrdered);
            }
            World.PendingRespawnEvents.Clear();
        }

        // Dispatch queued melee attack events
        if (World.PendingMeleeEvents.Count > 0)
        {
            foreach (var meleeEvent in World.PendingMeleeEvents)
            {
                Broadcast(PacketSerializer.Serialize(MessageType.ServerMeleeAttack, meleeEvent), DeliveryMethod.ReliableOrdered);
            }
            World.PendingMeleeEvents.Clear();
        }

        // Broadcast authoritative world snapshot to each player
        BroadcastSnapshot(_netManager);

        // Check match completion conditions
        if (Settings.Mode == GameMode.PvpElimination && Players.Count > 1)
        {
            int aliveCount = World.Players.Values.Count(p => p.IsAlive);
            if (aliveCount <= 1)
            {
                EndMatch();
                return;
            }
        }

        if (Settings.ScoreLimit > 0 && World.Players.Values.Any(p => p.Kills >= Settings.ScoreLimit))
        {
            EndMatch();
        }
        else if (Settings.MatchDuration > 0 && _elapsedMatchTime >= Settings.MatchDuration)
        {
            EndMatch();
        }
    }

    /// <summary>
    /// Serializes and sends an authoritative snapshot of the world to all room participants.
    /// </summary>
    /// <param name="netManager">Network manager used for dispatching.</param>
    public void BroadcastSnapshot(NetworkManager netManager)
    {
        foreach (var player in Players.Values)
        {
            var snapshot = World.CreateSnapshot(player.PlayerId);
            byte[] data = PacketSerializer.Serialize(MessageType.ServerSnapshot, snapshot);
            netManager.SendToPeer(player.Peer, data, DeliveryMethod.Unreliable);
        }
    }

    /// <summary>
    /// Forwards client movement and attack inputs to the world simulation.
    /// </summary>
    /// <param name="playerId">Identifier of player originating input.</param>
    /// <param name="input">Received client input packet.</param>
    public void ProcessInput(byte playerId, ClientInputPacket input)
    {
        if (Players.TryGetValue(playerId, out var client))
        {
            client.LastProcessedInput = input.SequenceNumber;
        }

        World.ProcessPlayerInput(playerId, input, GameConstants.TickInterval);
    }

    /// <summary>
    /// Relays a text chat message from a player to all room participants.
    /// </summary>
    /// <param name="senderId">Sender player ID.</param>
    /// <param name="message">Chat message text.</param>
    public void BroadcastChat(byte senderId, string message)
    {
        var chatPacket = new ServerChatRelayPacket
        {
            SenderId = senderId,
            Message = message,
            Timestamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        Broadcast(PacketSerializer.Serialize(MessageType.ServerChatRelay, chatPacket), DeliveryMethod.ReliableOrdered);
    }

    /// <summary>
    /// Concludes the active match, tallies scores, and broadcasts final results.
    /// </summary>
    public void EndMatch()
    {
        if (State == GameRoomState.Finished)
        {
            return;
        }

        State = GameRoomState.Finished;

        var scores = Players.ToDictionary(
            kvp => kvp.Key,
            kvp => World.Players.TryGetValue(kvp.Key, out var p) ? p.Kills : 0);

        byte winnerId = scores.Count > 0
            ? scores.OrderByDescending(s => s.Value).First().Key
            : (byte)0;

        var endPacket = new ServerMatchEndPacket
        {
            WinnerPlayerId = winnerId,
            Scores = scores
        };

        Broadcast(PacketSerializer.Serialize(MessageType.ServerMatchEnd, endPacket), DeliveryMethod.ReliableOrdered);
    }

    /// <summary>
    /// Broadcasts a network packet payload to all participants in this room.
    /// </summary>
    /// <param name="data">Serialized packet bytes.</param>
    /// <param name="method">LiteNetLib delivery method guarantee.</param>
    public void Broadcast(byte[] data, DeliveryMethod method)
    {
        foreach (var player in Players.Values)
        {
            _netManager.SendToPeer(player.Peer, data, method);
        }
    }
}
