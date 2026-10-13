namespace ArenaStorm.WebAPI.Services;

using System.Collections.Concurrent;

/// <summary>
/// Status of a game lobby.
/// </summary>
public enum LobbyStatus
{
    /// <summary>
    /// The lobby is waiting for players to join.
    /// </summary>
    Waiting,

    /// <summary>
    /// The match countdown has begun.
    /// </summary>
    Starting,

    /// <summary>
    /// The match is currently in progress.
    /// </summary>
    InProgress
}

/// <summary>
/// Represents a player participating in a lobby.
/// </summary>
/// <param name="UserId">Player's unique user identifier.</param>
/// <param name="DisplayName">Player's public display name.</param>
/// <param name="IsReady">Whether the player is marked as ready.</param>
public record LobbyPlayer(Guid UserId, string DisplayName, bool IsReady = false);

/// <summary>
/// Represents metadata and state of an active game lobby.
/// </summary>
/// <param name="Id">Unique 8-character lobby identifier.</param>
/// <param name="Name">Lobby display title.</param>
/// <param name="HostId">User identifier of the current lobby host.</param>
/// <param name="HostName">Display name of the lobby host.</param>
/// <param name="MaxPlayers">Maximum player capacity.</param>
/// <param name="GameMode">Selected game mode.</param>
/// <param name="MapId">Selected arena map identifier.</param>
/// <param name="IsPrivate">Whether password is required to join.</param>
/// <param name="Password">Optional join password.</param>
/// <param name="Status">Current lobby status.</param>
/// <param name="Players">List of players currently in the lobby.</param>
/// <param name="CreatedAt">Timestamp when the lobby was created.</param>
public record LobbyInfo(
    string Id,
    string Name,
    Guid HostId,
    string HostName,
    int MaxPlayers,
    string GameMode,
    string MapId,
    bool IsPrivate,
    string? Password,
    LobbyStatus Status,
    List<LobbyPlayer> Players,
    DateTime CreatedAt);

/// <summary>
/// In-memory management service for ephemeral game lobbies.
/// </summary>
public class LobbyService
{
    private readonly ConcurrentDictionary<string, LobbyInfo> _lobbies = new();
    private readonly object _syncRoot = new();

    /// <summary>
    /// Creates a new game lobby with the specified host and settings.
    /// </summary>
    public LobbyInfo CreateLobby(
        string name,
        Guid hostId,
        string hostName,
        int maxPlayers,
        string gameMode,
        string mapId,
        bool isPrivate,
        string? password)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var hostPlayer = new LobbyPlayer(hostId, hostName, true);
        var lobby = new LobbyInfo(
            Id: id,
            Name: name,
            HostId: hostId,
            HostName: hostName,
            MaxPlayers: Math.Max(2, maxPlayers),
            GameMode: string.IsNullOrWhiteSpace(gameMode) ? "Deathmatch" : gameMode,
            MapId: string.IsNullOrWhiteSpace(mapId) ? "arena_01" : mapId,
            IsPrivate: isPrivate,
            Password: isPrivate ? password : null,
            Status: LobbyStatus.Waiting,
            Players: [hostPlayer],
            CreatedAt: DateTime.UtcNow);

        _lobbies[id] = lobby;
        return lobby;
    }

    /// <summary>
    /// Retrieves a paginated list of public, joinable lobbies.
    /// </summary>
    public List<LobbyInfo> GetPublicLobbies(int page, int pageSize)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        return _lobbies.Values
            .Where(l => !l.IsPrivate && l.Status == LobbyStatus.Waiting)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();
    }

    /// <summary>
    /// Gets lobby details by its unique identifier.
    /// </summary>
    public LobbyInfo? GetLobby(string id)
    {
        _lobbies.TryGetValue(id, out var lobby);
        return lobby;
    }

    /// <summary>
    /// Adds a player to the specified lobby if capacity and password checks pass.
    /// </summary>
    public bool JoinLobby(string lobbyId, Guid userId, string displayName, string? password)
    {
        lock (_syncRoot)
        {
            if (!_lobbies.TryGetValue(lobbyId, out var lobby))
            {
                return false;
            }

            if (lobby.Status != LobbyStatus.Waiting)
            {
                return false;
            }

            if (lobby.Players.Count >= lobby.MaxPlayers)
            {
                return false;
            }

            if (lobby.Players.Any(p => p.UserId == userId))
            {
                return true;
            }

            if (lobby.IsPrivate && !string.Equals(lobby.Password, password, StringComparison.Ordinal))
            {
                return false;
            }

            var updatedPlayers = new List<LobbyPlayer>(lobby.Players)
            {
                new(userId, displayName, false)
            };

            _lobbies[lobbyId] = lobby with { Players = updatedPlayers };
            return true;
        }
    }

    /// <summary>
    /// Removes a player from the lobby, transferring host status or removing empty lobbies.
    /// </summary>
    public bool LeaveLobby(string lobbyId, Guid userId)
    {
        lock (_syncRoot)
        {
            if (!_lobbies.TryGetValue(lobbyId, out var lobby))
            {
                return false;
            }

            var player = lobby.Players.FirstOrDefault(p => p.UserId == userId);
            if (player == null)
            {
                return false;
            }

            var updatedPlayers = lobby.Players.Where(p => p.UserId != userId).ToList();

            if (updatedPlayers.Count == 0)
            {
                _lobbies.TryRemove(lobbyId, out _);
                return true;
            }

            var newHostId = lobby.HostId;
            var newHostName = lobby.HostName;

            if (lobby.HostId == userId)
            {
                var nextHost = updatedPlayers[0];
                newHostId = nextHost.UserId;
                newHostName = nextHost.DisplayName;
            }

            _lobbies[lobbyId] = lobby with
            {
                HostId = newHostId,
                HostName = newHostName,
                Players = updatedPlayers
            };

            return true;
        }
    }

    /// <summary>
    /// Starts the match if the requester is the lobby host.
    /// </summary>
    public bool StartMatch(string lobbyId, Guid requesterId)
    {
        lock (_syncRoot)
        {
            if (!_lobbies.TryGetValue(lobbyId, out var lobby))
            {
                return false;
            }

            if (lobby.HostId != requesterId)
            {
                return false;
            }

            if (lobby.Status != LobbyStatus.Waiting)
            {
                return false;
            }

            _lobbies[lobbyId] = lobby with { Status = LobbyStatus.Starting };
            return true;
        }
    }

    /// <summary>
    /// Updates lobby settings if the requester is the lobby host.
    /// </summary>
    public bool UpdateLobby(
        string lobbyId,
        Guid requesterId,
        string? name,
        int? maxPlayers,
        string? gameMode,
        string? mapId)
    {
        lock (_syncRoot)
        {
            if (!_lobbies.TryGetValue(lobbyId, out var lobby))
            {
                return false;
            }

            if (lobby.HostId != requesterId)
            {
                return false;
            }

            var updatedName = !string.IsNullOrWhiteSpace(name) ? name : lobby.Name;
            var updatedMaxPlayers = maxPlayers.HasValue ? Math.Max(lobby.Players.Count, maxPlayers.Value) : lobby.MaxPlayers;
            var updatedGameMode = !string.IsNullOrWhiteSpace(gameMode) ? gameMode : lobby.GameMode;
            var updatedMapId = !string.IsNullOrWhiteSpace(mapId) ? mapId : lobby.MapId;

            _lobbies[lobbyId] = lobby with
            {
                Name = updatedName,
                MaxPlayers = updatedMaxPlayers,
                GameMode = updatedGameMode,
                MapId = updatedMapId
            };

            return true;
        }
    }
}
