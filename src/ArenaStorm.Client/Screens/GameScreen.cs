using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ArenaStorm.Client.Audio;
using ArenaStorm.Client.Input;
using ArenaStorm.Client.Networking;
using ArenaStorm.Client.Rendering;
using ArenaStorm.Client.Services;
using ArenaStorm.Shared.Constants;
using ArenaStorm.Shared.Models;
using ArenaStorm.Shared.Protocol;
using ArenaStorm.Shared.Utils;

namespace ArenaStorm.Client.Screens;

/// <summary>
/// Primary authoritative gameplay screen managing client simulation, prediction,
/// entity interpolation, combat visual effects, pickups, PvE enemies, audio, and HUD.
/// </summary>
public class GameScreen : Screen
{
    private const int TileSize = GameConstants.TileSize;
    private const float PlayerRadius = 14f;

    private readonly ApiClient _api;
    private readonly CharacterClass _selectedClass;
    private readonly string _serverHost;
    private readonly int _serverPort;

    private readonly ServerConnection _connection;
    private readonly Camera _camera;
    private readonly ArenaMap _map;

    private byte _localPlayerId = 1;
    private float _localX;
    private float _localY;
    private float _localAimAngle;
    private short _localHealth = 100;
    private short _localMaxHealth = 100;
    private short _localShield;
    private int _localKills;
    private int _localDeaths;

    private uint _nextInputSequence = 1;
    private readonly List<SavedInput> _pendingInputs = [];

    // Interpolated remote players
    private readonly Dictionary<byte, RemotePlayerDisplay> _remotePlayers = [];

    // Server-synchronized entities
    private readonly List<ProjectileSnapshot> _serverProjectiles = [];
    private readonly List<PickupSnapshot> _serverPickups = [];
    private readonly List<EnemySnapshot> _serverEnemies = [];

    // Offline sandbox simulation entities
    private readonly List<LocalProjectile> _localProjectiles = [];

    // Visual melee slash effects
    private readonly List<VisualSlashEffect> _visualSlashes = [];

    // In-game text chat log & typing state
    private bool _isChatOpen;
    private string _currentChatInput = string.Empty;
    private readonly List<ChatLogEntry> _chatLog = [];

    private readonly record struct SavedInput(uint Sequence, byte Flags, float DeltaTime);
    private readonly record struct LocalProjectile(Vector2 Position, float Angle, float Lifetime);
    private readonly record struct VisualSlashEffect(Vector2 Position, float Angle, float Range, float Duration);
    private readonly record struct ChatLogEntry(string Sender, string Message, float TimeRemaining);

