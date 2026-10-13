using ArenaStorm.Shared.Constants;
using ArenaStorm.Shared.Models;
using ArenaStorm.Shared.Utils;

namespace ArenaStorm.Server.Game;

/// <summary>
/// Provides authoritative collision detection, spatial queries, and movement clamping for the arena.
/// </summary>
public class PhysicsEngine
{
    private const float ProjectileCollisionRadius = 4f;
    private const float PlayerCollisionRadius = 14f;

    /// <summary>Gets the shared arena map instance.</summary>
    public ArenaMap Map { get; private set; } = new();

    /// <summary>
    /// Gets the width of the tile grid.
    /// </summary>
    public int MapWidth => Map.Width;

    /// <summary>
    /// Gets the height of the tile grid.
    /// </summary>
    public int MapHeight => Map.Height;

    /// <summary>
    /// Gets the 2D grid of map tiles (0 = empty/walkable, 1 = solid wall).
    /// </summary>
    public int[,] TileMap => Map.Tiles;

    /// <summary>
    /// Initializes and loads the tile map layout for the specified map identifier.
    /// </summary>
    /// <param name="mapId">Map identifier to load.</param>
    public void LoadMap(string mapId)
    {
        Map = new ArenaMap();
    }

    /// <summary>
    /// Checks whether the specified world coordinate is within bounds and on an open tile.
    /// </summary>
    /// <param name="x">World X coordinate.</param>
    /// <param name="y">World Y coordinate.</param>
    /// <returns><c>true</c> if the coordinate is walkable; otherwise, <c>false</c>.</returns>
    public bool IsWalkable(float x, float y)
    {
        int tileX = (int)MathF.Floor(x / GameConstants.TileSize);
        int tileY = (int)MathF.Floor(y / GameConstants.TileSize);

        if (tileX < 0 || tileX >= MapWidth || tileY < 0 || tileY >= MapHeight)
        {
            return false;
        }

        return TileMap[tileX, tileY] == 0;
    }

    /// <summary>
    /// Checks if a circle of given radius collides with any solid wall tiles.
    /// </summary>
    /// <param name="x">Circle center X.</param>
    /// <param name="y">Circle center Y.</param>
    /// <param name="radius">Collision radius.</param>
    /// <returns><c>true</c> if intersecting a wall; otherwise, <c>false</c>.</returns>
    public bool CheckWallCollision(float x, float y, float radius)
    {
        int minTileX = (int)MathF.Floor((x - radius) / GameConstants.TileSize);
        int maxTileX = (int)MathF.Floor((x + radius) / GameConstants.TileSize);
        int minTileY = (int)MathF.Floor((y - radius) / GameConstants.TileSize);
        int maxTileY = (int)MathF.Floor((y + radius) / GameConstants.TileSize);

        for (int tx = minTileX; tx <= maxTileX; tx++)
        {
            for (int ty = minTileY; ty <= maxTileY; ty++)
            {
                if (tx < 0 || tx >= MapWidth || ty < 0 || ty >= MapHeight)
                {
                    return true;
                }

                if (TileMap[tx, ty] != 0)
                {
                    float rx = tx * GameConstants.TileSize;
                    float ry = ty * GameConstants.TileSize;
                    if (CollisionHelper.CheckCircleRect(x, y, radius, rx, ry, GameConstants.TileSize, GameConstants.TileSize))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Tests for projectile intersection with active enemy players.
    /// </summary>
    /// <param name="projectile">The projectile being simulated.</param>
    /// <param name="players">Current room players dictionary.</param>
    /// <param name="hitPlayerId">Outputs the ID of the struck player, if any.</param>
    /// <returns><c>true</c> if an opponent was hit; otherwise, <c>false</c>.</returns>
    public bool CheckProjectileCollision(ProjectileState projectile, Dictionary<byte, PlayerState> players, out byte hitPlayerId)
    {
        hitPlayerId = 0;

        foreach (var (id, player) in players)
        {
            if (id == projectile.OwnerId || !player.IsAlive)
            {
                continue;
            }

            if (CollisionHelper.CheckCircleCollision(projectile.X, projectile.Y, ProjectileCollisionRadius, player.X, player.Y, PlayerCollisionRadius))
            {
                hitPlayerId = id;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Clamps an entity position to walkable bounds, sliding along walls if colliding.
    /// </summary>
    /// <param name="x">Target world X coordinate.</param>
    /// <param name="y">Target world Y coordinate.</param>
    /// <param name="radius">Entity bounding radius.</param>
    /// <returns>Adjusted non-penetrating (x, y) coordinates.</returns>
    public (float x, float y) ClampToWalkable(float x, float y, float radius)
    {
        float minBoundX = GameConstants.TileSize + radius;
        float maxBoundX = ((MapWidth - 1) * GameConstants.TileSize) - radius;
        float minBoundY = GameConstants.TileSize + radius;
        float maxBoundY = ((MapHeight - 1) * GameConstants.TileSize) - radius;

        x = Math.Clamp(x, minBoundX, maxBoundX);
        y = Math.Clamp(y, minBoundY, maxBoundY);

        if (!CheckWallCollision(x, y, radius))
        {
            return (x, y);
        }

        int minTileX = Math.Max(0, (int)MathF.Floor((x - radius) / GameConstants.TileSize));
        int maxTileX = Math.Min(MapWidth - 1, (int)MathF.Floor((x + radius) / GameConstants.TileSize));
        int minTileY = Math.Max(0, (int)MathF.Floor((y - radius) / GameConstants.TileSize));
        int maxTileY = Math.Min(MapHeight - 1, (int)MathF.Floor((y + radius) / GameConstants.TileSize));

        for (int tx = minTileX; tx <= maxTileX; tx++)
        {
            for (int ty = minTileY; ty <= maxTileY; ty++)
            {
                if (TileMap[tx, ty] != 0)
                {
                    float rx = tx * GameConstants.TileSize;
                    float ry = ty * GameConstants.TileSize;
                    float rw = GameConstants.TileSize;
                    float rh = GameConstants.TileSize;

                    if (CollisionHelper.CheckCircleRect(x, y, radius, rx, ry, rw, rh))
                    {
                        float tileCenterX = rx + (rw * 0.5f);
                        float tileCenterY = ry + (rh * 0.5f);
                        float dx = x - tileCenterX;
                        float dy = y - tileCenterY;

                        if (MathF.Abs(dx) > MathF.Abs(dy))
                        {
                            x = dx > 0 ? rx + rw + radius : rx - radius;
                        }
                        else
                        {
                            y = dy > 0 ? ry + rh + radius : ry - radius;
                        }
                    }
                }
            }
        }

        x = Math.Clamp(x, minBoundX, maxBoundX);
        y = Math.Clamp(y, minBoundY, maxBoundY);
        return (x, y);
    }
}
