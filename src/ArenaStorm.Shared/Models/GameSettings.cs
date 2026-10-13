namespace ArenaStorm.Shared.Models;

/// <summary>
/// Supported match gameplay modes.
/// </summary>
public enum GameMode : byte
{
    /// <summary>Free-for-all deathmatch where players compete for top kills.</summary>
    PvpDeathmatch = 0,

    /// <summary>Round-based elimination combat.</summary>
    PvpElimination = 1,

    /// <summary>Cooperative defense against survival waves.</summary>
    CoopSurvival = 2
}

/// <summary>
/// Configuration parameters governing a game match instance.
/// </summary>
/// <param name="MapId">Identifier of the arena map layout.</param>
/// <param name="Mode">Match gameplay mode.</param>
/// <param name="MaxPlayers">Maximum number of concurrent participants.</param>
/// <param name="TickRate">Simulation frequency in updates per second.</param>
/// <param name="RespawnTime">Delay in seconds before eliminated players respawn.</param>
/// <param name="ScoreLimit">Score threshold required to win the match.</param>
/// <param name="MatchDuration">Maximum match length in seconds.</param>
public record GameSettings(
    string MapId,
    GameMode Mode,
    int MaxPlayers,
    int TickRate = 30,
    float RespawnTime = 3.0f,
    int ScoreLimit = 20,
    float MatchDuration = 300f);
