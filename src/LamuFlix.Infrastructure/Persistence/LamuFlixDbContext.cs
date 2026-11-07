using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;

namespace LamuFlix.Infrastructure.Persistence;

public sealed class LamuFlixDbContext(DbContextOptions<LamuFlixDbContext> options) : DbContext(options)
{
    public DbSet<MovieRecord> Movies => Set<MovieRecord>();

    public DbSet<ActorRecord> Actors => Set<ActorRecord>();

    public DbSet<DirectorRecord> Directors => Set<DirectorRecord>();

    public DbSet<GenreRecord> Genres => Set<GenreRecord>();

    public DbSet<WriterRecord> Writers => Set<WriterRecord>();

    public DbSet<CollectionRecord> Collections => Set<CollectionRecord>();

    public DbSet<KeywordRecord> Keywords => Set<KeywordRecord>();

    public DbSet<PlaybackEventRecord> PlaybackEvents => Set<PlaybackEventRecord>();

    public DbSet<MovieFeaturesRecord> MovieFeatures => Set<MovieFeaturesRecord>();

    public DbSet<OutboxMessageRecord> OutboxMessages => Set<OutboxMessageRecord>();

    public DbSet<DailyRequestUsageRecord> DailyRequestUsages => Set<DailyRequestUsageRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LamuFlixDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        DeriveLibraryPathKeys();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        DeriveLibraryPathKeys();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DeriveLibraryPathKeys();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        DeriveLibraryPathKeys();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void DeriveLibraryPathKeys()
    {
        foreach (var entry in ChangeTracker.Entries<MovieRecord>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.LibraryPathKey = entry.Entity.LibraryPath.Value.ToUpperInvariant();
            }
        }
    }
}