    private class RemotePlayerDisplay
    {
        public byte PlayerId { get; set; }
        public float CurrentX { get; set; }
        public float CurrentY { get; set; }
        public float TargetX { get; set; }
        public float TargetY { get; set; }
        public float AimAngle { get; set; }
        public short Health { get; set; }
        public short Shield { get; set; }
        public byte ClassId { get; set; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameScreen"/> class.
    /// </summary>
    public GameScreen(
        ScreenManager screenManager,
        ApiClient? api = null,
        CharacterClass selectedClass = CharacterClass.Ranger,
        string host = NetworkConstants.DefaultHost,
        int port = NetworkConstants.GameServerPort)
        : base(screenManager)
    {
        _api = api ?? new ApiClient();
        _selectedClass = selectedClass;
        _serverHost = host;
        _serverPort = port;

        _map = new ArenaMap();
        _connection = new ServerConnection();
        _camera = new Camera();

        var classData = CharacterClassRegistry.Get(_selectedClass);
        _localHealth = classData.MaxHealth;
        _localMaxHealth = classData.MaxHealth;

        // Position at central lane by default
        _localX = 15f * TileSize;
        _localY = 10f * TileSize;

        SubscribeNetworkEvents();
    }

    private void SubscribeNetworkEvents()
    {
        _connection.OnConnected += () =>
        {
            _pendingInputs.Clear();
            _nextInputSequence = 1;
            _connection.SendClassSelect(_selectedClass);
            AddChatMessage("SYSTEM", "Connected to game server. Handshaking...");
        };

        _connection.OnWelcome += welcome =>
        {
            _localPlayerId = welcome.PlayerId;
            AddChatMessage("SYSTEM", $"Welcome to room {welcome.RoomId}! Assigned PlayerId #{welcome.PlayerId}");
        };

        _connection.OnDamageEvent += dmg =>
        {
            if (dmg.TargetId == _localPlayerId)
            {
                _localHealth = (short)Math.Max(0, _localHealth - dmg.Damage);
                SoundManager.PlayHit();
            }
        };

        _connection.OnKillEvent += kill =>
        {
            if (kill.KillerId == _localPlayerId)
            {
                _localKills++;
                SoundManager.PlayKill();
                AddChatMessage("KILL", "You eliminated an opponent!");
            }
            if (kill.VictimId == _localPlayerId)
            {
                _localDeaths++;
                SoundManager.PlayDeath();
                AddChatMessage("DEATH", "You were defeated!");
            }
        };

        _connection.OnMeleeAttack += melee =>
        {
            // Spawn visual slash effect
            Vector2 origin = melee.AttackerId == _localPlayerId
                ? new Vector2(_localX, _localY)
                : _remotePlayers.TryGetValue(melee.AttackerId, out var rp) ? new Vector2(rp.CurrentX, rp.CurrentY) : Vector2.Zero;

            _visualSlashes.Add(new VisualSlashEffect(origin, melee.Angle, melee.Range, 0.2f));
            SoundManager.PlayMelee();
        };

        _connection.OnChatRelay += chat =>
        {
            string sender = chat.SenderId == _localPlayerId ? "YOU" : $"Player #{chat.SenderId}";
            AddChatMessage(sender, chat.Message);
        };
    }

    /// <inheritdoc/>
    public override void LoadContent()
    {
        PrimitiveRenderer.Initialize(ScreenManager.Game.GraphicsDevice);
        SoundManager.Initialize();
        _connection.Connect(_serverHost, _serverPort);
    }

    /// <inheritdoc/>
    public override void UnloadContent()
    {
        _connection.Disconnect();
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (dt <= 0f) return;

        // Escape key returns to Lobby
        if (ScreenManager.InputManager.IsKeyPressed(Keys.Escape))
        {
            if (_isChatOpen)
            {
                _isChatOpen = false;
                _currentChatInput = string.Empty;
            }
            else
            {
                ScreenManager.ChangeScreen(new LobbyScreen(ScreenManager, _api, _selectedClass));
                return;
            }
        }

        // 1. Process server packets
        _connection.Update();
        ServerSnapshotPacket? snapshot = _connection.GetLatestSnapshot();
        if (snapshot.HasValue)
        {
            ProcessServerSnapshot(snapshot.Value);
        }

        // 2. Chat input toggle
        if (!_isChatOpen && (ScreenManager.InputManager.IsKeyPressed(Keys.Enter) || ScreenManager.InputManager.IsKeyPressed(Keys.T)))
        {
            _isChatOpen = true;
            _currentChatInput = string.Empty;
        }
        else if (_isChatOpen)
        {
            HandleChatInput();
        }

        // 3. Movement and Aim inputs
        byte inputFlags = _isChatOpen ? (byte)0 : InputState.GetInputFlags(ScreenManager.InputManager);
        Vector2 mouseScreen = new(ScreenManager.InputManager.MouseX, ScreenManager.InputManager.MouseY);
        Vector2 mouseWorld = _camera.ScreenToWorld(mouseScreen, ScreenManager.Game.GraphicsDevice);
        _localAimAngle = MathF.Atan2(mouseWorld.Y - _localY, mouseWorld.X - _localX);

        // 4. Client-side prediction
        var classData = CharacterClassRegistry.Get(_selectedClass);
        float moveSpeed = classData.MoveSpeed * TileSize;
        Vector2 moveDirection = GetMovementDirection(inputFlags);
        _localX += moveDirection.X * moveSpeed * dt;
        _localY += moveDirection.Y * moveSpeed * dt;
        ResolveCollisions();

        // 5. Transmit input packet
        uint sequence = _nextInputSequence++;
        _pendingInputs.Add(new SavedInput(sequence, inputFlags, dt));

        if (_connection.IsConnected)
        {
            _connection.SendInput(new ClientInputPacket
            {
                SequenceNumber = sequence,
                InputFlags = inputFlags,
                AimAngle = _localAimAngle,
                Timestamp = (uint)gameTime.TotalGameTime.TotalMilliseconds
            });

            // Local audio trigger for firing and melee
            if ((inputFlags & InputFlags.Fire) != 0 && ScreenManager.InputManager.IsLeftMouseClicked)
            {
                SoundManager.PlayShoot();
            }
            if ((inputFlags & InputFlags.Melee) != 0 && ScreenManager.InputManager.IsRightMouseClicked)
            {
                SoundManager.PlayMelee();
            }
        }
        else
        {
            // Offline sandbox shooting
            if (ScreenManager.InputManager.IsLeftMouseClicked)
            {
                _localProjectiles.Add(new LocalProjectile(new Vector2(_localX, _localY), _localAimAngle, 0f));
                SoundManager.PlayShoot();
            }
        }

        // 6. Update visual slashes & offline projectiles
        UpdateVisualEffects(dt);

        // 7. Interpolate remote players towards snapshot positions
        foreach (var rp in _remotePlayers.Values)
        {
            rp.CurrentX = MathHelper.Lerp(rp.CurrentX, rp.TargetX, MathF.Min(1f, 15f * dt));
            rp.CurrentY = MathHelper.Lerp(rp.CurrentY, rp.TargetY, MathF.Min(1f, 15f * dt));
        }

        // 8. Update chat messages timers
        for (int i = _chatLog.Count - 1; i >= 0; i--)
        {
            float rem = _chatLog[i].TimeRemaining - dt;
            if (rem <= 0f) _chatLog.RemoveAt(i);
            else _chatLog[i] = _chatLog[i] with { TimeRemaining = rem };
        }

        // 9. Camera track
        _camera.Follow(_localX, _localY, 0.12f);
    }

    private void HandleChatInput()
    {
        var keyboard = Keyboard.GetState();
        if (ScreenManager.InputManager.IsKeyPressed(Keys.Enter))
        {
            if (!string.IsNullOrWhiteSpace(_currentChatInput))
            {
                _connection.SendChat(_currentChatInput);
            }
            _isChatOpen = false;
            _currentChatInput = string.Empty;
            return;
        }

        if (ScreenManager.InputManager.IsKeyPressed(Keys.Back) && _currentChatInput.Length > 0)
        {
            _currentChatInput = _currentChatInput[..^1];
        }

        foreach (var key in keyboard.GetPressedKeys())
        {
            if (ScreenManager.InputManager.IsKeyPressed(key))
            {
                char? ch = KeyToChar(key, keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift));
                if (ch.HasValue && _currentChatInput.Length < 60)
                {
                    _currentChatInput += ch.Value;
                }
            }
        }
    }

