using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;

namespace LamuFlix.IntegrationTests;

internal static class MovieCatalogSeed
{
    internal static readonly DateTimeOffset FixedTime = new(2026, 9, 30, 0, 0, 0, TimeSpan.Zero);

    internal static MovieRecord Create(string title, string pathSuffix) =>
        new()
        {
            Title = title,
            LibraryPath = new LibraryPath($"C:/library/{pathSuffix}.mkv"),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Pending
        };

    internal static async Task<MovieRecord> AddAsync(LamuFlixDbContext context, MovieRecord movie, CancellationToken ct)
    {
        context.Movies.Add(movie);
        await context.SaveChangesAsync(ct);
        return movie;
    }
}