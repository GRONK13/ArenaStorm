using Microsoft.Xna.Framework.Input;

namespace ArenaStorm.Client.Input;

/// <summary>
/// Captures and queries keyboard and mouse input states across frames.
/// </summary>
public class InputManager
{
    private KeyboardState _currentKeyboard;
    private KeyboardState _previousKeyboard;
    private MouseState _currentMouse;
    private MouseState _previousMouse;

    /// <summary>
    /// Updates previous and current input states from MonoGame peripherals.
    /// </summary>
    public void Update()
    {
        _previousKeyboard = _currentKeyboard;
        _currentKeyboard = Keyboard.GetState();
        _previousMouse = _currentMouse;
        _currentMouse = Mouse.GetState();
    }

    /// <summary>
    /// Gets a value indicating whether the specified key is currently held down.
    /// </summary>
    /// <param name="key">The key to inspect.</param>
    /// <returns><c>true</c> if held; otherwise <c>false</c>.</returns>
    public bool IsKeyDown(Keys key) => _currentKeyboard.IsKeyDown(key);

    /// <summary>
    /// Gets a value indicating whether the specified key was pressed during the current frame.
    /// </summary>
    /// <param name="key">The key to inspect.</param>
    /// <returns><c>true</c> if just pressed; otherwise <c>false</c>.</returns>
    public bool IsKeyPressed(Keys key) => _currentKeyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);

    /// <summary>
    /// Gets a value indicating whether the specified key was released during the current frame.
    /// </summary>
    /// <param name="key">The key to inspect.</param>
    /// <returns><c>true</c> if just released; otherwise <c>false</c>.</returns>
    public bool IsKeyReleased(Keys key) => !_currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyDown(key);

    /// <summary>Gets the current mouse X coordinate in window space.</summary>
    public int MouseX => _currentMouse.X;

    /// <summary>Gets the current mouse Y coordinate in window space.</summary>
    public int MouseY => _currentMouse.Y;

    /// <summary>Gets a value indicating whether the left mouse button is held down.</summary>
    public bool IsLeftMouseDown => _currentMouse.LeftButton == ButtonState.Pressed;

    /// <summary>Gets a value indicating whether the left mouse button was clicked during this frame.</summary>
    public bool IsLeftMouseClicked => _currentMouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;

    /// <summary>Gets a value indicating whether the right mouse button is held down.</summary>
    public bool IsRightMouseDown => _currentMouse.RightButton == ButtonState.Pressed;

    /// <summary>Gets a value indicating whether the right mouse button was clicked during this frame.</summary>
    public bool IsRightMouseClicked => _currentMouse.RightButton == ButtonState.Pressed && _previousMouse.RightButton == ButtonState.Released;
}
