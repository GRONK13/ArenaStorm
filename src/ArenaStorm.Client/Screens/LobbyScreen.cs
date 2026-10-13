using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ArenaStorm.Client.Rendering;
using ArenaStorm.Client.Services;
using ArenaStorm.Shared.Constants;
using ArenaStorm.Shared.Models;

namespace ArenaStorm.Client.Screens;

/// <summary>
/// Lobby and character selection screen coordinating matchmaking and hero customization.
/// </summary>
public class LobbyScreen : Screen
{
    private readonly ApiClient _api;
    private CharacterClass _selectedClass;

    private List<LobbyItemDto> _lobbies = [];
    private float _refreshTimer = 5.0f;
    private string _status = "Select your Character Archetype and Enter Match";

    /// <summary>
    /// Initializes a new instance of the <see cref="LobbyScreen"/> class.
    /// </summary>
    public LobbyScreen(ScreenManager screenManager, ApiClient api, CharacterClass initialClass = CharacterClass.Ranger)
        : base(screenManager)
    {
        _api = api;
        _selectedClass = initialClass;
    }

    /// <inheritdoc/>
    public override void LoadContent()
    {
        PrimitiveRenderer.Initialize(ScreenManager.Game.GraphicsDevice);
        RefreshLobbies();
    }

    private async void RefreshLobbies()
    {
        _lobbies = await _api.GetLobbiesAsync();
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _refreshTimer -= dt;
        if (_refreshTimer <= 0f)
        {
            _refreshTimer = 5.0f;
            RefreshLobbies();
        }

        var input = ScreenManager.InputManager;
        if (input.IsLeftMouseClicked)
        {
            int mx = input.MouseX;
            int my = input.MouseY;

            // Class selection cards (top-left panel, y from 140 to 360)
            if (mx >= 80 && mx <= 280)
            {
                if (my >= 140 && my <= 190) _selectedClass = CharacterClass.Ranger;
                else if (my >= 200 && my <= 250) _selectedClass = CharacterClass.Tank;
                else if (my >= 260 && my <= 310) _selectedClass = CharacterClass.Mage;
                else if (my >= 320 && my <= 370) _selectedClass = CharacterClass.Warrior;
            }

            // "ENTER ARENA / QUICK PLAY" button
            if (mx >= 80 && mx <= 500 && my >= 580 && my <= 640)
            {
                StartGame();
            }

            // "VIEW LEADERBOARDS" button
            if (mx >= 540 && mx <= 800 && my >= 580 && my <= 640)
            {
                ScreenManager.ChangeScreen(new LeaderboardScreen(ScreenManager, _api, _selectedClass));
            }

            // "LOG OUT" button
            if (mx >= 1120 && mx <= 1220 && my >= 30 && my <= 65)
            {
                ScreenManager.ChangeScreen(new LoginScreen(ScreenManager));
            }

            // Join specific lobby row
            if (mx >= 540 && mx <= 1200)
            {
                for (int i = 0; i < Math.Min(5, _lobbies.Count); i++)
                {
                    int rowY = 180 + (i * 60);
                    if (my >= rowY && my <= rowY + 50)
                    {
                        JoinSpecificLobby(_lobbies[i].Id);
                        break;
                    }
                }
            }
        }
    }

    private async void JoinSpecificLobby(string lobbyId)
    {
        _status = $"Joining lobby {lobbyId}...";
        bool joined = await _api.JoinLobbyAsync(lobbyId);
        if (joined)
        {
            StartGame();
        }
        else
        {
            _status = $"Failed to join lobby {lobbyId}";
        }
    }

    private void StartGame()
    {
        ScreenManager.ChangeScreen(new GameScreen(
            ScreenManager,
            _api,
            _selectedClass,
            _api.ServerHost,
            NetworkConstants.GameServerPort));
    }

