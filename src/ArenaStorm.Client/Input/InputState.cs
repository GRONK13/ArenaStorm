using Microsoft.Xna.Framework.Input;
using ArenaStorm.Shared.Protocol;

namespace ArenaStorm.Client.Input;

/// <summary>
/// Provides helper functions converting raw peripheral inputs into game protocol representations.
/// </summary>
public static class InputState
{
    /// <summary>
    /// Evaluates current keyboard and mouse states into a bitfield of input action flags.
    /// </summary>
    /// <param name="input">The active input manager.</param>
    /// <returns>A byte bitmask containing active <see cref="InputFlags"/>.</returns>
    public static byte GetInputFlags(InputManager input)
    {
        byte flags = 0;
        if (input.IsKeyDown(Keys.W) || input.IsKeyDown(Keys.Up)) flags |= InputFlags.Up;
        if (input.IsKeyDown(Keys.S) || input.IsKeyDown(Keys.Down)) flags |= InputFlags.Down;
        if (input.IsKeyDown(Keys.A) || input.IsKeyDown(Keys.Left)) flags |= InputFlags.Left;
        if (input.IsKeyDown(Keys.D) || input.IsKeyDown(Keys.Right)) flags |= InputFlags.Right;
        if (input.IsLeftMouseDown) flags |= InputFlags.Fire;
        if (input.IsRightMouseDown) flags |= InputFlags.Melee;
        return flags;
    }

    /// <summary>
    /// Computes the aiming angle in radians from a given position toward current mouse coordinates.
    /// </summary>
    /// <param name="input">The active input manager.</param>
    /// <param name="playerX">Origin entity X coordinate.</param>
    /// <param name="playerY">Origin entity Y coordinate.</param>
    /// <returns>Aim direction in radians.</returns>
    public static float GetAimAngle(InputManager input, float playerX, float playerY)
    {
        float dx = input.MouseX - playerX;
        float dy = input.MouseY - playerY;
        return MathF.Atan2(dy, dx);
    }
}
