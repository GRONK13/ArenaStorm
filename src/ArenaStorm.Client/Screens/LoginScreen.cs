using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ArenaStorm.Client.Rendering;
using ArenaStorm.Client.Services;
using ArenaStorm.Shared.Models;

namespace ArenaStorm.Client.Screens;

/// <summary>
/// Screen providing user authentication (login/register) and guest play access.
/// </summary>
public class LoginScreen : Screen
{
    private readonly ApiClient _api = new();

    private string _username = "Player1";
    private string _password = "password123";
    private string _serverHost = Environment.GetEnvironmentVariable("ARENA_SERVER_IP") ?? "127.0.0.1";
    private string _statusMessage = "Enter credentials or Play as Guest";
    private Color _statusColor = Color.LightGray;

    private int _focusedField; // 0 = Username, 1 = Password, 2 = Server Host
    private CharacterClass _selectedClass = CharacterClass.Ranger;
    private double _keyRepeatTimer;
    private Keys _lastHeldKey = Keys.None;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginScreen"/> class.
    /// </summary>
    public LoginScreen(ScreenManager screenManager) : base(screenManager)
    {
    }

    /// <inheritdoc/>
    public override void LoadContent()
    {
        PrimitiveRenderer.Initialize(ScreenManager.Game.GraphicsDevice);
        _api.SetServerHost(_serverHost);
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        var input = ScreenManager.InputManager;

        // Handle field navigation with Tab or Up/Down
        if (input.IsKeyPressed(Keys.Tab) || input.IsKeyPressed(Keys.Down))
        {
            _focusedField = (_focusedField + 1) % 3;
        }
        else if (input.IsKeyPressed(Keys.Up))
        {
            _focusedField = (_focusedField + 2) % 3;
        }

        // Handle text typing
        HandleKeyboardInput(gameTime);

        // Enter submits login
        if (input.IsKeyPressed(Keys.Enter))
        {
            DoLogin();
        }

        // Handle mouse button clicks
        if (input.IsLeftMouseClicked)
        {
            int mx = input.MouseX;
            int my = input.MouseY;

            // Username input box
            if (mx >= 440 && mx <= 840 && my >= 230 && my <= 265)
            {
                _focusedField = 0;
            }
            // Password input box
            else if (mx >= 440 && mx <= 840 && my >= 290 && my <= 325)
            {
                _focusedField = 1;
            }
            // Server Host input box
            else if (mx >= 440 && mx <= 840 && my >= 350 && my <= 385)
            {
                _focusedField = 2;
            }
            // "LOGIN" button
            else if (mx >= 440 && mx <= 630 && my >= 405 && my <= 445)
            {
                DoLogin();
            }
            // "REGISTER" button
            else if (mx >= 650 && mx <= 840 && my >= 405 && my <= 445)
            {
                DoRegister();
            }
            // "PLAY AS GUEST / QUICK PLAY" button
            else if (mx >= 440 && mx <= 840 && my >= 460 && my <= 500)
            {
                PlayAsGuest();
            }
        }
    }

