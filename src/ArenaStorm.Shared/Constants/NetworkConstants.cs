namespace ArenaStorm.Shared.Constants;

/// <summary>
/// Network configuration and connection parameters for client and server.
/// </summary>
public static class NetworkConstants
{
    /// <summary>Default UDP listening port for the LiteNetLib game server.</summary>
    public const int GameServerPort = 9050;

    /// <summary>Default HTTP listening port for the Web API service.</summary>
    public const int WebApiPort = 5000;

    /// <summary>Default host address for local connection.</summary>
    public const string DefaultHost = "127.0.0.1";

    /// <summary>Maximum simultaneous peer connections permitted on the game server.</summary>
    public const int MaxConnections = 12;

    /// <summary>Connection timeout in milliseconds before a client is disconnected.</summary>
    public const int DisconnectTimeout = 5000;

    /// <summary>Interval in milliseconds between heartbeat ping messages.</summary>
    public const int PingInterval = 1000;

    /// <summary>Handshake connection key string used to validate LiteNetLib connection requests.</summary>
    public const string ConnectionKey = "ArenaStorm_v1";
}
