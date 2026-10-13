namespace ArenaStorm.WebAPI.Services;

using ArenaStorm.WebAPI.Data;
using ArenaStorm.WebAPI.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Data transfer object for leaderboard rankings.
/// </summary>
/// <param name="Rank">The ordinal rank of the player (1-based).</param>
/// <param name="PlayerId">The unique user identifier.</param>
/// <param name="DisplayName">The player's display name.</param>
/// <param name="Value">The score or metric value.</param>
public record LeaderboardEntryDto(int Rank, Guid PlayerId, string DisplayName, int Value);

/// <summary>
/// Service managing player stats and competitive leaderboards.
/// </summary>
public class LeaderboardService(AppDbContext dbContext)
{
    /// <summary>
    /// Retrieves a paginated list of ranked players for a given leaderboard category.
    /// </summary>
    /// <param name="type">Leaderboard category ("kills", "wins", "kd").</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Number of entries per page.</param>
    /// <returns>List of leaderboard entries.</returns>
    public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(string type, int page, int pageSize)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var normalizedType = type.Trim().ToLowerInvariant();

        var entries = await dbContext.LeaderboardEntries
            .AsNoTracking()
            .Include(e => e.User)
            .Where(e => e.LeaderboardType.ToLower() == normalizedType)
            .OrderByDescending(e => e.Value)
            .ThenBy(e => e.UpdatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync();

        var result = new List<LeaderboardEntryDto>(entries.Count);
        for (var i = 0; i < entries.Count; i++)
        {
            var rank = (safePage - 1) * safePageSize + i + 1;
            result.Add(new LeaderboardEntryDto(
                Rank: rank,
                PlayerId: entries[i].UserId,
                DisplayName: entries[i].User?.DisplayName ?? "Unknown",
                Value: entries[i].Value));
        }

        return result;
    }

    /// <summary>
    /// Retrieves a specific player's ranking and value for the given category.
    /// </summary>
    /// <param name="type">Leaderboard category ("kills", "wins", "kd").</param>
    /// <param name="userId">Player's unique user identifier.</param>
    /// <returns>Player leaderboard entry or null if not found.</returns>
    public async Task<LeaderboardEntryDto?> GetPlayerRankAsync(string type, Guid userId)
    {
        var normalizedType = type.Trim().ToLowerInvariant();
        var entry = await dbContext.LeaderboardEntries
            .AsNoTracking()
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.LeaderboardType.ToLower() == normalizedType);

        if (entry == null)
        {
            return null;
        }

        var higherRanked = await dbContext.LeaderboardEntries
            .CountAsync(e => e.LeaderboardType.ToLower() == normalizedType && e.Value > entry.Value);

        return new LeaderboardEntryDto(
            Rank: higherRanked + 1,
            PlayerId: entry.UserId,
            DisplayName: entry.User?.DisplayName ?? "Unknown",
            Value: entry.Value);
    }

    /// <summary>
    /// Updates player gameplay stats and recalculates leaderboard entries.
    /// </summary>
    /// <param name="userId">Player's unique user identifier.</param>
    /// <param name="kills">Kills obtained in the match.</param>
    /// <param name="deaths">Deaths suffered in the match.</param>
    /// <param name="won">Whether the player won the match.</param>
    public async Task UpdateStatsAsync(Guid userId, int kills, int deaths, bool won)
    {
        var stats = await dbContext.PlayerStats.FirstOrDefaultAsync(s => s.UserId == userId);
        if (stats == null)
        {
            stats = new PlayerStats
            {
                UserId = userId,
                TotalKills = kills,
                TotalDeaths = deaths,
                GamesPlayed = 1,
                GamesWon = won ? 1 : 0,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.PlayerStats.Add(stats);
        }
        else
        {
            stats.TotalKills += kills;
            stats.TotalDeaths += deaths;
            stats.GamesPlayed += 1;
            if (won)
            {
                stats.GamesWon += 1;
            }
            stats.UpdatedAt = DateTime.UtcNow;
        }

        await UpsertEntryAsync(userId, "kills", stats.TotalKills);
        await UpsertEntryAsync(userId, "wins", stats.GamesWon);
        await UpsertEntryAsync(userId, "kd", (int)Math.Round(stats.KdRatio * 100));

        await dbContext.SaveChangesAsync();
    }

    private async Task UpsertEntryAsync(Guid userId, string type, int value)
    {
        var entry = await dbContext.LeaderboardEntries
            .FirstOrDefaultAsync(e => e.UserId == userId && e.LeaderboardType == type);

        if (entry == null)
        {
            dbContext.LeaderboardEntries.Add(new LeaderboardEntry
            {
                UserId = userId,
                LeaderboardType = type,
                Value = value,
                Rank = 0,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            entry.Value = value;
            entry.UpdatedAt = DateTime.UtcNow;
        }
    }
}
