using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ArenaStorm.Client.Input;
using ArenaStorm.Client.Screens;

namespace ArenaStorm.Client;

/// <summary>
/// Main MonoGame application entry point managing game loop, graphics device, and screen pipeline.
/// </summary>
public class ArenaStormGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private ScreenManager? _screenManager;
    private InputManager? _inputManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArenaStormGame"/> class.
    /// </summary>
    public ArenaStormGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0); // 60 FPS
    }

    /// <inheritdoc/>
    protected override void Initialize()
    {
        Window.Title = "Arena Storm";
        _inputManager = new InputManager();
        base.Initialize();
    }

    /// <inheritdoc/>
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _screenManager = new ScreenManager(this, _spriteBatch, _inputManager!);
        _screenManager.ChangeScreen(new LoginScreen(_screenManager));
    }

    /// <inheritdoc/>
    protected override void Update(GameTime gameTime)
    {
        _inputManager?.Update();
        _screenManager?.Update(gameTime);
        base.Update(gameTime);
    }

    /// <inheritdoc/>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _screenManager?.Draw(gameTime);
        base.Draw(gameTime);
    }
}
