using Microsoft.Xna.Framework;

namespace ArenaStorm.Client.Screens;

/// <summary>
/// Abstract base class for all game screens in ArenaStorm.
/// </summary>
public abstract class Screen
{
    /// <summary>
    /// Gets the ScreenManager controlling transitions and global services.
    /// </summary>
    protected ScreenManager ScreenManager { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Screen"/> class.
    /// </summary>
    /// <param name="screenManager">The managing ScreenManager.</param>
    protected Screen(ScreenManager screenManager) => ScreenManager = screenManager;

    /// <summary>
    /// Loads screen assets and establishes connections.
    /// </summary>
    public virtual void LoadContent() { }

    /// <summary>
    /// Updates screen logic and processes inputs.
    /// </summary>
    /// <param name="gameTime">Provides snapshot of timing values.</param>
    public abstract void Update(GameTime gameTime);

    /// <summary>
    /// Draws visual scene elements.
    /// </summary>
    /// <param name="gameTime">Provides snapshot of timing values.</param>
    public abstract void Draw(GameTime gameTime);

    /// <summary>
    /// Cleans up resources when exiting this screen.
    /// </summary>
    public virtual void UnloadContent() { }
}