    private static char? KeyToChar(Keys key, bool shift)
    {
        if (key >= Keys.A && key <= Keys.Z)
        {
            char c = (char)('a' + (key - Keys.A));
            return shift ? char.ToUpper(c) : c;
        }
        if (key >= Keys.D0 && key <= Keys.D9) return (char)('0' + (key - Keys.D0));
        if (key == Keys.Space) return ' ';
        return null;
    }

    private void AddChatMessage(string sender, string message)
    {
        _chatLog.Add(new ChatLogEntry(sender, message, 8.0f));
        if (_chatLog.Count > 6) _chatLog.RemoveAt(0);
    }

    private void ProcessServerSnapshot(ServerSnapshotPacket snapshot)
    {
        if (snapshot.Players != null)
        {
            foreach (var p in snapshot.Players)
            {
                if (p.PlayerId == _localPlayerId)
                {
                    _localHealth = p.Health;
                    _localShield = p.Shield;

                    // Reconcile client prediction
                    _pendingInputs.RemoveAll(i => i.Sequence <= snapshot.LastProcessedInput);
                    float recX = p.X;
                    float recY = p.Y;
                    var classData = CharacterClassRegistry.Get(_selectedClass);
                    float speed = classData.MoveSpeed * TileSize;

                    foreach (var input in _pendingInputs)
                    {
                        Vector2 dir = GetMovementDirection(input.Flags);
                        recX += dir.X * speed * input.DeltaTime;
                        recY += dir.Y * speed * input.DeltaTime;
                    }

                    _localX = recX;
                    _localY = recY;
                    ResolveCollisions();
                }
                else
                {
                    // Interpolate remote player
                    if (!_remotePlayers.TryGetValue(p.PlayerId, out var remote))
                    {
                        remote = new RemotePlayerDisplay
                        {
                            PlayerId = p.PlayerId,
                            CurrentX = p.X,
                            CurrentY = p.Y,
                            TargetX = p.X,
                            TargetY = p.Y
                        };
                        _remotePlayers[p.PlayerId] = remote;
                    }

                    remote.TargetX = p.X;
                    remote.TargetY = p.Y;
                    remote.AimAngle = p.AimAngle;
                    remote.Health = p.Health;
                    remote.Shield = p.Shield;
                    remote.ClassId = p.ClassId;
                }
            }
        }

        _serverProjectiles.Clear();
        if (snapshot.Projectiles != null) _serverProjectiles.AddRange(snapshot.Projectiles);

        _serverPickups.Clear();
        if (snapshot.Pickups != null) _serverPickups.AddRange(snapshot.Pickups);

        _serverEnemies.Clear();
        if (snapshot.Enemies != null) _serverEnemies.AddRange(snapshot.Enemies);
    }

