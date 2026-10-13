using ArenaStorm.Shared.Constants;
using ArenaStorm.Shared.Models;
using ArenaStorm.Shared.Protocol;
using ArenaStorm.Shared.Utils;

namespace ArenaStorm.Server.Game;

/// <summary>
/// Authoritative gameplay state simulation container for a single match arena.
/// </summary>
public class GameWorld
{
    private const float PlayerRadius = 14f;
    private const float ProjectileRadius = 4f;
    private const float EnemyRadius = 12f;

    private readonly Dictionary<byte, uint> _lastProcessedInputs = new();
    private readonly Dictionary<byte, float> _attackCooldowns = new();
    private readonly Dictionary<byte, float> _meleeCooldowns = new();

    /// <summary>
    /// Gets the match settings and rules governing this world.
    /// </summary>
    public GameSettings Settings { get; }

    /// <summary>
    /// Gets the dictionary of active players indexed by player ID.
    /// </summary>
    public Dictionary<byte, PlayerState> Players { get; } = new();

    /// <summary>
    /// Gets the list of active in-flight projectiles.
    /// </summary>
    public List<ProjectileState> Projectiles { get; } = [];

    /// <summary>
    /// Gets the list of arena pickups available on the map.
    /// </summary>
    public List<PickupState> Pickups { get; } = [];

    /// <summary>
    /// Gets the list of AI-controlled enemies in PvE co-op survival mode.
    /// </summary>
    public List<EnemyState> Enemies { get; } = [];

    /// <summary>
    /// Gets the current collision and spatial physics engine.
    /// </summary>
    public PhysicsEngine Physics { get; } = new();

    /// <summary>
    /// Gets the spawn manager governing player spawn coordinates.
    /// </summary>
    public SpawnManager Spawns { get; } = new();

    /// <summary>
    /// Gets the monotonic authoritative server tick count.
    /// </summary>
    public uint CurrentTick { get; private set; }

    /// <summary>
    /// Gets or sets the monotonic projectile sequence generator.
    /// </summary>
    public ushort NextProjectileId { get; set; }

    /// <summary>
    /// Gets or sets the monotonic enemy sequence generator.
    /// </summary>
    public ushort NextEnemyId { get; set; }

    /// <summary>
    /// Gets the current PvE wave counter.
    /// </summary>
    public int CurrentWave { get; private set; } = 1;

    /// <summary>
    /// Gets the wave spawn cooldown timer in seconds.
    /// </summary>
    public float WaveSpawnTimer { get; private set; } = 3.0f;

    /// <summary>
    /// Gets the list of pending damage events awaiting broadcast to clients.
    /// </summary>
    public List<ServerDamageEventPacket> PendingDamageEvents { get; } = [];

    /// <summary>
    /// Gets the list of pending kill events awaiting broadcast to clients.
    /// </summary>
    public List<ServerKillEventPacket> PendingKillEvents { get; } = [];

    /// <summary>
    /// Gets the list of pending player respawn events awaiting broadcast to clients.
    /// </summary>
    public List<ServerRespawnPacket> PendingRespawnEvents { get; } = [];

    /// <summary>
    /// Gets the list of executed melee attack events awaiting broadcast to clients.
    /// </summary>
    public List<ServerMeleeAttackPacket> PendingMeleeEvents { get; } = [];

    /// <summary>
    /// Initializes a new instance of <see cref="GameWorld"/> for the specified map layout and game settings.
    /// </summary>
    /// <param name="mapId">Map identifier to load.</param>
    /// <param name="settings">Optional match configuration settings.</param>
    public GameWorld(string mapId = "arena_1", GameSettings? settings = null)
    {
        Settings = settings ?? new GameSettings(mapId, GameMode.PvpDeathmatch, GameConstants.MaxPlayersPerMatch);
        Physics.LoadMap(mapId);
        Spawns.Initialize(mapId);
        InitializePickups();
    }

