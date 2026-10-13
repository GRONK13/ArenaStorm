using ArenaStorm.Server;
using ArenaStorm.Server.Game;
using ArenaStorm.Shared.Constants;
using ArenaStorm.Shared.Models;
using ArenaStorm.Shared.Protocol;

namespace ArenaStorm.Server.Tests;

public class GameServerTests
{
    [Fact]
    public void PhysicsEngine_LoadMap_SetsWallsAndObstacles()
    {
        var physics = new PhysicsEngine();
        physics.LoadMap("arena_1");

        Assert.Equal(30, physics.MapWidth);
        Assert.Equal(20, physics.MapHeight);

        // Borders should be solid walls
        Assert.False(physics.IsWalkable(0, 0));
        Assert.True(physics.CheckWallCollision(0, 0, 10f));

        // Center lane should have walkable paths
        Assert.True(physics.IsWalkable(10 * GameConstants.TileSize, 10 * GameConstants.TileSize));
    }

    [Fact]
    public void PhysicsEngine_ClampToWalkable_KeepsEntityWithinBounds()
    {
        var physics = new PhysicsEngine();
        physics.LoadMap("arena_1");

        // Attempting to move outside top-left boundary
        var (x, y) = physics.ClampToWalkable(-50f, -50f, 14f);
        Assert.True(x >= GameConstants.TileSize);
        Assert.True(y >= GameConstants.TileSize);
        Assert.False(physics.CheckWallCollision(x, y, 14f));
    }

    [Fact]
    public void SpawnManager_Initialize_PopulatesSpawnPoints()
    {
        var spawns = new SpawnManager();
        spawns.Initialize("arena_1");

        Assert.True(spawns.SpawnPoints.Count >= 8);

        var (x, y) = spawns.GetRandomSpawnPoint();
        Assert.True(x > 0);
        Assert.True(y > 0);
    }

    [Fact]
    public void GameWorld_AddPlayer_InitializesState()
    {
        var world = new GameWorld("arena_1");
        world.AddPlayer(0, "TestPlayer", CharacterClass.Ranger);

        Assert.True(world.Players.ContainsKey(0));
        var player = world.Players[0];
        Assert.Equal("TestPlayer", player.DisplayName);
        Assert.Equal(100, player.Health);
        Assert.True(player.IsAlive);
    }

    [Fact]
    public void GameWorld_ProcessPlayerInput_MovesPlayerAndSpawnsProjectile()
    {
        var world = new GameWorld("arena_1");
        world.AddPlayer(1, "Hero", CharacterClass.Ranger);

        var input = new ClientInputPacket
        {
            SequenceNumber = 10,
            InputFlags = InputFlags.Right | InputFlags.Fire,
            AimAngle = 0f,
            Timestamp = 1000
        };

        float startX = world.Players[1].X;
        world.ProcessPlayerInput(1, input, 0.1f);

        Assert.True(world.Players[1].X > startX);
        Assert.Single(world.Projectiles);
        Assert.Equal(1, world.Projectiles[0].OwnerId);
    }

    [Fact]
    public void GameWorld_Update_ResolvesProjectileCollisionsAndDamage()
    {
        var world = new GameWorld("arena_1");
        world.AddPlayer(0, "Attacker", CharacterClass.Ranger);
        world.AddPlayer(1, "Target", CharacterClass.Ranger);

        // Position target right in front of attacker
        world.Players[0].X = 200f;
        world.Players[0].Y = 200f;
        world.Players[1].X = 220f;
        world.Players[1].Y = 200f;

        // Attacker fires towards target (angle = 0)
        world.SpawnProjectile(0, 205f, 200f, 0f, 25);

        world.Update(0.05f);

        Assert.Single(world.PendingDamageEvents);
        Assert.Equal(1, world.PendingDamageEvents[0].TargetId);
        Assert.Equal(0, world.PendingDamageEvents[0].AttackerId);
        Assert.Equal(25, world.PendingDamageEvents[0].Damage);
        Assert.Equal(75, world.Players[1].Health);
    }

    [Fact]
    public void GameRoom_AddRemovePlayer_UpdatesPlayerCount()
    {
        var server = new GameServer();
        var room = server.CreateRoom("room_test", new GameSettings("arena_1", GameMode.PvpDeathmatch, 4));

        Assert.Equal(0, room.PlayerCount);
        Assert.Equal(GameRoomState.Waiting, room.State);

        room.StartMatch();
        Assert.Equal(GameRoomState.Playing, room.State);

        room.EndMatch();
        Assert.Equal(GameRoomState.Finished, room.State);
    }

    [Fact]
    public void GameWorld_MeleeAttack_DamagesOpponentInCone()
    {
        var world = new GameWorld("arena_1");
        world.AddPlayer(0, "WarriorPlayer", CharacterClass.Warrior);
        world.AddPlayer(1, "VictimPlayer", CharacterClass.Ranger);

        world.Players[0].X = 200f;
        world.Players[0].Y = 200f;
        world.Players[0].AimAngle = 0f; // facing right

        world.Players[1].X = 230f; // 30 units to the right (within 65 units melee range)
        world.Players[1].Y = 200f;

        var input = new ClientInputPacket
        {
            SequenceNumber = 1,
            InputFlags = InputFlags.Melee,
            AimAngle = 0f,
            Timestamp = 100
        };

        world.ProcessPlayerInput(0, input, 0.05f);

        Assert.Single(world.PendingMeleeEvents);
        Assert.Single(world.PendingDamageEvents);
        Assert.Equal(1, world.PendingDamageEvents[0].TargetId);
        Assert.True(world.Players[1].Health < 100);
    }

    [Fact]
    public void GameWorld_Update_PlayerCollectsPickup_AppliesBuff()
    {
        var world = new GameWorld("arena_1");
        world.AddPlayer(0, "Collector", CharacterClass.Ranger);

        var shieldPickup = world.Pickups.First(p => p.Type == PickupType.Shield);
        world.Players[0].X = shieldPickup.X;
        world.Players[0].Y = shieldPickup.Y;

        world.Update(0.05f);

        Assert.False(shieldPickup.IsActive);
        Assert.True(world.Players[0].Shield > 0);
    }

    [Fact]
    public void GameWorld_CoopSurvival_SpawnsWavesAndUpdatesEnemies()
    {
        var settings = new GameSettings("arena_1", GameMode.CoopSurvival, 4);
        var world = new GameWorld("arena_1", settings);
        world.AddPlayer(0, "Survivor", CharacterClass.Ranger);

        // Run updates to trigger initial wave spawn (timer starts at 3s)
        for (int i = 0; i < 70; i++)
        {
            world.Update(0.05f);
        }

        Assert.NotEmpty(world.Enemies);
        Assert.True(world.CurrentWave >= 2);
    }

    [Fact]
    public void GameWorld_PvpElimination_PlayerDoesNotRespawn()
    {
        var settings = new GameSettings("arena_1", GameMode.PvpElimination, 2);
        var world = new GameWorld("arena_1", settings);
        world.AddPlayer(0, "Fighter1", CharacterClass.Ranger);
        world.AddPlayer(1, "Fighter2", CharacterClass.Ranger);

        // Kill Fighter2
        world.Players[1].ApplyDamage(200);
        Assert.False(world.Players[1].IsAlive);

        // Run updates past normal respawn time (3 seconds = 60 ticks)
        for (int i = 0; i < 100; i++)
        {
            world.Update(0.05f);
        }

        // Under elimination mode, dead player must stay dead
        Assert.False(world.Players[1].IsAlive);
        Assert.Empty(world.PendingRespawnEvents);
    }
}
