using ArenaStorm.Shared.Constants;
using ArenaStorm.Shared.Protocol;
using LiteNetLib;

namespace ArenaStorm.Server.Networking;

/// <summary>
/// Manages low-level UDP network transport, connections, and event dispatching using LiteNetLib.
/// </summary>
public class NetworkManager
{
    private EventBasedNetListener? _listener;
    private NetManager? _netManager;

    /// <summary>
    /// Invoked when a remote peer successfully completes handshake and connects.
    /// </summary>
    public Action<NetPeer>? OnClientConnected { get; set; }

    /// <summary>
    /// Invoked when a remote peer disconnects or times out.
    /// </summary>
    public Action<NetPeer, DisconnectInfo>? OnClientDisconnected { get; set; }

    /// <summary>
    /// Invoked when a network packet is received and parsed into a message type and payload.
    /// </summary>
    public Action<NetPeer, MessageType, ReadOnlyMemory<byte>>? OnMessageReceived { get; set; }

    /// <summary>
    /// Gets a value indicating whether the network transport is running.
    /// </summary>
    public bool IsRunning => _netManager != null && _netManager.IsRunning;

    /// <summary>
    /// Initializes network listener, starts the UDP server manager, and begins accepting connections.
    /// </summary>
    /// <param name="port">The UDP port to listen on.</param>
    public void Start(int port)
    {
        _listener = new EventBasedNetListener();
        _netManager = new NetManager(_listener)
        {
            DisconnectTimeout = NetworkConstants.DisconnectTimeout,
            PingInterval = NetworkConstants.PingInterval,
            AutoRecycle = false
        };

        _listener.ConnectionRequestEvent += request =>
        {
            if (_netManager.ConnectedPeersCount < NetworkConstants.MaxConnections)
            {
                request.AcceptIfKey(NetworkConstants.ConnectionKey);
            }
            else
            {
                request.Reject();
            }
        };

        _listener.PeerConnectedEvent += OnPeerConnected;
        _listener.PeerDisconnectedEvent += OnPeerDisconnected;
        _listener.NetworkReceiveEvent += OnNetworkReceive;

        _netManager.Start(port);
    }

    /// <summary>
    /// Polls pending network socket events and dispatches queued callbacks.
    /// Must be invoked regularly from the authoritative server loop.
    /// </summary>
    public void PollEvents()
    {
        _netManager?.PollEvents();
    }

    /// <summary>
    /// Stops the network server and disconnects all active peers.
    /// </summary>
    public void Stop()
    {
        if (_netManager != null && _netManager.IsRunning)
        {
            _netManager.Stop();
        }

        _netManager = null;
        _listener = null;
    }

    /// <summary>
    /// Transmits a byte payload to all connected peers using the specified delivery method.
    /// </summary>
    /// <param name="data">Raw byte array to send.</param>
    /// <param name="method">LiteNetLib packet delivery reliability guarantee.</param>
    public void SendToAll(byte[] data, DeliveryMethod method)
    {
        _netManager?.SendToAll(data, method);
    }

    /// <summary>
    /// Transmits a byte payload to a specific peer using the specified delivery method.
    /// </summary>
    /// <param name="peer">Target remote peer.</param>
    /// <param name="data">Raw byte array to send.</param>
    /// <param name="method">LiteNetLib packet delivery reliability guarantee.</param>
    public void SendToPeer(NetPeer peer, byte[] data, DeliveryMethod method)
    {
        if (peer.ConnectionState == ConnectionState.Connected)
        {
            peer.Send(data, method);
        }
    }

    private void OnPeerConnected(NetPeer peer)
    {
        OnClientConnected?.Invoke(peer);
    }

    private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        OnClientDisconnected?.Invoke(peer, disconnectInfo);
    }

    private void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        try
        {
            byte[] data = reader.GetRemainingBytes();
            if (data.Length > 0)
            {
                var payload = PacketSerializer.Deserialize(data, out var messageType);
                OnMessageReceived?.Invoke(peer, messageType, payload);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to deserialize packet from peer {PeerId}", peer.Id);
        }
        finally
        {
            reader.Recycle();
        }
    }
}
