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

<<<<<<< HEAD
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
=======
    public DbSet<PlaybackEventRecord> PlaybackEvents => Set<PlaybackEventRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
>>>>>>> 036b565 (DEV-351 - Phase 2: Persistence record, adapter, and composition)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LamuFlixDbContext).Assembly);
    }
}
