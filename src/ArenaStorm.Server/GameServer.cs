using System.Diagnostics;
using ArenaStorm.Server.Game;
using ArenaStorm.Server.Networking;
using ArenaStorm.Shared.Constants;
using ArenaStorm.Shared.Models;
using ArenaStorm.Shared.Protocol;
using LiteNetLib;
using Serilog;

namespace ArenaStorm.Server;

/// <summary>
/// Authoritative standalone game server hosting and managing network transport, game rooms, and match loops.
/// </summary>
public class GameServer
{
    private readonly NetworkManager _networkManager = new();
    private readonly Dictionary<string, GameRoom> _rooms = new();
    private readonly Dictionary<int, ClientConnection> _pendingClients = new();
    private readonly Dictionary<int, ClientConnection> _clientsByPeer = new();
    private readonly HashSet<byte> _assignedPlayerIds = [];

    private volatile bool _running;

    /// <summary>
    /// Gets the network manager wrapping LiteNetLib transport.
    /// </summary>
    public NetworkManager Network => _networkManager;

    /// <summary>
    /// Gets the read-only dictionary of active rooms indexed by room ID.
    /// </summary>
    public IReadOnlyDictionary<string, GameRoom> Rooms => _rooms;

    /// <summary>
    /// Gets the read-only dictionary of clients in the pending (unassigned) state.
    /// </summary>
    public IReadOnlyDictionary<int, ClientConnection> PendingClients => _pendingClients;

    /// <summary>
    /// Initializes event subscriptions and starts the network listener on the configured port.
    /// </summary>
    /// <param name="port">UDP port to listen on.</param>
    public void Start(int port)
    {
        _networkManager.OnClientConnected = HandleClientConnected;
        _networkManager.OnClientDisconnected = HandleClientDisconnected;
        _networkManager.OnMessageReceived = HandleMessageReceived;

        _networkManager.Start(port);
        CreateRoom("default", new GameSettings("arena_1", GameMode.PvpDeathmatch, GameConstants.MaxPlayersPerMatch));

        _running = true;
        Log.Information("GameServer successfully started on UDP port {Port}.", port);
    }

    /// <summary>
    /// Executes the server update tick: polls network events and advances all active game rooms.
    /// </summary>
    public void Update()
    {
        _networkManager.PollEvents();

        foreach (var room in _rooms.Values)
        {
            room.Update(GameConstants.TickInterval);
        }
    }

    /// <summary>
    /// Runs the fixed-timestep authoritative simulation loop at 30Hz until stopped.
    /// </summary>
    public void Run()
    {
        var stopwatch = Stopwatch.StartNew();
        double accumulator = 0;

        while (_running)
        {
            double elapsed = stopwatch.Elapsed.TotalSeconds;
            stopwatch.Restart();
            accumulator += elapsed;

            while (accumulator >= GameConstants.TickInterval)
            {
                Update();
                accumulator -= GameConstants.TickInterval;
            }

            Thread.Sleep(1); // prevent busy-wait
        }
    }

    /// <summary>
    /// Halts server execution and cleans up rooms and open network connections.
    /// </summary>
    public void Stop()
    {
        _running = false;

        foreach (var room in _rooms.Values)
        {
            room.EndMatch();
        }

        _rooms.Clear();
        _pendingClients.Clear();
        _clientsByPeer.Clear();
        _assignedPlayerIds.Clear();

        _networkManager.Stop();
        Log.Information("GameServer stopped and network sockets closed.");
    }

    /// <summary>
    /// Instantiates and registers a new game room instance.
    /// </summary>
    /// <param name="roomId">Unique room identifier.</param>
    /// <param name="settings">Match configuration settings.</param>
    /// <returns>The newly created <see cref="GameRoom"/> instance.</returns>
    public GameRoom CreateRoom(string roomId, GameSettings settings)
    {
        var room = new GameRoom(roomId, settings, _networkManager);
        _rooms[roomId] = room;
        Log.Information("Created game room {RoomId} ({Mode} on {MapId}).", roomId, settings.Mode, settings.MapId);
        return room;
    }