    /// <inheritdoc/>
    public override void Draw(GameTime gameTime)
    {
        SpriteBatch sb = ScreenManager.SpriteBatch;
        sb.Begin();

        // Background
        PrimitiveRenderer.DrawRect(sb, 0, 0, 1280, 720, new Color(18, 20, 26));

        // Header
        PrimitiveRenderer.DrawRect(sb, 0, 0, 1280, 80, new Color(26, 30, 40));
        PrimitiveRenderer.DrawRectOutline(sb, 0, 0, 1280, 80, Color.DarkSlateGray, 1);

        PrimitiveRenderer.DrawString(sb, "ARENA STORM LOBBY", 40, 26, Color.Gold, 3);
        PrimitiveRenderer.DrawString(sb, $"PLAYER: {_api.DisplayName}", 450, 32, Color.White, 2);

        // Logout Button
        PrimitiveRenderer.DrawRect(sb, 1120, 24, 110, 36, new Color(120, 40, 40));
        PrimitiveRenderer.DrawRectOutline(sb, 1120, 24, 110, 36, Color.Red, 1);
        PrimitiveRenderer.DrawString(sb, "LOGOUT", 1140, 34, Color.White, 2);

        // --- Left Panel: Class Selection ---
        PrimitiveRenderer.DrawRect(sb, 60, 100, 440, 450, new Color(24, 28, 36));
        PrimitiveRenderer.DrawRectOutline(sb, 60, 100, 440, 450, Color.DarkSlateGray, 1);
        PrimitiveRenderer.DrawString(sb, "SELECT CHARACTER ARCHETYPE", 80, 115, Color.CornflowerBlue, 2);

        DrawClassButton(sb, CharacterClass.Ranger, "1. RANGER", 80, 140, Color.SeaGreen);
        DrawClassButton(sb, CharacterClass.Tank, "2. TANK", 80, 200, Color.SteelBlue);
        DrawClassButton(sb, CharacterClass.Mage, "3. MAGE", 80, 260, Color.MediumPurple);
        DrawClassButton(sb, CharacterClass.Warrior, "4. WARRIOR", 80, 320, Color.IndianRed);

        // Archetype stats breakdown
        var classData = CharacterClassRegistry.Get(_selectedClass);
        PrimitiveRenderer.DrawRect(sb, 80, 390, 400, 140, new Color(16, 18, 22));
        PrimitiveRenderer.DrawRectOutline(sb, 80, 390, 400, 140, Color.Gray, 1);

        PrimitiveRenderer.DrawString(sb, $"CLASS: {classData.Name.ToUpper()}", 95, 405, Color.Gold, 2);
        PrimitiveRenderer.DrawString(sb, $"HEALTH:    {classData.MaxHealth} HP", 95, 430, Color.White, 2);
        PrimitiveRenderer.DrawString(sb, $"SPEED:     {classData.MoveSpeed:F1} TILES/SEC", 95, 450, Color.White, 2);
        PrimitiveRenderer.DrawString(sb, $"DAMAGE:    {classData.AttackDamage} DMG", 95, 470, Color.White, 2);
        PrimitiveRenderer.DrawString(sb, $"COOLDOWN:  {classData.AttackCooldown:F2}S", 95, 490, Color.White, 2);

        // --- Right Panel: Server / Lobbies ---
        PrimitiveRenderer.DrawRect(sb, 520, 100, 700, 450, new Color(24, 28, 36));
        PrimitiveRenderer.DrawRectOutline(sb, 520, 100, 700, 450, Color.DarkSlateGray, 1);
        PrimitiveRenderer.DrawString(sb, "AVAILABLE GAME SESSIONS", 540, 115, Color.CornflowerBlue, 2);

        if (_lobbies.Count == 0)
        {
            PrimitiveRenderer.DrawString(sb, "DEFAULT STANDALONE SERVER READY (PORT 9050)", 540, 180, Color.LightGray, 2);
            PrimitiveRenderer.DrawString(sb, "CLICK 'ENTER ARENA' TO JOIN ACTIVE MATCH", 540, 215, Color.Goldenrod, 2);
        }
        else
        {
            for (int i = 0; i < Math.Min(5, _lobbies.Count); i++)
            {
                var l = _lobbies[i];
                int y = 160 + (i * 65);
                PrimitiveRenderer.DrawRect(sb, 540, y, 660, 52, new Color(32, 36, 46));
                PrimitiveRenderer.DrawRectOutline(sb, 540, y, 660, 52, Color.DimGray, 1);

                PrimitiveRenderer.DrawString(sb, $"{l.Name.ToUpper()}", 555, y + 10, Color.Gold, 2);
                PrimitiveRenderer.DrawString(sb, $"HOST: {l.HostName}  PLAYERS: {l.CurrentPlayers}/{l.MaxPlayers}  MODE: {l.GameMode}", 555, y + 30, Color.LightGray, 2);

                PrimitiveRenderer.DrawRect(sb, 1110, y + 8, 80, 36, new Color(40, 100, 60));
                PrimitiveRenderer.DrawString(sb, "JOIN", 1130, y + 18, Color.White, 2);
            }
        }

        // --- Bottom Controls ---
        // Big Enter Match Button
        PrimitiveRenderer.DrawRect(sb, 60, 580, 440, 64, new Color(34, 130, 60));
        PrimitiveRenderer.DrawRectOutline(sb, 60, 580, 440, 64, Color.LimeGreen, 2);
        PrimitiveRenderer.DrawString(sb, "ENTER ARENA / QUICK PLAY", 120, 602, Color.White, 3);

        // Leaderboards Button
        PrimitiveRenderer.DrawRect(sb, 520, 580, 320, 64, new Color(40, 70, 140));
        PrimitiveRenderer.DrawRectOutline(sb, 520, 580, 320, 64, Color.DeepSkyBlue, 2);
        PrimitiveRenderer.DrawString(sb, "LEADERBOARDS", 580, 602, Color.White, 3);

        // Status banner
        PrimitiveRenderer.DrawString(sb, _status, 60, 665, Color.Yellow, 2);

        sb.End();
    }

    private void DrawClassButton(SpriteBatch sb, CharacterClass c, string label, int x, int y, Color baseColor)
    {
        bool isSelected = _selectedClass == c;
        Color bg = isSelected ? baseColor : new Color(32, 36, 44);
        Color border = isSelected ? Color.Gold : Color.DimGray;

        PrimitiveRenderer.DrawRect(sb, x, y, 400, 48, bg);
        PrimitiveRenderer.DrawRectOutline(sb, x, y, 400, 48, border, isSelected ? 3 : 1);
        PrimitiveRenderer.DrawString(sb, label, x + 18, y + 16, isSelected ? Color.White : Color.LightGray, 2);

        if (isSelected)
        {
            PrimitiveRenderer.DrawString(sb, "[SELECTED]", x + 280, y + 16, Color.Gold, 2);
        }
    }
}