    private void HandleKeyboardInput(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var pressedKeys = keyboard.GetPressedKeys();

        if (pressedKeys.Length == 0)
        {
            _lastHeldKey = Keys.None;
            return;
        }

        Keys key = pressedKeys[0];
        bool shouldProcess = false;

        if (key != _lastHeldKey)
        {
            _lastHeldKey = key;
            _keyRepeatTimer = 0.35;
            shouldProcess = true;
        }
        else
        {
            _keyRepeatTimer -= gameTime.ElapsedGameTime.TotalSeconds;
            if (_keyRepeatTimer <= 0)
            {
                _keyRepeatTimer = 0.06;
                shouldProcess = true;
            }
        }

        if (!shouldProcess) return;

        if (key == Keys.Back)
        {
            if (_focusedField == 0 && _username.Length > 0)
            {
                _username = _username[..^1];
            }
            else if (_focusedField == 1 && _password.Length > 0)
            {
                _password = _password[..^1];
            }
            else if (_focusedField == 2 && _serverHost.Length > 0)
            {
                _serverHost = _serverHost[..^1];
                _api.SetServerHost(_serverHost);
            }
            return;
        }

        char? ch = KeyToChar(key, keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift));
        if (ch.HasValue)
        {
            if (_focusedField == 0 && _username.Length < 20)
            {
                _username += ch.Value;
            }
            else if (_focusedField == 1 && _password.Length < 25)
            {
                _password += ch.Value;
            }
            else if (_focusedField == 2 && _serverHost.Length < 35)
            {
                _serverHost += ch.Value;
                _api.SetServerHost(_serverHost);
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
        if (key >= Keys.D0 && key <= Keys.D9)
        {
            return (char)('0' + (key - Keys.D0));
        }
        if (key == Keys.Space) return ' ';
        if (key == Keys.OemPeriod) return '.';
        if (key == Keys.OemMinus) return '-';
        return null;
    }

    private async void DoLogin()
    {
        if (string.IsNullOrWhiteSpace(_username))
        {
            _statusMessage = "Please enter a username";
            _statusColor = Color.Red;
            return;
        }

        _api.SetServerHost(_serverHost);
        _statusMessage = $"Connecting to {_serverHost}:5000...";
        _statusColor = Color.Yellow;

        var (success, msg) = await _api.LoginAsync(_username, _password);
        if (success)
        {
            _statusMessage = "Login succeeded! Entering lobby...";
            _statusColor = Color.LimeGreen;
            ScreenManager.ChangeScreen(new LobbyScreen(ScreenManager, _api, _selectedClass));
        }
        else
        {
            _statusMessage = $"Login failed: {msg}";
            _statusColor = Color.IndianRed;
        }
    }

    private async void DoRegister()
    {
        if (string.IsNullOrWhiteSpace(_username) || string.IsNullOrWhiteSpace(_password))
        {
            _statusMessage = "Username and password required";
            _statusColor = Color.Red;
            return;
        }

        _api.SetServerHost(_serverHost);
        _statusMessage = $"Registering at {_serverHost}:5000...";
        _statusColor = Color.Yellow;

        var (success, msg) = await _api.RegisterAsync(_username, _password, _username);
        if (success)
        {
            _statusMessage = "Registration succeeded! Entering lobby...";
            _statusColor = Color.LimeGreen;
            ScreenManager.ChangeScreen(new LobbyScreen(ScreenManager, _api, _selectedClass));
        }
        else
        {
            _statusMessage = $"Registration failed: {msg}";
            _statusColor = Color.IndianRed;
        }
    }

    private void PlayAsGuest()
    {
        _api.SetServerHost(_serverHost);
        _api.DisplayName = string.IsNullOrWhiteSpace(_username) ? "GuestHero" : _username;
        ScreenManager.ChangeScreen(new LobbyScreen(ScreenManager, _api, _selectedClass));
    }

    /// <inheritdoc/>
    public override void Draw(GameTime gameTime)
    {
        SpriteBatch sb = ScreenManager.SpriteBatch;
        sb.Begin();

        // Background
        PrimitiveRenderer.DrawRect(sb, 0, 0, 1280, 720, new Color(20, 22, 28));

        // Title
        PrimitiveRenderer.DrawString(sb, "ARENA STORM", 470, 75, Color.Gold, 5);
        PrimitiveRenderer.DrawString(sb, "MULTIPLAYER 2D ACTION COMBAT", 450, 135, Color.CornflowerBlue, 2);

        // Input Form Card
        PrimitiveRenderer.DrawRect(sb, 400, 175, 480, 400, new Color(30, 34, 42));
        PrimitiveRenderer.DrawRectOutline(sb, 400, 175, 480, 400, Color.DarkSlateGray, 2);

        // Username Field
        PrimitiveRenderer.DrawString(sb, "USERNAME:", 440, 205, Color.LightGray, 2);
        Color userBorder = _focusedField == 0 ? Color.Gold : Color.Gray;
        PrimitiveRenderer.DrawRect(sb, 440, 225, 400, 34, new Color(20, 22, 26));
        PrimitiveRenderer.DrawRectOutline(sb, 440, 225, 400, 34, userBorder, 2);
        string displayUser = _username + (_focusedField == 0 && (int)(gameTime.TotalGameTime.TotalSeconds * 2) % 2 == 0 ? "_" : "");
        PrimitiveRenderer.DrawString(sb, displayUser, 452, 234, Color.White, 2);

        // Password Field
        PrimitiveRenderer.DrawString(sb, "PASSWORD:", 440, 267, Color.LightGray, 2);
        Color passBorder = _focusedField == 1 ? Color.Gold : Color.Gray;
        PrimitiveRenderer.DrawRect(sb, 440, 287, 400, 34, new Color(20, 22, 26));
        PrimitiveRenderer.DrawRectOutline(sb, 440, 287, 400, 34, passBorder, 2);
        string maskedPass = new string('*', _password.Length) + (_focusedField == 1 && (int)(gameTime.TotalGameTime.TotalSeconds * 2) % 2 == 0 ? "_" : "");
        PrimitiveRenderer.DrawString(sb, maskedPass, 452, 296, Color.White, 2);

        // Server Host Field
        PrimitiveRenderer.DrawString(sb, "SERVER IP / HOST:", 440, 329, Color.LightGray, 2);
        Color hostBorder = _focusedField == 2 ? Color.Gold : Color.Gray;
        PrimitiveRenderer.DrawRect(sb, 440, 349, 400, 34, new Color(20, 22, 26));
        PrimitiveRenderer.DrawRectOutline(sb, 440, 349, 400, 34, hostBorder, 2);
        string displayHost = _serverHost + (_focusedField == 2 && (int)(gameTime.TotalGameTime.TotalSeconds * 2) % 2 == 0 ? "_" : "");
        PrimitiveRenderer.DrawString(sb, displayHost, 452, 358, Color.White, 2);

        // Buttons
        // LOGIN Button
        PrimitiveRenderer.DrawRect(sb, 440, 405, 190, 40, new Color(34, 110, 60));
        PrimitiveRenderer.DrawRectOutline(sb, 440, 405, 190, 40, Color.LimeGreen, 2);
        PrimitiveRenderer.DrawString(sb, "LOGIN", 500, 416, Color.White, 2);

        // REGISTER Button
        PrimitiveRenderer.DrawRect(sb, 650, 405, 190, 40, new Color(45, 80, 140));
        PrimitiveRenderer.DrawRectOutline(sb, 650, 405, 190, 40, Color.DeepSkyBlue, 2);
        PrimitiveRenderer.DrawString(sb, "REGISTER", 695, 416, Color.White, 2);

        // PLAY AS GUEST Button
        PrimitiveRenderer.DrawRect(sb, 440, 460, 400, 40, new Color(130, 80, 30));
        PrimitiveRenderer.DrawRectOutline(sb, 440, 460, 400, 40, Color.Goldenrod, 2);
        PrimitiveRenderer.DrawString(sb, "PLAY AS GUEST / QUICK MATCH", 480, 471, Color.White, 2);

        // Status banner
        PrimitiveRenderer.DrawString(sb, _statusMessage, 440, 520, _statusColor, 2);

        // Footer info
        PrimitiveRenderer.DrawString(sb, "USE TAB OR CLICK TO EDIT SERVER IP  -  ENTER TO CONFIRM", 360, 630, Color.Gray, 2);

        sb.End();
    }
}
