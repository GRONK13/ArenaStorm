using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArenaStorm.Client.Rendering;

/// <summary>
/// 2D orthographic tracking camera supporting view translation, zoom scaling, and screen-to-world conversion.
/// </summary>
public class Camera
{
    /// <summary>
    /// Gets or sets the center world focus position of the camera.
    /// </summary>
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the camera zoom factor (defaults to 1.0f).
    /// </summary>
    public float Zoom { get; set; } = 1.0f;

    /// <summary>
    /// Computes the camera transformation matrix for rendering with <see cref="SpriteBatch"/>.
    /// </summary>
    /// <param name="device">Graphics device supplying viewport dimensions.</param>
    /// <returns>Transformation matrix mapping world space to centered screen space.</returns>
    public Matrix GetTransformMatrix(GraphicsDevice device)
    {
        return Matrix.CreateTranslation(-Position.X, -Position.Y, 0f) *
               Matrix.CreateScale(Zoom, Zoom, 1f) *
               Matrix.CreateTranslation(device.Viewport.Width / 2f, device.Viewport.Height / 2f, 0f);
    }

    /// <summary>
    /// Smoothly interpolates the camera focus towards the target position.
    /// </summary>
    /// <param name="targetX">Target X world coordinate.</param>
    /// <param name="targetY">Target Y world coordinate.</param>
    /// <param name="lerp">Linear interpolation rate between 0 and 1.</param>
    public void Follow(float targetX, float targetY, float lerp = 0.1f)
    {
        Position = Vector2.Lerp(Position, new Vector2(targetX, targetY), lerp);
    }

    /// <summary>
    /// Converts a 2D screen-space coordinate into the corresponding world-space position.
    /// </summary>
    /// <param name="screenPosition">Position in screen pixel coordinates.</param>
    /// <param name="device">Graphics device supplying viewport dimensions.</param>
    /// <returns>World coordinate position.</returns>
    public Vector2 ScreenToWorld(Vector2 screenPosition, GraphicsDevice device)
    {
        Matrix transform = GetTransformMatrix(device);
        Matrix inverted = Matrix.Invert(transform);
        return Vector2.Transform(screenPosition, inverted);
    }
}
