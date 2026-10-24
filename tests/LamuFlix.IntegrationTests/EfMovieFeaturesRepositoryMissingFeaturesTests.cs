using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

/// <summary>
/// DEV-414 T002: real-PostgreSQL proof of the movies/movie_features anti-join. Eligibility must match
/// the handler's canonical .NET <see cref="string.IsNullOrWhiteSpace"/> / <see cref="char.IsWhiteSpace(char)"/>
/// Unicode semantics; no SQL TRIM/btrim equivalence is claimed, the parity corpus decides.
/// </summary>
[Collection(nameof(PostgresCollection))]
public sealed class EfMovieFeaturesRepositoryMissingFeaturesTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset ComputedAt = new(2026, 10, 22, 10, 30, 0, TimeSpan.Zero);

    private static readonly (string Label, string? Plot, bool HandlerNonblank)[] ParityCorpus =
    [
        ("null plot", null, false),
        ("empty plot", "", false),
        ("spaces only", "   ", false),
        ("tab and newline only", "\t\n\r\f\v", false),
        ("unicode whitespace only", "\u0085\u00A0\u1680\u2000\u2003\u200A\u2028\u2029\u202F\u205F\u3000", false),
        ("mixed leading and trailing whitespace", " \t\u00A0A real plot\u2003\u3000", true),
        ("plain text", "A plain plot.", true),
        ("leading tab before text", "\tDetective story", true),
        ("zero width space only", "\u200B", true),
    ];

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public void ParityCorpus_MatchesCanonicalDotNetWhitespaceSemantics()
    {
        foreach (var (label, plot, handlerNonblank) in ParityCorpus)
        {
            var canonicalNonblank = plot is not null && !string.IsNullOrWhiteSpace(plot);
            canonicalNonblank.ShouldBe(handlerNonblank, label);
        }
    }

    [Fact]
    public async Task FindMovieIdsMissingFeaturesAsync_ParityCorpus_SelectsExactlyHandlerNonblankMoviesWithoutFeatures()
    {
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedCorpusAsync(ct);
        var expected = seeded
            .Where(item => !string.IsNullOrWhiteSpace(item.Plot))
            .Select(item => item.MovieId.Value)
            .Order()
            .ToArray();
        await using var context = fixture.CreateMigratedContext();

        var selected = await new EfMovieFeaturesRepository(context).FindMovieIdsMissingFeaturesAsync(ct);

        selected.Select(id => id.Value).Order().ShouldBe(expected);
    }

    [Fact]
    public async Task FindMovieIdsMissingFeaturesAsync_ExcludesMoviesThatAlreadyHaveAFeaturesRow()
    {
        var ct = TestContext.Current.CancellationToken;
        var withFeatures = await SeedMovieAsync("anti-join-with-features", "A stored plot", EnrichmentStatus.Enriched, ct);
        var withFeaturesAndBlankPlot = await SeedMovieAsync("anti-join-blank-with-features", "   ", EnrichmentStatus.Enriched, ct);
        var withoutFeatures = await SeedMovieAsync("anti-join-without-features", "Another stored plot", EnrichmentStatus.Enriched, ct);
        await using (var write = fixture.CreateMigratedContext())
        {
            var features = new EfMovieFeaturesRepository(write);
            await features.UpsertAsync(Snapshot(withFeatures), ct);
            await features.UpsertAsync(Snapshot(withFeaturesAndBlankPlot), ct);
        }

        await using var context = fixture.CreateMigratedContext();
        var selected = await new EfMovieFeaturesRepository(context).FindMovieIdsMissingFeaturesAsync(ct);

        selected.Select(id => id.Value).ShouldBe([withoutFeatures.Value]);
    }

    [Fact]
    public async Task FindMovieIdsMissingFeaturesAsync_DoesNotFilterOnEnrichmentStatus()
    {
        var ct = TestContext.Current.CancellationToken;
        var pending = await SeedMovieAsync("anti-join-pending", "Pending plot", EnrichmentStatus.Pending, ct);
        var enriched = await SeedMovieAsync("anti-join-enriched", "Enriched plot", EnrichmentStatus.Enriched, ct);
        var notFound = await SeedMovieAsync("anti-join-not-found", "Not found plot", EnrichmentStatus.NotFound, ct);
        var failed = await SeedMovieAsync("anti-join-failed", "Failed plot", EnrichmentStatus.Failed, ct);
        await using var context = fixture.CreateMigratedContext();

        var selected = await new EfMovieFeaturesRepository(context).FindMovieIdsMissingFeaturesAsync(ct);

        selected.Select(id => id.Value).Order().ShouldBe(
            new[] { pending.Value, enriched.Value, notFound.Value, failed.Value }.Order());
    }

    [Fact]
    public async Task FindMovieIdsMissingFeaturesAsync_EmptyLibrary_ReturnsEmptyList()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateMigratedContext();

        var selected = await new EfMovieFeaturesRepository(context).FindMovieIdsMissingFeaturesAsync(ct);

        selected.ShouldBeEmpty();
    }

    [Fact]
    public async Task FindMovieIdsMissingFeaturesAsync_SelectsEveryCandidateWithOneAntiJoinCommand()
    {
        var ct = TestContext.Current.CancellationToken;
        var interceptor = new ReaderCommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(fixture.ConnectionString, builder => builder.UseVector())
            .AddInterceptors(interceptor)
            .Options;
        await using var context = new LamuFlixDbContext(options);
        for (var index = 0; index < 6; index++)
        {
            var record = MovieCatalogSeed.Create("Batch candidate", $"anti-join-batch-{index}");
            record.Plot = $"Candidate plot {index}";
            context.Movies.Add(record);
        }

        await context.SaveChangesAsync(ct);
        interceptor.Commands.Clear();

        var selected = await new EfMovieFeaturesRepository(context).FindMovieIdsMissingFeaturesAsync(ct);

        selected.Count.ShouldBe(6);
        // One command proves the database-side anti-join replaces any per-movie feature lookup.
        interceptor.Commands.Count.ShouldBe(1);
        interceptor.Commands[0].ShouldContain("movie_features");
        interceptor.Commands[0].ShouldContain("movies");
    }

    [Fact]
    public async Task FindMovieIdsMissingFeaturesAsync_CancelledToken_PropagatesCancellation()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedMovieAsync("anti-join-cancelled", "Stored plot", EnrichmentStatus.Pending, ct);
        await using var context = fixture.CreateMigratedContext();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => new EfMovieFeaturesRepository(context).FindMovieIdsMissingFeaturesAsync(cancelled.Token));
    }

    private static MovieFeaturesSnapshot Snapshot(MovieId movieId) =>
        new(movieId, [.. Enumerable.Repeat(0.5f, EmbeddingDimensions.Value)], ComputedAt);

    private async Task<List<(MovieId MovieId, string? Plot)>> SeedCorpusAsync(CancellationToken cancellationToken)
    {
        var seeded = new List<(MovieId MovieId, string? Plot)>(ParityCorpus.Length);
        foreach (var (label, plot, _) in ParityCorpus)
        {
            var movieId = await SeedMovieAsync(
                $"anti-join-corpus-{label.Replace(' ', '-')}", plot, EnrichmentStatus.Pending, cancellationToken);
            seeded.Add((movieId, plot));
        }

        return seeded;
    }

    private async Task<MovieId> SeedMovieAsync(
        string pathSuffix,
        string? plot,
        EnrichmentStatus status,
        CancellationToken cancellationToken)
    {
        await using var context = fixture.CreateMigratedContext();
        var record = new MovieRecord
        {
            Title = "Anti Join Owner",
            LibraryPath = new LibraryPath($"C:/library/{pathSuffix}.mkv"),
            Format = new MediaFormat("mkv"),
            Status = status,
            Plot = plot,
        };
        await MovieCatalogSeed.AddAsync(context, record, cancellationToken);
        return record.Id ?? throw new InvalidOperationException("The seeded movie has no id.");
    }

    private sealed class ReaderCommandCaptureInterceptor : DbCommandInterceptor
    {
        internal List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Commands.Add(command.CommandText);
            return result;
        }
    }
}