    private void ResolveCollisions()
    {
        float minBoundX = TileSize + PlayerRadius;
        float maxBoundX = ((_map.Width - 1) * TileSize) - PlayerRadius;
        float minBoundY = TileSize + PlayerRadius;
        float maxBoundY = ((_map.Height - 1) * TileSize) - PlayerRadius;

        _localX = Math.Clamp(_localX, minBoundX, maxBoundX);
        _localY = Math.Clamp(_localY, minBoundY, maxBoundY);

        // Resolve against solid map tiles
        int minTileX = Math.Max(0, (int)MathF.Floor((_localX - PlayerRadius) / TileSize));
        int maxTileX = Math.Min(_map.Width - 1, (int)MathF.Floor((_localX + PlayerRadius) / TileSize));
        int minTileY = Math.Max(0, (int)MathF.Floor((_localY - PlayerRadius) / TileSize));
        int maxTileY = Math.Min(_map.Height - 1, (int)MathF.Floor((_localY + PlayerRadius) / TileSize));

        for (int tx = minTileX; tx <= maxTileX; tx++)
        {
            for (int ty = minTileY; ty <= maxTileY; ty++)
            {
                if (_map.IsSolid(tx, ty))
                {
                    float rx = tx * TileSize;
                    float ry = ty * TileSize;
                    if (CollisionHelper.CheckCircleRect(_localX, _localY, PlayerRadius, rx, ry, TileSize, TileSize))
                    {
                        float cx = rx + (TileSize * 0.5f);
                        float cy = ry + (TileSize * 0.5f);
                        float dx = _localX - cx;
                        float dy = _localY - cy;

                        if (MathF.Abs(dx) > MathF.Abs(dy))
                            _localX = dx > 0 ? rx + TileSize + PlayerRadius : rx - PlayerRadius;
                        else
                            _localY = dy > 0 ? ry + TileSize + PlayerRadius : ry - PlayerRadius;
                    }
                }
            }
        }
    }

