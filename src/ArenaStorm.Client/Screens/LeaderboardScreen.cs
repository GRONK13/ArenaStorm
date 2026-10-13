using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ArenaStorm.Client.Rendering;
using ArenaStorm.Client.Services;
using ArenaStorm.Shared.Models;

namespace ArenaStorm.Client.Screens;

/// <summary>
/// Displays competitive match rankings and player statistics.
/// </summary>
public class LeaderboardScreen : Screen
{
    private readonly ApiClient _api;
    private readonly CharacterClass _returnClass;

    private string _activeCategory = "kills";
    private List<LeaderboardItemDto> _entries = [];
    private bool _loading = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="LeaderboardScreen"/> class.
    /// </summary>
    public LeaderboardScreen(ScreenManager screenManager, ApiClient api, CharacterClass returnClass)
        : base(screenManager)
    {
        _api = api;
        _returnClass = returnClass;
    }

    /// <inheritdoc/>
    public override void LoadContent()
    {
        PrimitiveRenderer.Initialize(ScreenManager.Game.GraphicsDevice);
        FetchLeaderboard();
    }

    private async void FetchLeaderboard()
    {
        _loading = true;
        _entries = await _api.GetLeaderboardAsync(_activeCategory);
        _loading = false;
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        var input = ScreenManager.InputManager;
        if (input.IsLeftMouseClicked)
        {
            int mx = input.MouseX;
            int my = input.MouseY;

            // KILLS Category Tab
            if (mx >= 200 && mx <= 360 && my >= 110 && my <= 155)
            {
                if (_activeCategory != "kills")
                {
                    _activeCategory = "kills";
                    FetchLeaderboard();
                }
            }
            // WINS Category Tab
            else if (mx >= 380 && mx <= 540 && my >= 110 && my <= 155)
            {
                if (_activeCategory != "wins")
                {
                    _activeCategory = "wins";
                    FetchLeaderboard();
                }
            }
            // BACK BUTTON
            else if (mx >= 80 && mx <= 220 && my >= 620 && my <= 670)
            {
                ScreenManager.ChangeScreen(new LobbyScreen(ScreenManager, _api, _returnClass));
            }
        }
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
        PrimitiveRenderer.DrawString(sb, "GLOBAL LEADERBOARDS", 40, 26, Color.Gold, 3);

        // Category Tabs
        Color killsTabBg = _activeCategory == "kills" ? new Color(40, 80, 140) : new Color(30, 34, 42);
        PrimitiveRenderer.DrawRect(sb, 200, 110, 160, 44, killsTabBg);
        PrimitiveRenderer.DrawRectOutline(sb, 200, 110, 160, 44, Color.DimGray, 1);
        PrimitiveRenderer.DrawString(sb, "MOST KILLS", 230, 125, Color.White, 2);

        Color winsTabBg = _activeCategory == "wins" ? new Color(40, 80, 140) : new Color(30, 34, 42);
        PrimitiveRenderer.DrawRect(sb, 380, 110, 160, 44, winsTabBg);
        PrimitiveRenderer.DrawRectOutline(sb, 380, 110, 160, 44, Color.DimGray, 1);
        PrimitiveRenderer.DrawString(sb, "MOST WINS", 415, 125, Color.White, 2);

        // Leaderboard Table Container
        PrimitiveRenderer.DrawRect(sb, 200, 170, 880, 420, new Color(24, 28, 36));
        PrimitiveRenderer.DrawRectOutline(sb, 200, 170, 880, 420, Color.DarkSlateGray, 1);

        // Table Header
        PrimitiveRenderer.DrawRect(sb, 200, 170, 880, 40, new Color(32, 38, 50));
        PrimitiveRenderer.DrawString(sb, "RANK", 230, 182, Color.Gold, 2);
        PrimitiveRenderer.DrawString(sb, "PLAYER NAME", 360, 182, Color.Gold, 2);
        PrimitiveRenderer.DrawString(sb, _activeCategory.ToUpper(), 960, 182, Color.Gold, 2);

        if (_loading)
        {
            PrimitiveRenderer.DrawString(sb, "LOADING LEADERBOARD ENTRIES...", 480, 340, Color.Yellow, 2);
        }
        else if (_entries.Count == 0)
        {
            PrimitiveRenderer.DrawString(sb, "NO MATCH RECORDS AVAILABLE YET", 480, 340, Color.LightGray, 2);
        }
        else
        {
            for (int i = 0; i < Math.Min(8, _entries.Count); i++)
            {
                var entry = _entries[i];
                int rowY = 220 + (i * 45);

                Color rowBg = i % 2 == 0 ? new Color(26, 30, 38) : new Color(22, 26, 34);
                PrimitiveRenderer.DrawRect(sb, 210, rowY, 860, 38, rowBg);

                Color rankColor = i switch
                {
                    0 => Color.Gold,
                    1 => Color.Silver,
                    2 => new Color(205, 127, 50),
                    _ => Color.LightGray
                };

                PrimitiveRenderer.DrawString(sb, $"#{entry.Rank}", 230, rowY + 10, rankColor, 2);
                PrimitiveRenderer.DrawString(sb, entry.DisplayName, 360, rowY + 10, Color.White, 2);
                PrimitiveRenderer.DrawString(sb, entry.Value.ToString(), 980, rowY + 10, Color.LimeGreen, 2);
            }
        }

        // Back Button
        PrimitiveRenderer.DrawRect(sb, 200, 610, 180, 48, new Color(50, 54, 66));
        PrimitiveRenderer.DrawRectOutline(sb, 200, 610, 180, 48, Color.Gray, 1);
        PrimitiveRenderer.DrawString(sb, "<- BACK TO LOBBY", 215, 626, Color.White, 2);

        sb.End();
    }
}
