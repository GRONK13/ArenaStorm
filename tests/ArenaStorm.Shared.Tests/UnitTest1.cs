using ArenaStorm.Shared.Constants;
using ArenaStorm.Shared.Models;
using ArenaStorm.Shared.Protocol;
using ArenaStorm.Shared.Utils;

namespace ArenaStorm.Shared.Tests;

public class SharedTests
{
    [Fact]
    public void PacketSerializer_ClientInputPacket_RoundtripsAccurately()
    {
        var input = new ClientInputPacket
        {
            SequenceNumber = 12345,
            InputFlags = InputFlags.Up | InputFlags.Fire,
            AimAngle = 1.57f,
            Timestamp = 987654
        };

        byte[] serialized = PacketSerializer.Serialize(MessageType.ClientInput, input);
        Assert.NotNull(serialized);
        Assert.True(serialized.Length > 1);

        ReadOnlyMemory<byte> payload = PacketSerializer.Deserialize(serialized, out MessageType msgType);
        Assert.Equal(MessageType.ClientInput, msgType);

        var deserialized = PacketSerializer.DeserializePayload<ClientInputPacket>(payload);
        Assert.Equal(input.SequenceNumber, deserialized.SequenceNumber);
        Assert.Equal(input.InputFlags, deserialized.InputFlags);
        Assert.Equal(input.AimAngle, deserialized.AimAngle, 2);
        Assert.Equal(input.Timestamp, deserialized.Timestamp);
    }

    [Fact]
    public void PacketSerializer_ServerWelcomePacket_RoundtripsAccurately()
    {
        var welcome = new ServerWelcomePacket
        {
            PlayerId = 3,
            RoomId = "room_alpha",
            MapId = "arena_1",
            TickRate = 30
        };

        byte[] serialized = PacketSerializer.Serialize(MessageType.ServerWelcome, welcome);
        ReadOnlyMemory<byte> payload = PacketSerializer.Deserialize(serialized, out MessageType msgType);
        Assert.Equal(MessageType.ServerWelcome, msgType);

        var deserialized = PacketSerializer.DeserializePayload<ServerWelcomePacket>(payload);
        Assert.Equal((byte)3, deserialized.PlayerId);
        Assert.Equal("room_alpha", deserialized.RoomId);
        Assert.Equal("arena_1", deserialized.MapId);
        Assert.Equal((byte)30, deserialized.TickRate);
    }

    [Fact]
    public void PacketSerializer_ServerSnapshotPacket_RoundtripsAccurately()
    {
        var snapshot = new ServerSnapshotPacket
        {
            Tick = 100,
            LastProcessedInput = 42,
            Players =
            [
                new PlayerSnapshot
                {
                    PlayerId = 1,
                    X = 150.5f,
                    Y = 220.3f,
                    AimAngle = 0.5f,
                    Health = 85,
                    ClassId = 0,
                    AnimationState = 1,
                    Shield = 25
                }
            ],
            Projectiles =
            [
                new ProjectileSnapshot
                {
                    Id = 1,
                    X = 160f,
                    Y = 225f,
                    Angle = 0.5f,
                    OwnerId = 1,
                    ProjectileType = 0
                }
            ],
            Pickups =
            [
                new PickupSnapshot
                {
                    Id = 1,
                    X = 300f,
                    Y = 400f,
                    PickupType = 0,
                    IsActive = true
                }
            ],
            Enemies =
            [
                new EnemySnapshot
                {
                    Id = 1,
                    EnemyType = 0,
                    X = 200f,
                    Y = 200f,
                    Health = 50
                }
            ]
        };

        byte[] serialized = PacketSerializer.Serialize(MessageType.ServerSnapshot, snapshot);
        ReadOnlyMemory<byte> payload = PacketSerializer.Deserialize(serialized, out MessageType msgType);
        Assert.Equal(MessageType.ServerSnapshot, msgType);

        var deserialized = PacketSerializer.DeserializePayload<ServerSnapshotPacket>(payload);
        Assert.Equal(snapshot.Tick, deserialized.Tick);
        Assert.Equal(snapshot.LastProcessedInput, deserialized.LastProcessedInput);
        Assert.Single(deserialized.Players);
        Assert.Equal(85, deserialized.Players[0].Health);
        Assert.Equal(25, deserialized.Players[0].Shield);
        Assert.Single(deserialized.Projectiles);
        Assert.Single(deserialized.Pickups);
        Assert.NotNull(deserialized.Enemies);
        Assert.Single(deserialized.Enemies!);
    }