    private void UpdateVisualEffects(float dt)
    {
        for (int i = _visualSlashes.Count - 1; i >= 0; i--)
        {
            float rem = _visualSlashes[i].Duration - dt;
            if (rem <= 0f) _visualSlashes.RemoveAt(i);
            else _visualSlashes[i] = _visualSlashes[i] with { Duration = rem };
        }

        for (int i = _localProjectiles.Count - 1; i >= 0; i--)
        {
            var p = _localProjectiles[i];
            float nextLifetime = p.Lifetime + dt;
            if (nextLifetime >= GameConstants.ProjectileDefaultLifetime)
            {
                _localProjectiles.RemoveAt(i);
            }
            else
            {
                Vector2 nextPos = p.Position + (new Vector2(MathF.Cos(p.Angle), MathF.Sin(p.Angle)) * GameConstants.ProjectileDefaultSpeed * dt);
                _localProjectiles[i] = new LocalProjectile(nextPos, p.Angle, nextLifetime);
            }
        }
    }

    private static Vector2 GetMovementDirection(byte flags)
    {
        float dx = 0f;
        float dy = 0f;
        if ((flags & InputFlags.Right) != 0) dx += 1f;
        if ((flags & InputFlags.Left) != 0) dx -= 1f;
        if ((flags & InputFlags.Down) != 0) dy += 1f;
        if ((flags & InputFlags.Up) != 0) dy -= 1f;

        Vector2 dir = new(dx, dy);
        if (dir.LengthSquared() > 0f) dir.Normalize();
        return dir;
    }

    /// <inheritdoc/>
    public override void Draw(GameTime gameTime)
    {
        SpriteBatch sb = ScreenManager.SpriteBatch;
        GraphicsDevice device = ScreenManager.Game.GraphicsDevice;

        // --- World Space Layer ---
        sb.Begin(transformMatrix: _camera.GetTransformMatrix(device));

        DrawTileMap(sb);
        DrawPickups(sb);
        DrawProjectiles(sb);
        DrawEnemies(sb);
        DrawRemotePlayers(sb);
        DrawLocalPlayer(sb);
        DrawSlashEffects(sb);

        sb.End();

        // --- Screen Space HUD Layer ---
        sb.Begin();

        DrawStatusBar(sb);
        DrawHealthAndAbilities(sb);
        DrawScoreHUD(sb);
        DrawChatHUD(sb);

        // Tab scoreboard overlay
        if (ScreenManager.InputManager.IsKeyDown(Keys.Tab))
        {
            DrawFullScoreboard(sb);
        }

        sb.End();
    }

    private void DrawTileMap(SpriteBatch sb)
    {
        Color floorColor = new(45, 48, 56);
        Color floorBorder = new(38, 40, 48);
        Color wallColor = new(24, 26, 32);
        Color wallBorder = new(14, 16, 20);

        for (int tx = 0; tx < _map.Width; tx++)
        {
            for (int ty = 0; ty < _map.Height; ty++)
            {
                float x = tx * TileSize;
                float y = ty * TileSize;

                if (_map.IsSolid(tx, ty))
                {
                    PrimitiveRenderer.DrawRect(sb, x, y, TileSize, TileSize, wallColor);
                    PrimitiveRenderer.DrawRectOutline(sb, x, y, TileSize, TileSize, wallBorder, 1);
                }
                else
                {
                    PrimitiveRenderer.DrawRect(sb, x, y, TileSize, TileSize, floorColor);
                    PrimitiveRenderer.DrawRectOutline(sb, x, y, TileSize, TileSize, floorBorder, 1);
                }
            }
        }
    }

    private void DrawPickups(SpriteBatch sb)
    {
        foreach (var pickup in _serverPickups)
        {
            if (!pickup.IsActive) continue;

            Color pColor = pickup.PickupType switch
            {
                (byte)PickupType.Health => Color.LimeGreen,
                (byte)PickupType.Speed => Color.Yellow,
                (byte)PickupType.Damage => Color.OrangeRed,
                (byte)PickupType.Shield => Color.Cyan,
                _ => Color.White
            };

            PrimitiveRenderer.DrawFilledCircle(sb, pickup.X, pickup.Y, 10f, pColor);
            PrimitiveRenderer.DrawCircle(sb, pickup.X, pickup.Y, 12f, Color.White, 12);
        }
    }

