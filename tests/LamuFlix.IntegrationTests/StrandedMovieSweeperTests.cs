using System;
using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class StrandedMovieSweeperTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task FindStrandedMovieIdsAsync_SelectsOnlyNullOrStrictlyExpiredPendingRowsWithoutMutation()
    {
        var now = MovieCatalogSeed.FixedTime;
        var cutoff = now - TimeSpan.FromMinutes(5);
        await using (var context = fixture.CreateMigratedContext())
        {
            var strandedNull = MovieCatalogSeed.Create("null attempt", "sweeper-null");
            var strandedExpired = MovieCatalogSeed.Create("expired", "sweeper-expired");
            strandedExpired.LastAttemptAt = cutoff.AddTicks(-1);
            var fresh = MovieCatalogSeed.Create("fresh", "sweeper-fresh");
            fresh.LastAttemptAt = cutoff.AddTicks(1);
            var boundary = MovieCatalogSeed.Create("boundary", "sweeper-boundary");
            boundary.LastAttemptAt = cutoff;
            var failed = MovieCatalogSeed.Create("failed", "sweeper-failed");
            failed.Status = EnrichmentStatus.Failed;
            var enriched = MovieCatalogSeed.Create("enriched", "sweeper-enriched");
            enriched.Status = EnrichmentStatus.Enriched;
            enriched.LastAttemptAt = cutoff.AddTicks(-1);
            context.Movies.AddRange(strandedNull, strandedExpired, fresh, boundary, failed, enriched);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var expected = new[] { strandedNull.Id!, strandedExpired.Id! };
            var originalAttempts = context.Movies.AsNoTracking().ToDictionary(movie => movie.Id!, movie => movie.EnrichmentAttempts);
            var originalTimes = context.Movies.AsNoTracking().ToDictionary(movie => movie.Id!, movie => movie.LastAttemptAt);
            var originalStatuses = context.Movies.AsNoTracking().ToDictionary(movie => movie.Id!, movie => movie.Status);
            var repository = new EfMovieRepository(
                context,
                new FixedTimeProvider(now),
                Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }));

            var actual = await repository.FindStrandedMovieIdsAsync(cutoff, TestContext.Current.CancellationToken);

            Assert.Equal(expected.OrderBy(id => id.Value), actual.OrderBy(id => id.Value));
            var persisted = await context.Movies.AsNoTracking().ToArrayAsync(TestContext.Current.CancellationToken);
            Assert.All(persisted, movie => Assert.Equal(originalAttempts[movie.Id!], movie.EnrichmentAttempts));
            Assert.All(persisted, movie => Assert.Equal(originalTimes[movie.Id!], movie.LastAttemptAt));
            Assert.All(persisted, movie => Assert.Equal(originalStatuses[movie.Id!], movie.Status));
        }
    }
}