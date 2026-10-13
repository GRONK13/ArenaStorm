using System.Collections.Concurrent;
using LiteNetLib;
using ArenaStorm.Shared.Protocol;

namespace ArenaStorm.Client.Networking;

/// <summary>
/// High-level session manager handling game server communication, snapshots, and input forwarding.
/// </summary>
public class ServerConnection
{
    /// <summary>
    /// Gets the underlying client network transport manager.
    /// </summary>
    public ClientNetworkManager NetworkManager { get; }

    /// <summary>
    /// Gets a thread-safe queue containing received world snapshot packets.
    /// </summary>
    public ConcurrentQueue<ServerSnapshotPacket> SnapshotQueue { get; } = new();

    /// <summary>
    /// Gets a value indicating whether a connection to the game server is active.
    /// </summary>
    public bool IsConnected => NetworkManager.IsConnected;

    /// <summary>
    /// Gets the round-trip latency to the server in milliseconds.
    /// </summary>
    public int Ping => NetworkManager.Ping;

    /// <summary>
    /// Gets the authoritative simulation tick number from the most recent snapshot.
    /// </summary>
    public uint LastServerTick { get; private set; }

    /// <summary>Invoked upon establishing connection with the game server.</summary>
    public event Action? OnConnected;

    /// <summary>Invoked upon disconnection from the game server.</summary>
    public event Action<DisconnectInfo>? OnDisconnected;

    /// <summary>Invoked when another player joins the active match session.</summary>
    public event Action<ServerPlayerJoinedPacket>? OnPlayerJoined;

    /// <summary>Invoked when another player leaves the active match session.</summary>
    public event Action<ServerPlayerLeftPacket>? OnPlayerLeft;

    /// <summary>Invoked when an elimination event occurs in the match session.</summary>
    public event Action<ServerKillEventPacket>? OnKillEvent;

    /// <summary>Invoked when damage is dealt in the match session.</summary>
    public event Action<ServerDamageEventPacket>? OnDamageEvent;

    /// <summary>Invoked upon receiving authoritative welcome parameters from the server.</summary>
    public event Action<ServerWelcomePacket>? OnWelcome;

    /// <summary>Invoked when a player executes a melee attack.</summary>
    public event Action<ServerMeleeAttackPacket>? OnMeleeAttack;

    /// <summary>Invoked when a chat message is relayed from the server.</summary>
    public event Action<ServerChatRelayPacket>? OnChatRelay;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerConnection"/> class.
    /// </summary>
    public ServerConnection()
    {
        NetworkManager = new ClientNetworkManager();
        NetworkManager.OnConnected += () => OnConnected?.Invoke();
        NetworkManager.OnDisconnected += info => OnDisconnected?.Invoke(info);
        NetworkManager.OnMessageReceived += HandleMessageReceived;
    }

    /// <summary>
    /// Connects to the specified game server.
    /// </summary>
    /// <param name="host">Host address.</param>
    /// <param name="port">UDP port.</param>
    public void Connect(string host, int port)
    {
        NetworkManager.Connect(host, port);
    }

    /// <summary>
    /// Disconnects from the game server.
    /// </summary>
    public void Disconnect()
    {
        NetworkManager.Disconnect();
    }

    /// <summary>
    /// Polls underlying network events. Call this during the game update cycle.
    /// </summary>
    public void Update()
    {
        NetworkManager.PollEvents();
    }

    /// <summary>
    /// Transmits client input actions to the game server.
    /// <summary>
    /// Transmits client input actions to the game server.
    /// </summary>
    /// <param name="input">The client input packet.</param>
    public void SendInput(ClientInputPacket input)
    {
        NetworkManager.Send(MessageType.ClientInput, input, DeliveryMethod.Unreliable);
    }

    /// <summary>
    /// Sends a text chat message to be relayed to all match participants.
    /// </summary>
    /// <param name="message">Chat message content.</param>
    public void SendChat(string message)
    {
        NetworkManager.Send(MessageType.ClientChatMessage, new ClientChatMessagePacket { Message = message }, DeliveryMethod.ReliableOrdered);
    }

    /// <summary>
    /// Sends character archetype selection to the game server.
    /// </summary>
    /// <param name="characterClass">Selected class.</param>
    public void SendClassSelect(ArenaStorm.Shared.Models.CharacterClass characterClass)
    {
        NetworkManager.Send(MessageType.ClientClassSelect, new ClientClassSelectPacket { ClassId = (byte)characterClass }, DeliveryMethod.ReliableOrdered);
    }

    /// <summary>
    /// Dequeues and returns the latest available world snapshot, dropping older intermediate snapshots.
    /// </summary>
    /// <returns>The newest snapshot, or <c>null</c> if no new snapshot is pending.</returns>
    public ServerSnapshotPacket? GetLatestSnapshot()
    {
        ServerSnapshotPacket? latest = null;
        while (SnapshotQueue.TryDequeue(out var snapshot))
        {
            latest = snapshot;
        }

        return latest;
    }

    private void HandleMessageReceived(MessageType type, ReadOnlyMemory<byte> payload)
    {
        switch (type)
        {
            case MessageType.ServerWelcome:
            {
                ServerWelcomePacket welcome = PacketSerializer.DeserializePayload<ServerWelcomePacket>(payload);
                OnWelcome?.Invoke(welcome);
                break;
            }
            case MessageType.ServerSnapshot:
            {
                ServerSnapshotPacket snapshot = PacketSerializer.DeserializePayload<ServerSnapshotPacket>(payload);
                LastServerTick = snapshot.Tick;
                SnapshotQueue.Enqueue(snapshot);
                break;
            }
            case MessageType.ServerPlayerJoined:
            {
                ServerPlayerJoinedPacket joined = PacketSerializer.DeserializePayload<ServerPlayerJoinedPacket>(payload);
                OnPlayerJoined?.Invoke(joined);
                break;
            }
            case MessageType.ServerPlayerLeft:
            {
                ServerPlayerLeftPacket left = PacketSerializer.DeserializePayload<ServerPlayerLeftPacket>(payload);
                OnPlayerLeft?.Invoke(left);
                break;
            }
            case MessageType.ServerDamageEvent:
            {
                ServerDamageEventPacket dmg = PacketSerializer.DeserializePayload<ServerDamageEventPacket>(payload);
                OnDamageEvent?.Invoke(dmg);
                break;
            }
            case MessageType.ServerKillEvent:
            {
                ServerKillEventPacket kill = PacketSerializer.DeserializePayload<ServerKillEventPacket>(payload);
                OnKillEvent?.Invoke(kill);
                break;
            }
            case MessageType.ServerMeleeAttack:
            {
                ServerMeleeAttackPacket melee = PacketSerializer.DeserializePayload<ServerMeleeAttackPacket>(payload);
                OnMeleeAttack?.Invoke(melee);
                break;
            }
            case MessageType.ServerChatRelay:
            {
                ServerChatRelayPacket chat = PacketSerializer.DeserializePayload<ServerChatRelayPacket>(payload);
                OnChatRelay?.Invoke(chat);
                break;
            }
        }
    }
}