    private void DrawProjectiles(SpriteBatch sb)
    {
        foreach (var p in _serverProjectiles)
        {
            PrimitiveRenderer.DrawFilledCircle(sb, p.X, p.Y, 4f, Color.Gold);
            PrimitiveRenderer.DrawCircle(sb, p.X, p.Y, 5f, Color.DarkOrange, 8);
        }

        foreach (var lp in _localProjectiles)
        {
            PrimitiveRenderer.DrawFilledCircle(sb, lp.Position.X, lp.Position.Y, 4f, Color.Gold);
            PrimitiveRenderer.DrawCircle(sb, lp.Position.X, lp.Position.Y, 5f, Color.DarkOrange, 8);
        }
    }

    private void DrawEnemies(SpriteBatch sb)
    {
        foreach (var e in _serverEnemies)
        {
            Color eColor = e.EnemyType switch
            {
                (byte)EnemyType.Fast => Color.Gold,
                (byte)EnemyType.Brute => Color.DarkRed,
                (byte)EnemyType.Shooter => Color.MediumPurple,
                _ => Color.Orange
            };

            float radius = e.EnemyType == (byte)EnemyType.Brute ? 16f : 12f;
            PrimitiveRenderer.DrawFilledCircle(sb, e.X, e.Y, radius, eColor);
            PrimitiveRenderer.DrawCircle(sb, e.X, e.Y, radius + 1f, Color.Black, 8);
            PrimitiveRenderer.DrawHealthBar(sb, e.X - 14, e.Y - radius - 8, 28, 3, Math.Max(0f, e.Health / 60f), Color.DarkRed, Color.Red);
        }
    }

    private void DrawRemotePlayers(SpriteBatch sb)
    {
        foreach (var rp in _remotePlayers.Values)
        {
            Color bodyColor = rp.ClassId switch
            {
                (byte)CharacterClass.Tank => Color.SteelBlue,
                (byte)CharacterClass.Mage => Color.MediumPurple,
                (byte)CharacterClass.Warrior => Color.IndianRed,
                _ => Color.SeaGreen
            };

            PrimitiveRenderer.DrawRect(sb, rp.CurrentX - 16, rp.CurrentY - 16, 32, 32, bodyColor);
            PrimitiveRenderer.DrawRectOutline(sb, rp.CurrentX - 16, rp.CurrentY - 16, 32, 32, Color.DarkSlateGray, 2);

            Vector2 aimEnd = new(rp.CurrentX + (MathF.Cos(rp.AimAngle) * 22f), rp.CurrentY + (MathF.Sin(rp.AimAngle) * 22f));
            PrimitiveRenderer.DrawLine(sb, new Vector2(rp.CurrentX, rp.CurrentY), aimEnd, Color.Gold, 2);

            // Health bar & shield
            PrimitiveRenderer.DrawHealthBar(sb, rp.CurrentX - 16, rp.CurrentY - 24, 32, 4, rp.Health / 100f, Color.DarkRed, Color.LimeGreen);
            if (rp.Shield > 0)
            {
                PrimitiveRenderer.DrawCircle(sb, rp.CurrentX, rp.CurrentY, 20f, Color.Cyan, 12);
            }
        }
    }

    private void DrawLocalPlayer(SpriteBatch sb)
    {
        Color playerColor = _selectedClass switch
        {
            CharacterClass.Tank => Color.DeepSkyBlue,
            CharacterClass.Mage => Color.MediumOrchid,
            CharacterClass.Warrior => Color.Crimson,
            _ => Color.MediumSeaGreen
        };

        PrimitiveRenderer.DrawRect(sb, _localX - 16, _localY - 16, 32, 32, playerColor);
        PrimitiveRenderer.DrawRectOutline(sb, _localX - 16, _localY - 16, 32, 32, Color.White, 2);

        Vector2 aimEnd = new(_localX + (MathF.Cos(_localAimAngle) * 24f), _localY + (MathF.Sin(_localAimAngle) * 24f));
        PrimitiveRenderer.DrawLine(sb, new Vector2(_localX, _localY), aimEnd, Color.Yellow, 2);

        PrimitiveRenderer.DrawHealthBar(sb, _localX - 16, _localY - 24, 32, 4, _localHealth / (float)_localMaxHealth, Color.DarkRed, Color.LimeGreen);

        if (_localShield > 0)
        {
            PrimitiveRenderer.DrawCircle(sb, _localX, _localY, 21f, Color.Cyan, 16);
        }
    }