    [Fact]
    public void CollisionHelper_DetectsCollisionsProperly()
    {
        // Circle vs Circle
        Assert.True(CollisionHelper.CheckCircleCollision(0, 0, 10, 15, 0, 10));
        Assert.False(CollisionHelper.CheckCircleCollision(0, 0, 5, 20, 0, 5));

        // Point in Rect
        Assert.True(CollisionHelper.CheckPointInRect(15, 15, 10, 10, 20, 20));
        Assert.False(CollisionHelper.CheckPointInRect(5, 5, 10, 10, 20, 20));

        // Circle vs Rect
        Assert.True(CollisionHelper.CheckCircleRect(8, 15, 5, 10, 10, 20, 20));
        Assert.False(CollisionHelper.CheckCircleRect(0, 0, 5, 20, 20, 20, 20));

        // Distance calculations
        Assert.Equal(5f, CollisionHelper.Distance(0, 0, 3, 4));
        Assert.Equal(25f, CollisionHelper.DistanceSquared(0, 0, 3, 4));
    }

    [Fact]
    public void CharacterClassRegistry_ReturnsExpectedArchetypes()
    {
        var ranger = CharacterClassRegistry.Get(CharacterClass.Ranger);
        var tank = CharacterClassRegistry.Get(CharacterClass.Tank);
        var mage = CharacterClassRegistry.Get(CharacterClass.Mage);
        var warrior = CharacterClassRegistry.Get(CharacterClass.Warrior);

        Assert.Equal(100, ranger.MaxHealth);
        Assert.Equal(180, tank.MaxHealth);
        Assert.Equal(80, mage.MaxHealth);
        Assert.Equal(130, warrior.MaxHealth);

        Assert.True(ranger.MoveSpeed > tank.MoveSpeed);
        Assert.True(warrior.AttackDamage > ranger.AttackDamage);
    }

    [Fact]
    public void PlayerState_DamageAbsorbedByShield_ThenHealth()
    {
        var player = new PlayerState
        {
            PlayerId = 1,
            MaxHealth = 100,
            Health = 100,
            Shield = 30,
            IsAlive = true
        };

        // Attack dealing 20 damage: should be completely absorbed by 30 shield
        player.ApplyDamage(20);
        Assert.Equal(10, player.Shield);
        Assert.Equal(100, player.Health);
        Assert.True(player.IsAlive);

        // Attack dealing 25 damage: absorbs remaining 10 shield, 15 penetrates to health
        player.ApplyDamage(25);
        Assert.Equal(0, player.Shield);
        Assert.Equal(85, player.Health);
        Assert.True(player.IsAlive);

        // Fatal attack dealing 100 damage: health drops to 0, player marked dead
        player.ApplyDamage(100);
        Assert.Equal(0, player.Health);
        Assert.False(player.IsAlive);

        // Respawn restores health and resets shield
        player.Respawn(50, 50);
        Assert.Equal(100, player.Health);
        Assert.Equal(0, player.Shield);
        Assert.True(player.IsAlive);
    }

    [Fact]
    public void ProjectileState_UpdatesAndExpires()
    {
        var p = new ProjectileState
        {
            Id = 1,
            X = 0f,
            Y = 0f,
            Angle = 0f, // facing right
            Speed = 100f,
            OwnerId = 0,
            Damage = 20,
            Lifetime = 0f,
            MaxLifetime = 1.0f
        };

        p.Update(0.5f);
        Assert.Equal(50f, p.X, 1);
        Assert.Equal(0f, p.Y, 1);
        Assert.False(p.IsExpired);

        p.Update(0.6f);
        Assert.True(p.IsExpired);
    }
}
