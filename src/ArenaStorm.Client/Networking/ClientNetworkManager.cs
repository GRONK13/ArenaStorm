using LiteNetLib;
using ArenaStorm.Shared.Constants;
using ArenaStorm.Shared.Protocol;

namespace ArenaStorm.Client.Networking;

/// <summary>
/// Low-level UDP networking manager wrapping LiteNetLib client functionality.
/// </summary>
public class ClientNetworkManager
{
    private readonly EventBasedNetListener _listener;
    private readonly NetManager _netManager;
    private NetPeer? _serverPeer;

    /// <summary>
    /// Invoked when a network connection to the server has been successfully established.
    /// </summary>
    public event Action? OnConnected;

    /// <summary>
    /// Invoked when the client disconnects or loses connection with the server.
    /// </summary>
    public event Action<DisconnectInfo>? OnDisconnected;

    /// <summary>
    /// Invoked when an application packet is received from the server.
    /// </summary>
    public event Action<MessageType, ReadOnlyMemory<byte>>? OnMessageReceived;

    /// <summary>
    /// Gets a value indicating whether the client is currently connected to a remote server.
    /// </summary>
    public bool IsConnected => _serverPeer is { ConnectionState: ConnectionState.Connected };

    /// <summary>
    /// Gets the estimated round-trip latency to the server in milliseconds.
    /// </summary>
    public int Ping => _serverPeer?.Ping ?? 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientNetworkManager"/> class.
    /// </summary>
    public ClientNetworkManager()
    {
        _listener = new EventBasedNetListener();
        _netManager = new NetManager(_listener)
        {
            DisconnectTimeout = NetworkConstants.DisconnectTimeout,
            AutoRecycle = true
        };

        _listener.PeerConnectedEvent += peer =>
        {
            _serverPeer = peer;
            OnConnected?.Invoke();
        };

        _listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
        {
            _serverPeer = null;
            OnDisconnected?.Invoke(disconnectInfo);
        };

        _listener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) =>
        {
            byte[] data = reader.GetRemainingBytes();
            if (data.Length > 0)
            {
                ReadOnlyMemory<byte> payload = PacketSerializer.Deserialize(data, out MessageType messageType);
                OnMessageReceived?.Invoke(messageType, payload);
            }
        };
    }

    /// <summary>
    /// Initiates an asynchronous connection attempt to the specified host and port.
    /// </summary>
    /// <param name="host">Server hostname or IP address.</param>
    /// <param name="port">Server UDP port.</param>
    public void Connect(string host, int port)
    {
        if (!_netManager.IsRunning)
        {
            _netManager.Start();
        }

        _serverPeer = _netManager.Connect(host, port, NetworkConstants.ConnectionKey);
    }

    /// <summary>
    /// Terminates any existing connection and shuts down the underlying networking socket.
    /// </summary>
    public void Disconnect()
    {
        if (_serverPeer != null)
        {
            _serverPeer.Disconnect();
            _serverPeer = null;
        }

        if (_netManager.IsRunning)
        {
            _netManager.Stop();
        }
    }

    /// <summary>
    /// Dispatches pending network events, firing registered event callbacks.
    /// </summary>
    public void PollEvents()
    {
        if (_netManager.IsRunning)
        {
            _netManager.PollEvents();
        }
    }

    /// <summary>
    /// Serializes and sends a message packet to the connected server.
    /// </summary>
    /// <typeparam name="T">Message payload type.</typeparam>
    /// <param name="type">Protocol message identifier.</param>
    /// <param name="message">Payload instance.</param>
    /// <param name="method">LiteNetLib delivery guarantee method.</param>
    public void Send<T>(MessageType type, T message, DeliveryMethod method)
    {
        if (!IsConnected || _serverPeer == null)
        {
            return;
        }

        byte[] packet = PacketSerializer.Serialize(type, message);
        _serverPeer.Send(packet, method);
    }
}
