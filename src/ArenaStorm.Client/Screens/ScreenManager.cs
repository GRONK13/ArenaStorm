using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ArenaStorm.Client.Input;

namespace ArenaStorm.Client.Screens;

/// <summary>
/// Manages active screen lifecycle, transitions, rendering delegates, and shared game resources.
/// </summary>
/// <param name="game">Root MonoGame instance.</param>
/// <param name="spriteBatch">Shared SpriteBatch for 2D graphics rendering.</param>
/// <param name="inputManager">Shared input peripheral tracker.</param>
public class ScreenManager(Game game, SpriteBatch spriteBatch, InputManager inputManager)
{
    /// <summary>Gets the root MonoGame application instance.</summary>
    public Game Game { get; } = game;

    /// <summary>Gets the shared SpriteBatch instance.</summary>
    public SpriteBatch SpriteBatch { get; } = spriteBatch;

    /// <summary>Gets the shared input manager.</summary>
    public InputManager InputManager { get; } = inputManager;

    /// <summary>Gets the currently displayed screen.</summary>
    public Screen? CurrentScreen { get; private set; }

    /// <summary>
    /// Transitions from the current screen to a new screen, invoking lifecycle hooks.
    /// </summary>
    /// <param name="newScreen">The destination screen instance.</param>
    public void ChangeScreen(Screen newScreen)
    {
        CurrentScreen?.UnloadContent();
        CurrentScreen = newScreen;
        CurrentScreen.LoadContent();
    }

    /// <summary>
    /// Updates the currently active screen.
    /// </summary>
    /// <param name="gameTime">Provides snapshot of timing values.</param>
    public void Update(GameTime gameTime)
    {
        CurrentScreen?.Update(gameTime);
    }

    /// <summary>
    /// Draws the currently active screen.
    /// </summary>
    /// <param name="gameTime">Provides snapshot of timing values.</param>
    public void Draw(GameTime gameTime)
    {
        CurrentScreen?.Draw(gameTime);
    }
}
