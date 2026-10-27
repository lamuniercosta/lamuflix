using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

/// <summary>
/// DEV-414 T001: real-PostgreSQL proof of the scalar plot read seam. Missing movie and null plot
/// intentionally collapse to null (brief Q3/Q6); the projection returns the stored plot unchanged,
/// so whitespace normalization is never asserted here.
/// </summary>
[Collection(nameof(PostgresCollection))]
public sealed class EfMovieRepositoryPlotTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetPlotAsync_AbsentMovie_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateMigratedContext();

        var plot = await Repository(context).GetPlotAsync(new MovieId(987_654), ct);

        plot.ShouldBeNull();
    }

    [Fact]
    public async Task GetPlotAsync_StoredNullPlot_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync("plot-null", plot: null, ct);
        await using var context = fixture.CreateMigratedContext();

        var plot = await Repository(context).GetPlotAsync(movieId, ct);

        plot.ShouldBeNull();
    }

    [Fact]
    public async Task GetPlotAsync_StoredNonblankPlot_ReturnsTheStoredValueUnchanged()
    {
        const string stored = "  A midnight train carries a stranger home.\u00A0";
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync("plot-nonblank", stored, ct);
        await using var context = fixture.CreateMigratedContext();

        var plot = await Repository(context).GetPlotAsync(movieId, ct);

        plot.ShouldBe(stored);
    }

    [Fact]
    public async Task GetPlotAsync_WhitespaceOnlyPlot_ReturnsTheStoredValueUnchanged()
    {
        const string stored = " \t\r\n\u00A0\u2003\u3000 ";
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync("plot-whitespace", stored, ct);
        await using var context = fixture.CreateMigratedContext();

        var plot = await Repository(context).GetPlotAsync(movieId, ct);

        plot.ShouldBe(stored);
    }

    [Fact]
    public async Task GetPlotAsync_ReadsFromDatabaseWithoutTrackingTheMovie()
    {
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync("plot-untracked", "Stored plot", ct);
        await using var context = fixture.CreateMigratedContext();

        var plot = await Repository(context).GetPlotAsync(movieId, ct);

        plot.ShouldBe("Stored plot");
        context.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPlotAsync_CancelledToken_PropagatesCancellation()
    {
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync("plot-cancelled", "Stored plot", ct);
        await using var context = fixture.CreateMigratedContext();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => Repository(context).GetPlotAsync(movieId, cancelled.Token));
    }

    private static EfMovieRepository Repository(LamuFlixDbContext db) =>
        new(
            db,
            TimeProvider.System,
            Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }),
            new RecordingEnrichmentStatusNotifier(),
            NullLogger<EfMovieRepository>.Instance);

    private async Task<MovieId> SeedMovieAsync(string pathSuffix, string? plot, CancellationToken cancellationToken)
    {
        await using var context = fixture.CreateMigratedContext();
        var record = MovieCatalogSeed.Create("Plot Owner", pathSuffix);
        record.Plot = plot;
        await MovieCatalogSeed.AddAsync(context, record, cancellationToken);
        return record.Id ?? throw new InvalidOperationException("The seeded movie has no id.");
    }
}
