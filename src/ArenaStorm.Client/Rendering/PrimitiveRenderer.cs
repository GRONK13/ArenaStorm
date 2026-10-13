using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArenaStorm.Client.Rendering;

/// <summary>
/// Provides utility methods for drawing 2D geometric primitives and text using <see cref="SpriteBatch"/>.
/// </summary>
public static class PrimitiveRenderer
{
    private static Texture2D? _pixel;

    /// <summary>
    /// Initializes the primitive renderer by generating a 1x1 white pixel texture.
    /// </summary>
    /// <param name="device">Graphics device to associate the texture with.</param>
    public static void Initialize(GraphicsDevice device)
    {
        if (_pixel != null && !_pixel.IsDisposed)
        {
            return;
        }

        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Draws a solid filled rectangle.
    /// </summary>
    /// <param name="sb">Active sprite batch.</param>
    /// <param name="x">Top-left X coordinate.</param>
    /// <param name="y">Top-left Y coordinate.</param>
    /// <param name="w">Width in pixels.</param>
    /// <param name="h">Height in pixels.</param>
    /// <param name="color">Fill color.</param>
    public static void DrawRect(SpriteBatch sb, float x, float y, float w, float h, Color color)
    {
        if (_pixel == null) return;
        sb.Draw(_pixel, new Vector2(x, y), null, color, 0f, Vector2.Zero, new Vector2(w, h), SpriteEffects.None, 0f);
    }

    /// <summary>
    /// Draws an outlined rectangle with specified border thickness.
    /// </summary>
    /// <param name="sb">Active sprite batch.</param>
    /// <param name="x">Top-left X coordinate.</param>
    /// <param name="y">Top-left Y coordinate.</param>
    /// <param name="w">Width in pixels.</param>
    /// <param name="h">Height in pixels.</param>
    /// <param name="color">Border color.</param>
    /// <param name="thickness">Border line thickness in pixels.</param>
    public static void DrawRectOutline(SpriteBatch sb, float x, float y, float w, float h, Color color, int thickness = 1)
    {
        // Top edge
        DrawRect(sb, x, y, w, thickness, color);
        // Bottom edge
        DrawRect(sb, x, y + h - thickness, w, thickness, color);
        // Left edge
        DrawRect(sb, x, y + thickness, thickness, Math.Max(0, h - (thickness * 2)), color);
        // Right edge
        DrawRect(sb, x + w - thickness, y + thickness, thickness, Math.Max(0, h - (thickness * 2)), color);
    }

    /// <summary>
    /// Draws a straight line between two points.
    /// </summary>
    /// <param name="sb">Active sprite batch.</param>
    /// <param name="start">Origin point.</param>
    /// <param name="end">Destination point.</param>
    /// <param name="color">Line color.</param>
    /// <param name="thickness">Stroke thickness.</param>
    public static void DrawLine(SpriteBatch sb, Vector2 start, Vector2 end, Color color, int thickness = 1)
    {
        if (_pixel == null) return;
        float distance = Vector2.Distance(start, end);
        float angle = MathF.Atan2(end.Y - start.Y, end.X - start.X);

        sb.Draw(
            _pixel,
            start,
            null,
            color,
            angle,
            new Vector2(0f, 0.5f),
            new Vector2(distance, thickness),
            SpriteEffects.None,
            0f);
    }

    /// <summary>
    /// Draws an approximated circle outline using polygon line segments.
    /// </summary>
    /// <param name="sb">Active sprite batch.</param>
    /// <param name="cx">Center X coordinate.</param>
    /// <param name="cy">Center Y coordinate.</param>
    /// <param name="radius">Circle radius.</param>
    /// <param name="color">Stroke color.</param>
    /// <param name="segments">Number of line segments.</param>
    public static void DrawCircle(SpriteBatch sb, float cx, float cy, float radius, Color color, int segments = 16)
    {
        if (segments < 3) segments = 3;
        float step = MathHelper.TwoPi / segments;

        for (int i = 0; i < segments; i++)
        {
            float a1 = i * step;
            float a2 = (i + 1) * step;
            Vector2 p1 = new(cx + (MathF.Cos(a1) * radius), cy + (MathF.Sin(a1) * radius));
            Vector2 p2 = new(cx + (MathF.Cos(a2) * radius), cy + (MathF.Sin(a2) * radius));
            DrawLine(sb, p1, p2, color, 1);
        }
    }

    /// <summary>
    /// Draws a filled circle approximated with horizontal spans.
    /// </summary>
    /// <param name="sb">Active sprite batch.</param>
    /// <param name="cx">Center X coordinate.</param>
    /// <param name="cy">Center Y coordinate.</param>
    /// <param name="radius">Circle radius.</param>
    /// <param name="color">Fill color.</param>
    public static void DrawFilledCircle(SpriteBatch sb, float cx, float cy, float radius, Color color)
    {
        int r = (int)MathF.Ceiling(radius);
        float rSq = radius * radius;
        for (int y = -r; y <= r; y++)
        {
            float halfWidth = MathF.Sqrt(Math.Max(0, rSq - (y * y)));
            DrawRect(sb, cx - halfWidth, cy + y, halfWidth * 2f, 1f, color);
        }
    }

    /// <summary>
    /// Draws a bordered progress/health bar.
    /// </summary>
    /// <param name="sb">Active sprite batch.</param>
    /// <param name="x">Top-left X coordinate.</param>
    /// <param name="y">Top-left Y coordinate.</param>
    /// <param name="width">Bar total width.</param>
    /// <param name="height">Bar total height.</param>
    /// <param name="percentage">Filled fraction between 0.0 and 1.0.</param>
    /// <param name="bgColor">Background color for depleted portion.</param>
    /// <param name="fgColor">Foreground color for filled portion.</param>
    public static void DrawHealthBar(
        SpriteBatch sb,
        float x,
        float y,
        float width,
        float height,
        float percentage,
        Color bgColor,
        Color fgColor)
    {
        float clamped = Math.Clamp(percentage, 0f, 1f);
        // Background
        DrawRect(sb, x, y, width, height, bgColor);
        // Filled portion
        if (clamped > 0f)
        {
            DrawRect(sb, x, y, width * clamped, height, fgColor);
        }
        // Border outline
        DrawRectOutline(sb, x, y, width, height, Color.Black, 1);
    }

    /// <summary>
    /// Draws a string using a built-in 5x7 bitmap font, requiring no external font assets.
    /// </summary>
    /// <param name="sb">Active sprite batch.</param>
    /// <param name="text">The string to draw.</param>
    /// <param name="x">Left position in pixels.</param>
    /// <param name="y">Top position in pixels.</param>
    /// <param name="color">Text color.</param>
    /// <param name="scale">Pixel scaling factor.</param>
    public static void DrawString(SpriteBatch sb, string text, float x, float y, Color color, int scale = 2)
    {
        float cursorX = x;
        foreach (char ch in text)
        {
            byte[] glyph = GetGlyph(ch);
            for (int col = 0; col < 5; col++)
            {
                byte colBits = glyph[col];
                for (int row = 0; row < 7; row++)
                {
                    if ((colBits & (1 << row)) != 0)
                    {
                        DrawRect(sb, cursorX + (col * scale), y + (row * scale), scale, scale, color);
                    }
                }
            }
            cursorX += (5 + 1) * scale;
        }
    }

    private static byte[] GetGlyph(char c)
    {
        return char.ToUpperInvariant(c) switch
        {
            '0' => [0x3E, 0x51, 0x49, 0x45, 0x3E],
            '1' => [0x00, 0x42, 0x7F, 0x40, 0x00],
            '2' => [0x42, 0x61, 0x51, 0x49, 0x46],
            '3' => [0x21, 0x41, 0x45, 0x4B, 0x31],
            '4' => [0x18, 0x14, 0x12, 0x7F, 0x10],
            '5' => [0x27, 0x45, 0x45, 0x45, 0x39],
            '6' => [0x3C, 0x4A, 0x49, 0x49, 0x30],
            '7' => [0x01, 0x71, 0x09, 0x05, 0x03],
            '8' => [0x36, 0x49, 0x49, 0x49, 0x36],
            '9' => [0x06, 0x49, 0x49, 0x29, 0x1E],
            'A' => [0x7E, 0x11, 0x11, 0x11, 0x7E],
            'B' => [0x7F, 0x49, 0x49, 0x49, 0x36],
            'C' => [0x3E, 0x41, 0x41, 0x41, 0x22],
            'D' => [0x7F, 0x41, 0x41, 0x22, 0x1C],
            'E' => [0x7F, 0x49, 0x49, 0x49, 0x41],
            'F' => [0x7F, 0x09, 0x09, 0x09, 0x01],
            'G' => [0x3E, 0x41, 0x49, 0x49, 0x7A],
            'H' => [0x7F, 0x08, 0x08, 0x08, 0x7F],
            'I' => [0x00, 0x41, 0x7F, 0x41, 0x00],
            'J' => [0x20, 0x40, 0x41, 0x3F, 0x01],
            'K' => [0x7F, 0x08, 0x14, 0x22, 0x41],
            'L' => [0x7F, 0x40, 0x40, 0x40, 0x40],
            'M' => [0x7F, 0x02, 0x0C, 0x02, 0x7F],
            'N' => [0x7F, 0x04, 0x08, 0x10, 0x7F],
            'O' => [0x3E, 0x41, 0x41, 0x41, 0x3E],
            'P' => [0x7F, 0x09, 0x09, 0x09, 0x06],
            'Q' => [0x3E, 0x41, 0x51, 0x21, 0x5E],
            'R' => [0x7F, 0x09, 0x19, 0x29, 0x46],
            'S' => [0x46, 0x49, 0x49, 0x49, 0x31],
            'T' => [0x01, 0x01, 0x7F, 0x01, 0x01],
            'U' => [0x3F, 0x40, 0x40, 0x40, 0x3F],
            'V' => [0x1F, 0x20, 0x40, 0x20, 0x1F],
            'W' => [0x7F, 0x20, 0x18, 0x20, 0x7F],
            'X' => [0x63, 0x14, 0x08, 0x14, 0x63],
            'Y' => [0x07, 0x08, 0x70, 0x08, 0x07],
            'Z' => [0x61, 0x51, 0x49, 0x45, 0x43],
            ':' => [0x00, 0x36, 0x36, 0x00, 0x00],
            '.' => [0x00, 0x60, 0x60, 0x00, 0x00],
            '-' => [0x08, 0x08, 0x08, 0x08, 0x08],
            '/' => [0x60, 0x18, 0x06, 0x01, 0x00],
            '|' => [0x00, 0x00, 0x7F, 0x00, 0x00],
            '[' => [0x00, 0x7F, 0x41, 0x41, 0x00],
            ']' => [0x00, 0x41, 0x41, 0x7F, 0x00],
            '(' => [0x00, 0x1C, 0x22, 0x41, 0x00],
            ')' => [0x00, 0x41, 0x22, 0x1C, 0x00],
            '+' => [0x08, 0x08, 0x3E, 0x08, 0x08],
            '=' => [0x14, 0x14, 0x14, 0x14, 0x14],
            '%' => [0x23, 0x13, 0x08, 0x64, 0x62],
            ' ' => [0x00, 0x00, 0x00, 0x00, 0x00],
            _ => [0x7F, 0x7F, 0x7F, 0x7F, 0x7F]
        };
    }
}
