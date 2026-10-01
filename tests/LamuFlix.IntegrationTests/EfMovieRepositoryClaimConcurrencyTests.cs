using System;
using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class EfMovieRepositoryClaimConcurrencyTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private const int ClaimIterations = 50;
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TryClaim_ConcurrentWorkersHaveOneWinnerAndOnePredicateUpdateEachIteration()
    {
        await using var seed = fixture.CreateContext();
        await seed.Database.MigrateAsync(TestContext.Current.CancellationToken);
        var connectionString = seed.Database.GetConnectionString();
        connectionString.ShouldNotBeNullOrWhiteSpace();
        var capture = new ClaimCommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(connectionString)
            .AddInterceptors(capture)
            .Options;

        for (var iteration = 0; iteration < ClaimIterations; iteration++)
        {
            var repository = Repository(seed);
            var id = await repository.NextIdentityAsync(TestContext.Current.CancellationToken);
            seed.Movies.Add(new MovieRecord
            {
                Id = id,
                Title = $"Claim {iteration}",
                LibraryPath = new LibraryPath($"C:/library/claim-{iteration}.mkv"),
                Format = new MediaFormat("mkv"),
                Status = EnrichmentStatus.Pending,
            });
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
            seed.ChangeTracker.Clear();

            await using var firstContext = new LamuFlixDbContext(options);
            await using var secondContext = new LamuFlixDbContext(options);
            var firstRepository = Repository(firstContext);
            var secondRepository = Repository(secondContext);
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var firstClaim = ClaimWhenReleasedAsync(firstRepository, id, start.Task);
            var secondClaim = ClaimWhenReleasedAsync(secondRepository, id, start.Task);
            start.SetResult();
            var results = await Task.WhenAll(firstClaim, secondClaim);

            results.Count(claimed => claimed).ShouldBe(1);
            results.Count(claimed => !claimed).ShouldBe(1);
            var attempts = await seed.Movies.AsNoTracking()
                .Where(movie => movie.Id == id)
                .Select(movie => movie.EnrichmentAttempts)
                .SingleAsync(TestContext.Current.CancellationToken);
            attempts.ShouldBe(1);
        }

        var commands = capture.Commands;
        commands.Length.ShouldBe(ClaimIterations * 2);
        commands.ShouldAllBe(command => command.Contains("UPDATE", StringComparison.OrdinalIgnoreCase)
            && command.Contains("WHERE", StringComparison.OrdinalIgnoreCase)
            && !command.Contains("LOCK TABLE", StringComparison.OrdinalIgnoreCase)
            && !command.Contains("FOR UPDATE", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<bool> ClaimWhenReleasedAsync(
        EfMovieRepository repository,
        MovieId id,
        Task start)
    {
        await start;
        return await repository.TryClaimForEnrichmentAsync(id, TestContext.Current.CancellationToken);
    }

    private static EfMovieRepository Repository(LamuFlixDbContext db) =>
        new(db, new FixedTimeProvider(Now), Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }));
}