    /// <summary>
    /// Closes and removes a game room from active rotation.
    /// </summary>
    /// <param name="roomId">Identifier of room to remove.</param>
    /// <returns><c>true</c> if the room was found and removed; otherwise, <c>false</c>.</returns>
    public bool RemoveRoom(string roomId)
    {
        if (_rooms.Remove(roomId, out var room))
        {
            room.EndMatch();
            Log.Information("Removed game room {RoomId}.", roomId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Transitions a client from pending state into the designated game room.
    /// </summary>
    /// <param name="client">Connected client connection.</param>
    /// <param name="roomId">Target room identifier.</param>
    public void AssignPlayerToRoom(ClientConnection client, string roomId)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
        {
            Log.Warning("Failed to assign player {PlayerId}: room {RoomId} does not exist.", client.PlayerId, roomId);
            return;
        }

        _pendingClients.Remove(client.Peer.Id);
        room.AddPlayer(client);
        Log.Information("Assigned player {PlayerId} to room {RoomId}.", client.PlayerId, roomId);
    }

    private void HandleClientConnected(NetPeer peer)
    {
        byte? freeId = null;
        for (byte i = 0; i < GameConstants.MaxPlayersPerMatch; i++)
        {
            if (!_assignedPlayerIds.Contains(i))
            {
                freeId = i;
                break;
            }
        }

        if (freeId == null)
        {
            Log.Warning("Server connection limit reached. Rejecting peer {PeerId}.", peer.Id);
            peer.Disconnect();
            return;
        }

        byte playerId = freeId.Value;
        _assignedPlayerIds.Add(playerId);

        var client = new ClientConnection(playerId, peer);
        _clientsByPeer[peer.Id] = client;
        _pendingClients[peer.Id] = client;

        Log.Information("Peer {PeerId} connected. Assigned PlayerId {PlayerId}. Awaiting room assignment.", peer.Id, playerId);

        // Auto-assign to default room if available
        if (_rooms.TryGetValue("default", out var defaultRoom) && defaultRoom.PlayerCount < defaultRoom.Settings.MaxPlayers)
        {
            AssignPlayerToRoom(client, "default");
        }
    }

    private void HandleClientDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (_clientsByPeer.Remove(peer.Id, out var client))
        {
            _assignedPlayerIds.Remove(client.PlayerId);
            _pendingClients.Remove(peer.Id);

            if (client.RoomId != null && _rooms.TryGetValue(client.RoomId, out var room))
            {
                room.RemovePlayer(client.PlayerId);
            }

            Log.Information("Peer {PeerId} (PlayerId: {PlayerId}) disconnected: {Reason}.", peer.Id, client.PlayerId, disconnectInfo.Reason);
        }
    }

    private void HandleMessageReceived(NetPeer peer, MessageType type, ReadOnlyMemory<byte> payload)
    {
        if (!_clientsByPeer.TryGetValue(peer.Id, out var client))
        {
            return;
        }

        switch (type)
        {
            case MessageType.Ping:
            {
                var ping = PacketSerializer.DeserializePayload<PingPacket>(payload);
                var pong = new PongPacket
                {
                    ClientTimestamp = ping.ClientTimestamp,
                    ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                _networkManager.SendToPeer(peer, PacketSerializer.Serialize(MessageType.Pong, pong), DeliveryMethod.Unreliable);
                break;
            }

            case MessageType.ClientInput:
            {
                if (client.RoomId != null && _rooms.TryGetValue(client.RoomId, out var room))
                {
                    var input = PacketSerializer.DeserializePayload<ClientInputPacket>(payload);
                    room.ProcessInput(client.PlayerId, input);
                }
                break;
            }

            case MessageType.ClientReady:
            {
                var ready = PacketSerializer.DeserializePayload<ClientReadyPacket>(payload);
                if (client.RoomId == null && _rooms.TryGetValue("default", out var defaultRoom))
                {
                    AssignPlayerToRoom(client, "default");
                }

                if (client.RoomId != null && _rooms.TryGetValue(client.RoomId, out var activeRoom))
                {
                    if (ready.IsReady && activeRoom.State == GameRoomState.Waiting)
                    {
                        activeRoom.StartMatch();
                    }
                }
                break;
            }

            case MessageType.ClientClassSelect:
            {
                var classSelect = PacketSerializer.DeserializePayload<ClientClassSelectPacket>(payload);
                if (client.RoomId != null && _rooms.TryGetValue(client.RoomId, out var room) &&
                    room.World.Players.TryGetValue(client.PlayerId, out var player))
                {
                    player.CharacterClass = (CharacterClass)classSelect.ClassId;
                    var classData = CharacterClassRegistry.Get(player.CharacterClass);
                    player.MaxHealth = classData.MaxHealth;
                    player.Health = classData.MaxHealth;
                }
                break;
            }

            case MessageType.ClientChatMessage:
            {
                var chat = PacketSerializer.DeserializePayload<ClientChatMessagePacket>(payload);
                if (client.RoomId != null && _rooms.TryGetValue(client.RoomId, out var room))
                {
                    room.BroadcastChat(client.PlayerId, chat.Message);
                }
                break;
            }
        }
    }
}
