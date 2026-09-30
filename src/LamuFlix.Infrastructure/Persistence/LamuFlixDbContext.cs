using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;

namespace LamuFlix.Infrastructure.Persistence;

public sealed class LamuFlixDbContext(DbContextOptions<LamuFlixDbContext> options) : DbContext(options)
{
    public DbSet<MovieRecord> Movies => Set<MovieRecord>();

    public DbSet<ActorRecord> Actors => Set<ActorRecord>();

    public DbSet<DirectorRecord> Directors => Set<DirectorRecord>();

    public DbSet<GenreRecord> Genres => Set<GenreRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LamuFlixDbContext).Assembly);
}
