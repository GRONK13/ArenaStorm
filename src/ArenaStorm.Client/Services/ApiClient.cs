using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ArenaStorm.Client.Services;

/// <summary>
/// HTTP client wrapper communicating with the ArenaStorm Web API.
/// </summary>
public class ApiClient
{
    private static readonly HttpClient Http = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Gets or sets the Web API endpoint base address.</summary>
    public string BaseUrl { get; set; } = "http://localhost:5000/api/v1";

    /// <summary>Gets the server host IP or hostname.</summary>
    public string ServerHost { get; set; } = "127.0.0.1";

    /// <summary>
    /// Configures the server hostname or IP address.
    /// </summary>
    public void SetServerHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) return;
        ServerHost = host.Trim();
        BaseUrl = $"http://{ServerHost}:5000/api/v1";
    }

    /// <summary>Gets the current authenticated JWT token.</summary>
    public string? Token { get; private set; }

    /// <summary>Gets the current authenticated user's ID.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>Gets the current user's display name.</summary>
    public string DisplayName { get; set; } = "GuestPlayer";

    /// <summary>Gets whether the client is authenticated.</summary>
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    public async Task<(bool success, string message)> RegisterAsync(string username, string password, string displayName)
    {
        try
        {
            var response = await Http.PostAsJsonAsync($"{BaseUrl}/auth/register", new
            {
                username,
                password,
                displayName
            });

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
                if (content != null)
                {
                    SetAuth(content.Token, content.Player.Id, content.Player.DisplayName);
                    return (true, "Registered and logged in successfully!");
                }
            }

            var err = await response.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(err) ? "Registration failed" : err);
        }
        catch (Exception ex)
        {
            return (false, $"Network error: {ex.Message}");
        }
    }

    /// <summary>
    /// Logs in using username and password.
    /// </summary>
    public async Task<(bool success, string message)> LoginAsync(string username, string password)
    {
        try
        {
            var response = await Http.PostAsJsonAsync($"{BaseUrl}/auth/login", new
            {
                username,
                password
            });

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
                if (content != null)
                {
                    SetAuth(content.Token, content.Player.Id, content.Player.DisplayName);
                    return (true, "Logged in successfully!");
                }
            }

            return (false, "Invalid username or password");
        }
        catch (Exception ex)
        {
            return (false, $"Network error: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves active public lobbies.
    /// </summary>
    public async Task<List<LobbyItemDto>> GetLobbiesAsync()
    {
        try
        {
            SetAuthorizationHeader();
            var response = await Http.GetAsync($"{BaseUrl}/lobbies?page=1&pageSize=20");
            if (response.IsSuccessStatusCode)
            {
                var page = await response.Content.ReadFromJsonAsync<LobbiesPageResponse>(JsonOptions);
                return page?.Items ?? [];
            }
        }
        catch
        {
            // Silently return empty on network fault
        }

        return [];
    }

    /// <summary>
    /// Creates a new lobby room.
    /// </summary>
    public async Task<string?> CreateLobbyAsync(string name, int maxPlayers, string gameMode, string mapId)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await Http.PostAsJsonAsync($"{BaseUrl}/lobbies", new
            {
                name,
                maxPlayers,
                gameMode,
                mapId,
                isPrivate = false
            });

            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<LobbyDetailsDto>(JsonOptions);
                return created?.Id;
            }
        }
        catch
        {
            // Fallthrough
        }

        return null;
    }

    /// <summary>
    /// Joins an existing lobby room.
    /// </summary>
    public async Task<bool> JoinLobbyAsync(string lobbyId)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await Http.PostAsJsonAsync($"{BaseUrl}/lobbies/{lobbyId}/join", new { });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Starts match for the given lobby room.
    /// </summary>
    public async Task<bool> StartLobbyAsync(string lobbyId)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await Http.PostAsync($"{BaseUrl}/lobbies/{lobbyId}/start", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Retrieves top ranked players for a category.
    /// </summary>
    public async Task<List<LeaderboardItemDto>> GetLeaderboardAsync(string category = "kills")
    {
        try
        {
            SetAuthorizationHeader();
            var response = await Http.GetAsync($"{BaseUrl}/leaderboards/{category}?page=1&pageSize=20");
            if (response.IsSuccessStatusCode)
            {
                var lb = await response.Content.ReadFromJsonAsync<LeaderboardResponse>(JsonOptions);
                return lb?.Entries ?? [];
            }
        }
        catch
        {
            // Fallthrough
        }

        return [];
    }

    private void SetAuth(string token, Guid userId, string displayName)
    {
        Token = token;
        UserId = userId;
        DisplayName = displayName;
        SetAuthorizationHeader();
    }

    private void SetAuthorizationHeader()
    {
        if (!string.IsNullOrEmpty(Token))
        {
            Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        }
    }

    private sealed record AuthResponse(string Token, string ExpiresAt, PlayerDto Player);
    private sealed record PlayerDto(Guid Id, string Username, string DisplayName);
    private sealed record LobbiesPageResponse(List<LobbyItemDto> Items, int Total);
    private sealed record LeaderboardResponse(string Type, List<LeaderboardItemDto> Entries, int Total);
}

/// <summary>Lobby summary data transfer object.</summary>
public record LobbyItemDto(string Id, string Name, string HostName, int CurrentPlayers, int MaxPlayers, string GameMode, string MapId, string Status);

/// <summary>Lobby details response data transfer object.</summary>
public record LobbyDetailsDto(string Id, string Name, string HostName, int CurrentPlayers, int MaxPlayers, string GameMode, string MapId, string Status);

/// <summary>Leaderboard item entry.</summary>
public record LeaderboardItemDto(int Rank, Guid PlayerId, string DisplayName, int Value);
