using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class EfMovieFeaturesRepositoryTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset ComputedAt =
        new(2026, 10, 22, 10, 30, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetByMovieIdAsync_AbsentFeatures_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync("C:/library/features-absent.mkv", ct);
        await using var context = fixture.CreateMigratedContext();
        var repository = new EfMovieFeaturesRepository(context);

        var snapshot = await repository.GetByMovieIdAsync(movieId, ct);

        snapshot.ShouldBeNull();
    }

    [Fact]
    public async Task UpsertAsync_RoundTripsVectorAndComputedAtFromFreshContext()
    {
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync("C:/library/features-roundtrip.mkv", ct);
        var vector = VariedVector();
        var submitted = new MovieFeaturesSnapshot(movieId, vector, ComputedAt);

        await using (var write = fixture.CreateMigratedContext())
        {
            await new EfMovieFeaturesRepository(write).UpsertAsync(submitted, ct);
        }

        await using var read = fixture.CreateMigratedContext();
        var stored = await new EfMovieFeaturesRepository(read).GetByMovieIdAsync(movieId, ct);

        stored.ShouldNotBeNull();
        stored.MovieId.ShouldBe(movieId);
        stored.Vector.SequenceEqual(vector).ShouldBeTrue();
        stored.ComputedAt.ShouldBe(ComputedAt);
        stored.ComputedAt.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public async Task UpsertAsync_AllZeroVector_IsStoredAndReadFromFreshContext()
    {
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync("C:/library/features-zero.mkv", ct);
        var zero = ImmutableArray.CreateRange(Enumerable.Repeat(0f, EmbeddingDimensions.Value));

        await using (var write = fixture.CreateMigratedContext())
        {
            await new EfMovieFeaturesRepository(write)
                .UpsertAsync(new MovieFeaturesSnapshot(movieId, zero, ComputedAt), ct);
        }

        await using var read = fixture.CreateMigratedContext();
        var record = await read.MovieFeatures.AsNoTracking().SingleAsync(ct);
        record.MovieId.ShouldBe(movieId);
        record.Vector.ToArray().ShouldAllBe(value => value == 0f);

        var stored = await new EfMovieFeaturesRepository(read).GetByMovieIdAsync(movieId, ct);
        stored.ShouldNotBeNull();
        stored.Vector.SequenceEqual(zero).ShouldBeTrue();
    }

    [Fact]
    public async Task UpsertAsync_Twice_KeepsOneRowAndUpdatesVectorAndComputedAt()
    {
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync("C:/library/features-repeat.mkv", ct);
        var first = new MovieFeaturesSnapshot(movieId, ConstantVector(0.25f), ComputedAt);
        var second = new MovieFeaturesSnapshot(movieId, ConstantVector(-0.5f), ComputedAt.AddMinutes(5));

        await using (var context = fixture.CreateMigratedContext())
        {
            var repository = new EfMovieFeaturesRepository(context);
            await repository.UpsertAsync(first, ct);
            await repository.UpsertAsync(second, ct);
        }

        await using var read = fixture.CreateMigratedContext();
        (await read.MovieFeatures.CountAsync(ct)).ShouldBe(1);
        var stored = await new EfMovieFeaturesRepository(read).GetByMovieIdAsync(movieId, ct);
        stored.ShouldNotBeNull();
        stored.Vector.SequenceEqual(second.Vector).ShouldBeTrue();
        stored.ComputedAt.ShouldBe(second.ComputedAt);
    }

    [Fact]
    public async Task UpsertAsync_ConcurrentContexts_LeavesOneCoherentSubmittedPair()
    {
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync("C:/library/features-concurrent.mkv", ct);
        var first = new MovieFeaturesSnapshot(movieId, ConstantVector(0.125f), ComputedAt);
        var second = new MovieFeaturesSnapshot(movieId, ConstantVector(0.75f), ComputedAt.AddMinutes(1));

        await Task.WhenAll(
            UpsertFromNewContextAsync(first, ct),
            UpsertFromNewContextAsync(second, ct));

        await using var read = fixture.CreateMigratedContext();
        (await read.MovieFeatures.CountAsync(ct)).ShouldBe(1);
        var stored = await new EfMovieFeaturesRepository(read).GetByMovieIdAsync(movieId, ct);
        stored.ShouldNotBeNull();
        (IsCoherentPair(stored, first) || IsCoherentPair(stored, second)).ShouldBeTrue();
    }

    [Fact]
    public async Task UpsertAsync_MissingParentMovie_IsRejectedByForeignKey()
    {
        var ct = TestContext.Current.CancellationToken;
        var missingMovieId = new MovieId(999_999);
        var snapshot = new MovieFeaturesSnapshot(missingMovieId, ConstantVector(0.5f), ComputedAt);

        await using var context = fixture.CreateMigratedContext();
        var repository = new EfMovieFeaturesRepository(context);

        var failure = await Should.ThrowAsync<PostgresException>(
            () => repository.UpsertAsync(snapshot, ct));

        failure.SqlState.ShouldBe(PostgresErrorCodes.ForeignKeyViolation);
        (await context.MovieFeatures.CountAsync(ct)).ShouldBe(0);
    }

    [Fact]
    public async Task DeleteMovie_CascadesItsStoredFeatures()
    {
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync("C:/library/features-cascade.mkv", ct);
        await using (var write = fixture.CreateMigratedContext())
        {
            await new EfMovieFeaturesRepository(write)
                .UpsertAsync(new MovieFeaturesSnapshot(movieId, ConstantVector(0.375f), ComputedAt), ct);
        }

        await using (var delete = fixture.CreateMigratedContext())
        {
            var movie = await delete.Movies.SingleAsync(record => record.Id == movieId, ct);
            delete.Movies.Remove(movie);
            await delete.SaveChangesAsync(ct);
        }

        await using var verify = fixture.CreateMigratedContext();
        (await verify.Movies.CountAsync(movie => movie.Id == movieId, ct)).ShouldBe(0);
        (await verify.MovieFeatures.CountAsync(ct)).ShouldBe(0);
    }

    [Fact]
    public async Task GetByMovieIdAsync_CancelledToken_PropagatesCancellation()
    {
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync("C:/library/features-cancel-read.mkv", ct);
        await using var context = fixture.CreateMigratedContext();
        var repository = new EfMovieFeaturesRepository(context);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => repository.GetByMovieIdAsync(movieId, cancelled.Token));
    }

    [Fact]
    public async Task UpsertAsync_CancelledToken_PropagatesCancellationBeforeSql()
    {
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync("C:/library/features-cancel-write.mkv", ct);
        var snapshot = new MovieFeaturesSnapshot(movieId, ConstantVector(0.625f), ComputedAt);
        await using var context = fixture.CreateMigratedContext();
        var repository = new EfMovieFeaturesRepository(context);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => repository.UpsertAsync(snapshot, cancelled.Token));

        (await context.MovieFeatures.CountAsync(ct)).ShouldBe(0);
    }

    private async Task UpsertFromNewContextAsync(MovieFeaturesSnapshot snapshot, CancellationToken cancellationToken)
    {
        await using var context = fixture.CreateMigratedContext();
        await new EfMovieFeaturesRepository(context).UpsertAsync(snapshot, cancellationToken);
    }

    private async Task<MovieId> SeedMovieAsync(string libraryPath, CancellationToken cancellationToken)
    {
        await using var context = fixture.CreateMigratedContext();
        var movie = new MovieRecord
        {
            Title = "Features Owner",
            LibraryPath = new LibraryPath(libraryPath),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Pending
        };
        context.Movies.Add(movie);
        await context.SaveChangesAsync(cancellationToken);
        var movieId = movie.Id;
        movieId.ShouldNotBeNull();
        return movieId;
    }

    private static ImmutableArray<float> ConstantVector(float value) =>
        ImmutableArray.CreateRange(Enumerable.Repeat(value, EmbeddingDimensions.Value));

    private static ImmutableArray<float> VariedVector() =>
        ImmutableArray.CreateRange(
            Enumerable.Range(0, EmbeddingDimensions.Value)
                .Select(index => ((index % 7) - 3) * 0.125f));

    private static bool IsCoherentPair(MovieFeaturesSnapshot stored, MovieFeaturesSnapshot submitted) =>
        stored.Vector.SequenceEqual(submitted.Vector) && stored.ComputedAt == submitted.ComputedAt;
}
