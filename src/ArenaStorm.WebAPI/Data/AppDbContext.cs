namespace ArenaStorm.WebAPI.Data;

using ArenaStorm.WebAPI.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Database context for ArenaStorm Web API.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets or sets the Users table.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Gets or sets the PlayerStats table.
    /// </summary>
    public DbSet<PlayerStats> PlayerStats => Set<PlayerStats>();

    /// <summary>
    /// Gets or sets the LeaderboardEntries table.
    /// </summary>
    public DbSet<LeaderboardEntry> LeaderboardEntries => Set<LeaderboardEntry>();

    /// <summary>
    /// Gets or sets the ChatMessages table.
    /// </summary>
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(builder =>
        {
            builder.HasIndex(u => u.Username).IsUnique();
            builder.Property(u => u.Username).HasMaxLength(50).IsRequired();
            builder.Property(u => u.DisplayName).HasMaxLength(50).IsRequired();

            builder.HasOne(u => u.Stats)
                .WithOne(s => s.User)
                .HasForeignKey<PlayerStats>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerStats>(builder =>
        {
            builder.Ignore(s => s.KdRatio);
        });

        modelBuilder.Entity<LeaderboardEntry>(builder =>
        {
            builder.HasIndex(e => new { e.UserId, e.LeaderboardType }).IsUnique();
            builder.Property(e => e.LeaderboardType).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<ChatMessage>(builder =>
        {
            builder.Property(m => m.Content).HasMaxLength(500).IsRequired();
            builder.Property(m => m.LobbyId).HasMaxLength(50).IsRequired();
        });
    }
}