    private void DrawSlashEffects(SpriteBatch sb)
    {
        foreach (var slash in _visualSlashes)
        {
            float arcSpan = MathF.PI / 3f;
            Vector2 left = slash.Position + (new Vector2(MathF.Cos(slash.Angle - arcSpan), MathF.Sin(slash.Angle - arcSpan)) * slash.Range);
            Vector2 center = slash.Position + (new Vector2(MathF.Cos(slash.Angle), MathF.Sin(slash.Angle)) * slash.Range);
            Vector2 right = slash.Position + (new Vector2(MathF.Cos(slash.Angle + arcSpan), MathF.Sin(slash.Angle + arcSpan)) * slash.Range);

            PrimitiveRenderer.DrawLine(sb, left, center, Color.LightGoldenrodYellow, 3);
            PrimitiveRenderer.DrawLine(sb, center, right, Color.LightGoldenrodYellow, 3);
        }
    }

    private void DrawStatusBar(SpriteBatch sb)
    {
        if (_connection.IsConnected)
        {
            PrimitiveRenderer.DrawFilledCircle(sb, 22, 22, 6, Color.LimeGreen);
            PrimitiveRenderer.DrawString(sb, $"CONNECTED  |  PING: {_connection.Ping} MS  |  TICK: {_connection.LastServerTick}  |  CLASS: {_selectedClass.ToString().ToUpper()}", 36, 16, Color.White, 2);
        }
        else
        {
            PrimitiveRenderer.DrawFilledCircle(sb, 22, 22, 6, Color.Gold);
            PrimitiveRenderer.DrawString(sb, "OFFLINE SANDBOX MODE  -  WASD: MOVE  |  LEFT-CLICK: SHOOT  |  ESC: MENU", 36, 16, Color.LightGoldenrodYellow, 2);
        }
    }

    private void DrawHealthAndAbilities(SpriteBatch sb)
    {
        float x = 20;
        float y = 630;
        float w = 260;
        float h = 70;

        PrimitiveRenderer.DrawRect(sb, x, y, w, h, new Color(15, 18, 24));
        PrimitiveRenderer.DrawRectOutline(sb, x, y, w, h, Color.DimGray, 1);

        string shieldText = _localShield > 0 ? $" (SHIELD: {_localShield})" : "";
        PrimitiveRenderer.DrawString(sb, $"HP: {_localHealth}/{_localMaxHealth}{shieldText}", x + 10, y + 10, Color.White, 2);
        PrimitiveRenderer.DrawHealthBar(sb, x + 10, y + 34, w - 20, 16, _localHealth / (float)_localMaxHealth, Color.DarkRed, Color.LimeGreen);

        if (_localShield > 0)
        {
            PrimitiveRenderer.DrawHealthBar(sb, x + 10, y + 54, w - 20, 4, _localShield / 100f, Color.DarkSlateGray, Color.Cyan);
        }
    }

    private void DrawScoreHUD(SpriteBatch sb)
    {
        float w = 200;
        float h = 70;
        float x = 1280 - w - 20;
        float y = 20;

        PrimitiveRenderer.DrawRect(sb, x, y, w, h, new Color(15, 18, 24));
        PrimitiveRenderer.DrawRectOutline(sb, x, y, w, h, Color.DimGray, 1);

        PrimitiveRenderer.DrawString(sb, "SCOREBOARD [TAB]", x + 12, y + 8, Color.Gold, 2);
        PrimitiveRenderer.DrawString(sb, $"KILLS:  {_localKills}", x + 12, y + 28, Color.White, 2);
        PrimitiveRenderer.DrawString(sb, $"DEATHS: {_localDeaths}", x + 12, y + 46, Color.White, 2);
    }

