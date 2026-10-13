using ArenaStorm.Shared.Constants;

namespace ArenaStorm.Server.Game;

/// <summary>
/// Manages player spawn locations across the arena map.
/// </summary>
public class SpawnManager
{
    private readonly Random _random = new();

    /// <summary>
    /// Gets the predefined spawn point coordinates for the active map.
    /// </summary>
    public List<(float X, float Y)> SpawnPoints { get; } = [];

    /// <summary>
    /// Initializes spawn locations for the specified map identifier.
    /// </summary>
    /// <param name="mapId">Map identifier to configure spawns for.</param>
    public void Initialize(string mapId)
    {
        SpawnPoints.Clear();

        // 10 predefined spawn points well-distributed across the 30x20 arena
        // Avoiding obstacles and boundary walls
        SpawnPoints.Add((3 * GameConstants.TileSize, 3 * GameConstants.TileSize));       // Top-Left
        SpawnPoints.Add((15 * GameConstants.TileSize, 2 * GameConstants.TileSize));      // Top-Center
        SpawnPoints.Add((26 * GameConstants.TileSize, 3 * GameConstants.TileSize));      // Top-Right

        SpawnPoints.Add((3 * GameConstants.TileSize, 10 * GameConstants.TileSize));      // Mid-Left
        SpawnPoints.Add((10 * GameConstants.TileSize, 9 * GameConstants.TileSize));      // Mid Inner-Left
        SpawnPoints.Add((19 * GameConstants.TileSize, 9 * GameConstants.TileSize));      // Mid Inner-Right
        SpawnPoints.Add((26 * GameConstants.TileSize, 10 * GameConstants.TileSize));     // Mid-Right

        SpawnPoints.Add((3 * GameConstants.TileSize, 16 * GameConstants.TileSize));      // Bottom-Left
        SpawnPoints.Add((15 * GameConstants.TileSize, 17 * GameConstants.TileSize));     // Bottom-Center
        SpawnPoints.Add((26 * GameConstants.TileSize, 16 * GameConstants.TileSize));     // Bottom-Right
    }

    /// <summary>
    /// Selects a random spawn point from the configured spawn locations.
    /// </summary>
    /// <returns>A tuple containing the world (X, Y) coordinates of the chosen spawn point.</returns>
    public (float X, float Y) GetRandomSpawnPoint()
    {
        if (SpawnPoints.Count == 0)
        {
            return (15 * GameConstants.TileSize, 10 * GameConstants.TileSize);
        }

        int index = _random.Next(SpawnPoints.Count);
        return SpawnPoints[index];
    }
}
