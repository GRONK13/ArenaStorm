namespace ArenaStorm.Shared.Models;

/// <summary>
/// Authoritative representation of an arena map layout shared between server and client.
/// </summary>
public class ArenaMap
{
    /// <summary>Default map horizontal tile count.</summary>
    public const int DefaultWidth = 30;

    /// <summary>Default map vertical tile count.</summary>
    public const int DefaultHeight = 20;

    /// <summary>Gets the tile grid width.</summary>
    public int Width { get; }

    /// <summary>Gets the tile grid height.</summary>
    public int Height { get; }

    /// <summary>Gets the 2D grid matrix of tiles (0 = floor, 1 = solid wall).</summary>
    public int[,] Tiles { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArenaMap"/> class.
    /// </summary>
    /// <param name="width">Map tile width.</param>
    /// <param name="height">Map tile height.</param>
    public ArenaMap(int width = DefaultWidth, int height = DefaultHeight)
    {
        Width = width;
        Height = height;
        Tiles = new int[width, height];
        BuildDefaultMap();
    }

    private void BuildDefaultMap()
    {
        // Perimeter border boundary walls
        for (int x = 0; x < Width; x++)
        {
            Tiles[x, 0] = 1;
            Tiles[x, Height - 1] = 1;
        }

        for (int y = 0; y < Height; y++)
        {
            Tiles[0, y] = 1;
            Tiles[Width - 1, y] = 1;
        }

        // Corner cover pillars (2x2 tiles)
        SetBlock(6, 4, 2, 2, 1);
        SetBlock(22, 4, 2, 2, 1);
        SetBlock(6, 14, 2, 2, 1);
        SetBlock(22, 14, 2, 2, 1);

        // Center arena obstacle (4x2 tiles)
        SetBlock(13, 9, 4, 2, 1);

        // Mid-lane tactical cover barriers (2x2 tiles)
        SetBlock(14, 3, 2, 2, 1);
        SetBlock(14, 15, 2, 2, 1);
    }

    private void SetBlock(int startX, int startY, int w, int h, int tileType)
    {
        for (int x = startX; x < startX + w && x < Width; x++)
        {
            for (int y = startY; y < startY + h && y < Height; y++)
            {
                Tiles[x, y] = tileType;
            }
        }
    }

    /// <summary>
    /// Checks whether the specified tile coordinate is solid/impassable.
    /// </summary>
    /// <param name="tileX">Tile column index.</param>
    /// <param name="tileY">Tile row index.</param>
    /// <returns><c>true</c> if out of bounds or solid; otherwise, <c>false</c>.</returns>
    public bool IsSolid(int tileX, int tileY)
    {
        if (tileX < 0 || tileX >= Width || tileY < 0 || tileY >= Height)
        {
            return true;
        }

        return Tiles[tileX, tileY] != 0;
    }
}