    private void InitializePickups()
    {
        ushort id = 1;
        // 4 tactical pickups placed across open corridors in the 30x20 arena
        Pickups.Add(new PickupState
        {
            Id = id++,
            X = 15f * GameConstants.TileSize,
            Y = 6f * GameConstants.TileSize,
            Type = PickupType.Health,
            IsActive = true,
            RespawnInterval = 15f
        });

        Pickups.Add(new PickupState
        {
            Id = id++,
            X = 3f * GameConstants.TileSize,
            Y = 10f * GameConstants.TileSize,
            Type = PickupType.Speed,
            IsActive = true,
            RespawnInterval = 20f
        });

        Pickups.Add(new PickupState
        {
            Id = id++,
            X = 27f * GameConstants.TileSize,
            Y = 10f * GameConstants.TileSize,
            Type = PickupType.Damage,
            IsActive = true,
            RespawnInterval = 25f
        });

        Pickups.Add(new PickupState
        {
            Id = id++,
            X = 15f * GameConstants.TileSize,
            Y = 14f * GameConstants.TileSize,
            Type = PickupType.Shield,
            IsActive = true,
            RespawnInterval = 30f
        });
    }

    /// <summary>
    /// Registers a new player into the active simulation and positions them at a random spawn.
    /// </summary>
    /// <param name="id">Assigned player ID (0-11).</param>
    /// <param name="displayName">Human-readable player display name.</param>
    /// <param name="characterClass">Selected character class archetype.</param>
    public void AddPlayer(byte id, string displayName, CharacterClass characterClass)
    {
        var classData = CharacterClassRegistry.Get(characterClass);
        var (spawnX, spawnY) = Spawns.GetRandomSpawnPoint();

        var player = new PlayerState
        {
            PlayerId = id,
            DisplayName = displayName,
            CharacterClass = characterClass,
            Health = classData.MaxHealth,
            MaxHealth = classData.MaxHealth,
            X = spawnX,
            Y = spawnY,
            IsAlive = true,
            RespawnTimer = 0f,
            Kills = 0,
            Deaths = 0,
            AnimationState = 0
        };

        Players[id] = player;
        _lastProcessedInputs[id] = 0;
        _attackCooldowns[id] = 0f;
        _meleeCooldowns[id] = 0f;
    }

    /// <summary>
    /// Removes a player and cleans up associated simulation state and projectiles.
    /// </summary>
    /// <param name="id">Player ID to remove.</param>
    public void RemovePlayer(byte id)
    {
        Players.Remove(id);
        _lastProcessedInputs.Remove(id);
        _attackCooldowns.Remove(id);
        _meleeCooldowns.Remove(id);
        Projectiles.RemoveAll(p => p.OwnerId == id);
    }

    /// <summary>
    /// Applies client movement inputs and processes attack actions.
    /// </summary>
    /// <param name="playerId">Player ID originating the input.</param>
    /// <param name="input">Received client input packet.</param>
    /// <param name="deltaTime">Delta simulation time in seconds.</param>
    public void ProcessPlayerInput(byte playerId, ClientInputPacket input, float deltaTime)
    {
        _lastProcessedInputs[playerId] = input.SequenceNumber;

        if (!Players.TryGetValue(playerId, out var player) || !player.IsAlive)
        {
            return;
        }

        player.AimAngle = input.AimAngle;

        // 1. Movement resolution
        float moveX = 0f;
        float moveY = 0f;

        if ((input.InputFlags & InputFlags.Up) != 0) moveY -= 1f;
        if ((input.InputFlags & InputFlags.Down) != 0) moveY += 1f;
        if ((input.InputFlags & InputFlags.Left) != 0) moveX -= 1f;
        if ((input.InputFlags & InputFlags.Right) != 0) moveX += 1f;

        if (moveX != 0f || moveY != 0f)
        {
            float length = MathF.Sqrt((moveX * moveX) + (moveY * moveY));
            moveX /= length;
            moveY /= length;

            var classData = CharacterClassRegistry.Get(player.CharacterClass);
            float speedMultiplier = player.SpeedBoostTimer > 0f ? 1.5f : 1.0f;
            float speed = classData.MoveSpeed * GameConstants.TileSize * speedMultiplier;
            float newX = player.X + (moveX * speed * deltaTime);
            float newY = player.Y + (moveY * speed * deltaTime);

            var clamped = Physics.ClampToWalkable(newX, newY, PlayerRadius);
            player.X = clamped.x;
            player.Y = clamped.y;
            player.AnimationState = 1;
        }
        else
        {
            player.AnimationState = 0;
        }

        // 2. Ranged attack
        if ((input.InputFlags & InputFlags.Fire) != 0)
        {
            float currentCooldown = _attackCooldowns.GetValueOrDefault(playerId, 0f);
            if (currentCooldown <= 0f)
            {
                var classData = CharacterClassRegistry.Get(player.CharacterClass);
                _attackCooldowns[playerId] = classData.AttackCooldown;

                float damageMultiplier = player.DamageBoostTimer > 0f ? 1.3f : 1.0f;
                short damage = (short)Math.Round(classData.AttackDamage * damageMultiplier);
                SpawnProjectile(playerId, player.X, player.Y, player.AimAngle, damage);
            }
        }

        // 3. Melee attack
        if ((input.InputFlags & InputFlags.Melee) != 0)
        {
            float currentMeleeCooldown = _meleeCooldowns.GetValueOrDefault(playerId, 0f);
            if (currentMeleeCooldown <= 0f)
            {
                _meleeCooldowns[playerId] = 0.45f;
                ExecuteMeleeAttack(player);
            }
        }
    }

