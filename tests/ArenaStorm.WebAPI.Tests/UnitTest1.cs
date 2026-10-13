using ArenaStorm.WebAPI.Data;
using ArenaStorm.WebAPI.Entities;
using ArenaStorm.WebAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ArenaStorm.WebAPI.Tests;

public class WebApiTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IConfiguration CreateConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "SuperSecretArenaStormKeyForTestingOnly2026!",
            ["Jwt:Issuer"] = "ArenaStormTestIssuer",
            ["Jwt:Audience"] = "ArenaStormTestAudience",
            ["Jwt:ExpiryHours"] = "24"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public async Task AuthService_RegisterAndLogin_SucceedsWithValidCredentials()
    {
        using var db = CreateInMemoryDbContext();
        var config = CreateConfiguration();
        var jwt = new JwtService(config);
        var auth = new AuthService(db, jwt);

        // 1. Register new account
        var regResult = await auth.RegisterAsync("ProGamer", "Password123!", "Pro Gamer 99");
        Assert.NotNull(regResult);
        Assert.Equal("ProGamer", regResult.Value.user.Username);
        Assert.Equal("Pro Gamer 99", regResult.Value.user.DisplayName);
        Assert.False(string.IsNullOrEmpty(regResult.Value.token));

        // Verify database entry
        var dbUser = await db.Users.Include(u => u.Stats).FirstOrDefaultAsync(u => u.Username == "ProGamer");
        Assert.NotNull(dbUser);
        Assert.NotNull(dbUser.Stats);
        Assert.Equal(0, dbUser.Stats.TotalKills);

        // 2. Reject duplicate registration
        var duplicateResult = await auth.RegisterAsync("ProGamer", "DifferentPassword!", "Another Name");
        Assert.Null(duplicateResult);

        // 3. Login with correct password
        var loginResult = await auth.LoginAsync("ProGamer", "Password123!");
        Assert.NotNull(loginResult);
        Assert.Equal(dbUser.Id, loginResult.Value.user.Id);
        Assert.False(string.IsNullOrEmpty(loginResult.Value.token));

        // 4. Reject invalid password
        var failedLogin = await auth.LoginAsync("ProGamer", "WrongPassword!");
        Assert.Null(failedLogin);
    }

    [Fact]
    public void JwtService_GenerateAndValidateToken_VerifiesClaims()
    {
        var config = CreateConfiguration();
        var jwt = new JwtService(config);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "SniperElite",
            DisplayName = "Elite Sniper",
            CharacterClass = "ranger"
        };

        string token = jwt.GenerateToken(user);
        Assert.False(string.IsNullOrEmpty(token));

        var principal = jwt.ValidateToken(token);
        Assert.NotNull(principal);
        Assert.Equal(user.Username, principal.Identity?.Name);
    }

    [Fact]
    public void LobbyService_CreateJoinLeaveAndStart_ManagesRoomLifecycle()
    {
        var lobbyService = new LobbyService();
        var hostId = Guid.NewGuid();
        var guestId = Guid.NewGuid();

        // 1. Create lobby
        var lobby = lobbyService.CreateLobby(
            name: "Champions Arena",
            hostId: hostId,
            hostName: "HostUser",
            maxPlayers: 4,
            gameMode: "pvp_deathmatch",
            mapId: "arena_1",
            isPrivate: false,
            password: null);

        Assert.NotNull(lobby);
        Assert.Equal("Champions Arena", lobby.Name);
        Assert.Single(lobby.Players);
        Assert.Equal(LobbyStatus.Waiting, lobby.Status);

        // 2. Guest joins lobby
        bool joined = lobbyService.JoinLobby(lobby.Id, guestId, "GuestUser", password: null);
        Assert.True(joined);

        var updated = lobbyService.GetLobby(lobby.Id);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.Players.Count);

        // 3. Host starts match
        bool started = lobbyService.StartMatch(lobby.Id, hostId);
        Assert.True(started);
        Assert.Equal(LobbyStatus.Starting, lobbyService.GetLobby(lobby.Id)?.Status);

        // 4. Guest leaves lobby
        bool left = lobbyService.LeaveLobby(lobby.Id, guestId);
        Assert.True(left);
        Assert.Equal(1, lobbyService.GetLobby(lobby.Id)?.Players.Count);
    }

    [Fact]
    public async Task LeaderboardService_UpdatesStatsAndRanks()
    {
        using var db = CreateInMemoryDbContext();
        var leaderboardService = new LeaderboardService(db);

        var userId1 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Username = "Alpha", DisplayName = "Alpha Dog" };

        var userId2 = Guid.NewGuid();
        var user2 = new User { Id = userId2, Username = "Beta", DisplayName = "Beta Wolf" };

        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        // Populate initial stats via LeaderboardService
        await leaderboardService.UpdateStatsAsync(userId2, kills: 25, deaths: 10, won: true);
        await leaderboardService.UpdateStatsAsync(userId1, kills: 10, deaths: 5, won: true);

        // Additional match for Alpha: +20 kills
        await leaderboardService.UpdateStatsAsync(userId1, kills: 20, deaths: 2, won: true);

        // Fetch kills leaderboard
        var killsLb = await leaderboardService.GetLeaderboardAsync("kills", 1, 10);
        Assert.Equal(2, killsLb.Count);

        // Alpha now has 10 + 20 = 30 kills, which should rank #1 over Beta's 25 kills
        Assert.Equal("Alpha Dog", killsLb[0].DisplayName);
        Assert.Equal(30, killsLb[0].Value);
        Assert.Equal(1, killsLb[0].Rank);

        Assert.Equal("Beta Wolf", killsLb[1].DisplayName);
        Assert.Equal(25, killsLb[1].Value);
        Assert.Equal(2, killsLb[1].Rank);
    }
}