    private void DrawChatHUD(SpriteBatch sb)
    {
        int startY = 480;
        for (int i = 0; i < _chatLog.Count; i++)
        {
            var log = _chatLog[i];
            PrimitiveRenderer.DrawString(sb, $"[{log.Sender}]: {log.Message}", 20, startY + (i * 22), Color.LightCyan, 2);
        }

        if (_isChatOpen)
        {
            PrimitiveRenderer.DrawRect(sb, 20, 600, 420, 26, new Color(10, 12, 16));
            PrimitiveRenderer.DrawRectOutline(sb, 20, 600, 420, 26, Color.Gold, 1);
            PrimitiveRenderer.DrawString(sb, $"SAY: {_currentChatInput}_", 26, 606, Color.White, 2);
        }
    }

    private void DrawFullScoreboard(SpriteBatch sb)
    {
        // Semi-transparent overlay backdrop
        PrimitiveRenderer.DrawRect(sb, 240, 120, 800, 480, new Color(12, 14, 20));
        PrimitiveRenderer.DrawRectOutline(sb, 240, 120, 800, 480, Color.Gold, 2);

        PrimitiveRenderer.DrawString(sb, "MATCH SCOREBOARD", 490, 140, Color.Gold, 3);
        PrimitiveRenderer.DrawRect(sb, 260, 180, 760, 36, new Color(24, 28, 38));
        PrimitiveRenderer.DrawString(sb, "ID", 280, 190, Color.Gold, 2);
        PrimitiveRenderer.DrawString(sb, "PLAYER NAME", 360, 190, Color.Gold, 2);
        PrimitiveRenderer.DrawString(sb, "CLASS", 600, 190, Color.Gold, 2);
        PrimitiveRenderer.DrawString(sb, "KILLS", 750, 190, Color.Gold, 2);
        PrimitiveRenderer.DrawString(sb, "DEATHS", 850, 190, Color.Gold, 2);

        // Local player row
        PrimitiveRenderer.DrawRect(sb, 260, 226, 760, 32, new Color(30, 45, 65));
        PrimitiveRenderer.DrawString(sb, $"#{_localPlayerId}", 280, 234, Color.Cyan, 2);
        PrimitiveRenderer.DrawString(sb, $"{_api.DisplayName} (YOU)", 360, 234, Color.White, 2);
        PrimitiveRenderer.DrawString(sb, _selectedClass.ToString().ToUpper(), 600, 234, Color.LightGray, 2);
        PrimitiveRenderer.DrawString(sb, _localKills.ToString(), 760, 234, Color.LimeGreen, 2);
        PrimitiveRenderer.DrawString(sb, _localDeaths.ToString(), 860, 234, Color.IndianRed, 2);

        // Remote players rows
        int rIndex = 0;
        foreach (var rp in _remotePlayers.Values)
        {
            int rowY = 265 + (rIndex * 34);
            PrimitiveRenderer.DrawRect(sb, 260, rowY, 760, 30, new Color(20, 22, 30));
            PrimitiveRenderer.DrawString(sb, $"#{rp.PlayerId}", 280, rowY + 6, Color.Gold, 2);
            PrimitiveRenderer.DrawString(sb, $"Player #{rp.PlayerId}", 360, rowY + 6, Color.White, 2);
            PrimitiveRenderer.DrawString(sb, ((CharacterClass)rp.ClassId).ToString().ToUpper(), 600, rowY + 6, Color.LightGray, 2);
            PrimitiveRenderer.DrawString(sb, "-", 760, rowY + 6, Color.Gray, 2);
            PrimitiveRenderer.DrawString(sb, "-", 860, rowY + 6, Color.Gray, 2);
            rIndex++;
        }
    }
}