    private void ExecuteMeleeAttack(PlayerState player)
    {
        const float meleeRange = 65f;
        const float meleeHalfConeAngle = MathF.PI / 3f; // 60 degree cone

        PendingMeleeEvents.Add(new ServerMeleeAttackPacket
        {
            AttackerId = player.PlayerId,
            Angle = player.AimAngle,
            Range = meleeRange
        });

        float damageMultiplier = player.DamageBoostTimer > 0f ? 1.3f : 1.0f;
        short baseDamage = player.CharacterClass == CharacterClass.Warrior ? (short)40 : (short)25;
        short meleeDamage = (short)Math.Round(baseDamage * damageMultiplier);

        // Strike other players
        foreach (var (targetId, target) in Players)
        {
            if (targetId == player.PlayerId || !target.IsAlive) continue;

            float dx = target.X - player.X;
            float dy = target.Y - player.Y;
            float dist = MathF.Sqrt((dx * dx) + (dy * dy));

            if (dist <= meleeRange + PlayerRadius)
            {
                float angleToTarget = MathF.Atan2(dy, dx);
                float angleDiff = MathF.Abs(MathF.Atan2(MathF.Sin(angleToTarget - player.AimAngle), MathF.Cos(angleToTarget - player.AimAngle)));

                if (angleDiff <= meleeHalfConeAngle)
                {
                    target.ApplyDamage(meleeDamage);
                    PendingDamageEvents.Add(new ServerDamageEventPacket
                    {
                        TargetId = targetId,
                        AttackerId = player.PlayerId,
                        Damage = meleeDamage,
                        HitX = target.X,
                        HitY = target.Y
                    });

                    if (!target.IsAlive)
                    {
                        target.Deaths++;
                        player.Kills++;
                        if (Settings.Mode != GameMode.PvpElimination)
                        {
                            target.RespawnTimer = GameConstants.DefaultRespawnTime;
                        }

                        PendingKillEvents.Add(new ServerKillEventPacket
                        {
                            VictimId = targetId,
                            KillerId = player.PlayerId
                        });
                    }
                }
            }
        }

        // Strike PvE enemies
        for (int i = Enemies.Count - 1; i >= 0; i--)
        {
            var enemy = Enemies[i];
            if (!enemy.IsAlive) continue;

            float dx = enemy.X - player.X;
            float dy = enemy.Y - player.Y;
            float dist = MathF.Sqrt((dx * dx) + (dy * dy));

            if (dist <= meleeRange + EnemyRadius)
            {
                float angleToEnemy = MathF.Atan2(dy, dx);
                float angleDiff = MathF.Abs(MathF.Atan2(MathF.Sin(angleToEnemy - player.AimAngle), MathF.Cos(angleToEnemy - player.AimAngle)));

                if (angleDiff <= meleeHalfConeAngle)
                {
                    enemy.ApplyDamage(meleeDamage);
                    PendingDamageEvents.Add(new ServerDamageEventPacket
                    {
                        TargetId = (byte)enemy.Id,
                        AttackerId = player.PlayerId,
                        Damage = meleeDamage,
                        HitX = enemy.X,
                        HitY = enemy.Y
                    });

                    if (!enemy.IsAlive)
                    {
                        player.Kills++;
                        Enemies.RemoveAt(i);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Advances world simulation by one tick: updates projectiles, pickups, PvE AI, resolves collisions, and handles respawns.
    /// </summary>
    /// <param name="deltaTime">Delta frame time in seconds.</param>
    public void Update(float deltaTime)
    {
        CurrentTick++;

        // 1. Update player cooldowns and active buff timers
        foreach (var player in Players.Values)
        {
            if (player.SpeedBoostTimer > 0f) player.SpeedBoostTimer -= deltaTime;
            if (player.DamageBoostTimer > 0f) player.DamageBoostTimer -= deltaTime;
        }

        foreach (var key in _attackCooldowns.Keys.ToList())
        {
            if (_attackCooldowns[key] > 0f) _attackCooldowns[key] -= deltaTime;
        }

        foreach (var key in _meleeCooldowns.Keys.ToList())
        {
            if (_meleeCooldowns[key] > 0f) _meleeCooldowns[key] -= deltaTime;
        }

        // 2. Update Pickups and check player collection
        UpdatePickups(deltaTime);

        // 3. Update Projectiles
        UpdateProjectiles(deltaTime);

        // 4. Update PvE enemies if in co-op mode
        if (Settings.Mode == GameMode.CoopSurvival)
        {
            UpdatePvE(deltaTime);
        }

        // 5. Process respawn timers (for non-elimination modes)
        if (Settings.Mode != GameMode.PvpElimination)
        {
            UpdateRespawns(deltaTime);
        }
    }

    private void UpdatePickups(float deltaTime)
    {
        foreach (var pickup in Pickups)
        {
            if (!pickup.IsActive)
            {
                pickup.RespawnTimer -= deltaTime;
                if (pickup.RespawnTimer <= 0f)
                {
                    pickup.IsActive = true;
                }
            }
            else
            {
                // Check if any alive player collects the pickup
                foreach (var player in Players.Values)
                {
                    if (!player.IsAlive) continue;

                    if (CollisionHelper.CheckCircleCollision(player.X, player.Y, PlayerRadius, pickup.X, pickup.Y, 14f))
                    {
                        ApplyPickupEffect(player, pickup.Type);
                        pickup.IsActive = false;
                        pickup.RespawnTimer = pickup.RespawnInterval;
                        break;
                    }
                }
            }
        }
    }

    private static void ApplyPickupEffect(PlayerState player, PickupType type)
    {
        switch (type)
        {
            case PickupType.Health:
                player.Health = (short)Math.Min((int)player.MaxHealth, player.Health + 50);
                break;
            case PickupType.Speed:
                player.SpeedBoostTimer = 8.0f;
                break;
            case PickupType.Damage:
                player.DamageBoostTimer = 10.0f;
                break;
            case PickupType.Shield:
                player.Shield = (short)Math.Min(100, player.Shield + 50);
                break;
        }
    }

    private void UpdateProjectiles(float deltaTime)
    {
        for (int i = Projectiles.Count - 1; i >= 0; i--)
        {
            var projectile = Projectiles[i];
            projectile.Update(deltaTime);

            if (projectile.IsExpired || Physics.CheckWallCollision(projectile.X, projectile.Y, ProjectileRadius))
            {
                Projectiles.RemoveAt(i);
                continue;
            }

            bool hit = false;

            // Check projectile hit against other players
            if (Physics.CheckProjectileCollision(projectile, Players, out byte hitPlayerId))
            {
                if (Players.TryGetValue(hitPlayerId, out var hitPlayer))
                {
                    hitPlayer.ApplyDamage(projectile.Damage);
                    PendingDamageEvents.Add(new ServerDamageEventPacket
                    {
                        TargetId = hitPlayerId,
                        AttackerId = projectile.OwnerId,
                        Damage = projectile.Damage,
                        HitX = projectile.X,
                        HitY = projectile.Y
                    });

                    if (!hitPlayer.IsAlive)
                    {
                        hitPlayer.Deaths++;
                        if (Players.TryGetValue(projectile.OwnerId, out var killer))
                        {
                            killer.Kills++;
                        }

                        if (Settings.Mode != GameMode.PvpElimination)
                        {
                            hitPlayer.RespawnTimer = GameConstants.DefaultRespawnTime;
                        }

                        PendingKillEvents.Add(new ServerKillEventPacket
                        {
                            VictimId = hitPlayerId,
                            KillerId = projectile.OwnerId
                        });
                    }
                }

                Projectiles.RemoveAt(i);
                hit = true;
            }

            // In PvE mode, player projectiles also check collision with AI enemies
            if (!hit && Settings.Mode == GameMode.CoopSurvival && projectile.OwnerId != 255)
            {
                for (int e = Enemies.Count - 1; e >= 0; e--)
                {
                    var enemy = Enemies[e];
                    if (!enemy.IsAlive) continue;

                    if (CollisionHelper.CheckCircleCollision(projectile.X, projectile.Y, ProjectileRadius, enemy.X, enemy.Y, EnemyRadius))
                    {
                        enemy.ApplyDamage(projectile.Damage);
                        PendingDamageEvents.Add(new ServerDamageEventPacket
                        {
                            TargetId = (byte)enemy.Id,
                            AttackerId = projectile.OwnerId,
                            Damage = projectile.Damage,
                            HitX = projectile.X,
                            HitY = projectile.Y
                        });

                        if (!enemy.IsAlive)
                        {
                            if (Players.TryGetValue(projectile.OwnerId, out var killer))
                            {
                                killer.Kills++;
                            }
                            Enemies.RemoveAt(e);
                        }

                        Projectiles.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }

    private void UpdatePvE(float deltaTime)
    {
        // Wave spawning logic
        if (Enemies.Count == 0)
        {
            WaveSpawnTimer -= deltaTime;
            if (WaveSpawnTimer <= 0f)
            {
                SpawnWave();
                WaveSpawnTimer = 5f;
            }
        }

        // Enemy AI movement and attack logic
        for (int i = Enemies.Count - 1; i >= 0; i--)
        {
            var enemy = Enemies[i];
            if (!enemy.IsAlive)
            {
                Enemies.RemoveAt(i);
                continue;
            }

            if (enemy.AttackCooldown > 0f)
            {
                enemy.AttackCooldown -= deltaTime;
            }

            // Target the nearest alive player
            PlayerState? nearestPlayer = null;
            float nearestDistSq = float.MaxValue;

            foreach (var p in Players.Values)
            {
                if (!p.IsAlive) continue;
                float dsq = CollisionHelper.DistanceSquared(enemy.X, enemy.Y, p.X, p.Y);
                if (dsq < nearestDistSq)
                {
                    nearestDistSq = dsq;
                    nearestPlayer = p;
                }
            }

            if (nearestPlayer == null) continue;

            float dx = nearestPlayer.X - enemy.X;
            float dy = nearestPlayer.Y - enemy.Y;
            float dist = MathF.Sqrt(nearestDistSq);
            float angle = MathF.Atan2(dy, dx);
            enemy.AimAngle = angle;

            // Move towards player
            if (dist > 28f)
            {
                float nextX = enemy.X + (MathF.Cos(angle) * enemy.MoveSpeed * deltaTime);
                float nextY = enemy.Y + (MathF.Sin(angle) * enemy.MoveSpeed * deltaTime);
                var clamped = Physics.ClampToWalkable(nextX, nextY, EnemyRadius);
                enemy.X = clamped.x;
                enemy.Y = clamped.y;
            }

            // Attack player
            if (dist <= 35f && enemy.AttackCooldown <= 0f)
            {
                enemy.AttackCooldown = 1.0f;
                nearestPlayer.ApplyDamage(enemy.Damage);
                PendingDamageEvents.Add(new ServerDamageEventPacket
                {
                    TargetId = nearestPlayer.PlayerId,
                    AttackerId = 255, // 255 designates AI enemy
                    Damage = enemy.Damage,
                    HitX = nearestPlayer.X,
                    HitY = nearestPlayer.Y
                });

                if (!nearestPlayer.IsAlive)
                {
                    nearestPlayer.Deaths++;
                    PendingKillEvents.Add(new ServerKillEventPacket
                    {
                        VictimId = nearestPlayer.PlayerId,
                        KillerId = 255
                    });
                }
            }
            else if (enemy.Type == EnemyType.Shooter && dist <= 260f && enemy.AttackCooldown <= 0f)
            {
                enemy.AttackCooldown = 2.0f;
                SpawnProjectile(255, enemy.X, enemy.Y, enemy.AimAngle, enemy.Damage);
            }
        }
    }

    private void SpawnWave()
    {
        int enemyCount = 3 + (CurrentWave * 2);
        for (int i = 0; i < enemyCount; i++)
        {
            var (spawnX, spawnY) = Spawns.GetRandomSpawnPoint();
            EnemyType type = (EnemyType)(i % 4);

            short hp = type switch
            {
                EnemyType.Fast => 30,
                EnemyType.Brute => 120,
                EnemyType.Shooter => 45,
                _ => 60
            };

            float speed = type switch
            {
                EnemyType.Fast => 160f,
                EnemyType.Brute => 70f,
                EnemyType.Shooter => 90f,
                _ => 110f
            };

            short dmg = type switch
            {
                EnemyType.Brute => 25,
                EnemyType.Shooter => 12,
                _ => 15
            };

            Enemies.Add(new EnemyState
            {
                Id = ++NextEnemyId,
                Type = type,
                X = spawnX,
                Y = spawnY,
                Health = hp,
                MaxHealth = hp,
                MoveSpeed = speed,
                Damage = dmg,
                AttackCooldown = 1.0f
            });
        }

        CurrentWave++;
    }

    private void UpdateRespawns(float deltaTime)
    {
        foreach (var player in Players.Values)
        {
            if (!player.IsAlive)
            {
                player.RespawnTimer -= deltaTime;
                if (player.RespawnTimer <= 0f)
                {
                    var (spawnX, spawnY) = Spawns.GetRandomSpawnPoint();
                    player.Respawn(spawnX, spawnY);
                    PendingRespawnEvents.Add(new ServerRespawnPacket
                    {
                        PlayerId = player.PlayerId,
                        X = spawnX,
                        Y = spawnY
                    });
                }
            }
        }
    }

    /// <summary>
    /// Constructs a full world snapshot customized for the specified recipient player.
    /// </summary>
    /// <param name="forPlayerId">Target player recipient ID.</param>
    /// <returns>A populated <see cref="ServerSnapshotPacket"/>.</returns>
    public ServerSnapshotPacket CreateSnapshot(byte forPlayerId)
    {
        uint lastInput = _lastProcessedInputs.GetValueOrDefault(forPlayerId, 0u);
        var playerSnapshots = Players.Values.Select(p => p.ToSnapshot()).ToArray();
        var projectileSnapshots = Projectiles.Select(p => p.ToSnapshot()).ToArray();
        var pickupSnapshots = Pickups.Select(p => p.ToSnapshot()).ToArray();
        var enemySnapshots = Enemies.Count > 0 ? Enemies.Select(e => e.ToSnapshot()).ToArray() : null;

        return new ServerSnapshotPacket
        {
            Tick = CurrentTick,
            LastProcessedInput = lastInput,
            Players = playerSnapshots,
            Projectiles = projectileSnapshots,
            Pickups = pickupSnapshots,
            Enemies = enemySnapshots
        };
    }

    /// <summary>
    /// Spawns an authoritative projectile moving in the specified direction.
    /// </summary>
    /// <param name="ownerId">Player ID who fired the projectile.</param>
    /// <param name="x">Initial X origin coordinate.</param>
    /// <param name="y">Initial Y origin coordinate.</param>
    /// <param name="angle">Trajectory angle in radians.</param>
    /// <param name="damage">Damage dealt on contact.</param>
    public void SpawnProjectile(byte ownerId, float x, float y, float angle, short damage)
    {
        var projectile = new ProjectileState
        {
            Id = ++NextProjectileId,
            X = x,
            Y = y,
            Angle = angle,
            Speed = GameConstants.ProjectileDefaultSpeed,
            OwnerId = ownerId,
            ProjectileType = 0,
            Damage = damage > 0 ? damage : GameConstants.ProjectileDefaultDamage,
            Lifetime = 0f,
            MaxLifetime = GameConstants.ProjectileDefaultLifetime
        };

        Projectiles.Add(projectile);
    }
}
