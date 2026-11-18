using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Library;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Evaluation.RealLibrary;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Pgvector;
using Testcontainers.PostgreSql;
using LibrarySortDirection = LamuFlix.Core.Library.SortDirection;

namespace LamuFlix.Evaluation.IntegrationTests.RealLibrary;

public sealed class RealLibraryReadOnlyLoaderTests : IAsyncLifetime
{
    private const string Image =
        "pgvector/pgvector:0.8.7-pg17@sha256:ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d";

    private static readonly RecommendationOptions Options = new();

    private PostgreSqlContainer? container;

    public async ValueTask InitializeAsync()
    {
        container = new PostgreSqlBuilder(Image).Build();
        await container.StartAsync(TestContext.Current.CancellationToken);

        await using var db = OpenContext();
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
            container = null;
        }
    }

    [Fact]
    public async Task LoadAsync_reads_one_snapshot_without_tracking_or_writing()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var watchedId = await SeedMovieAsync("Watched seed", isWatched: true, Review.Liked, ct);
        await SeedMovieAsync("Plain candidate", isWatched: false, Review.None, ct);
        await SeedLaunchAsync(watchedId, ct);

        var interceptor = new RecordingCommandInterceptor();

        // act
        await using var db = OpenContext(interceptor);
        var snapshot = await RealLibrarySnapshotLoader.LoadAsync(db, ct);

        // assert
        snapshot.Movies.Length.ShouldBe(2);
        db.ChangeTracker.Entries().ShouldBeEmpty();
        interceptor.WriteCommandCount.ShouldBe(0);
    }

    [Fact]
    public async Task LoadAsync_keeps_watched_review_and_launch_signals_for_the_profile()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var likedId = await SeedMovieAsync("Liked seed", isWatched: true, Review.Liked, ct, "Drama");
        var launchOnlyId = await SeedMovieAsync("Launch only", isWatched: false, Review.None, ct, "Comedy");
        await SeedLaunchAsync(launchOnlyId, ct);

        // act
        await using var db = OpenContext();
        var snapshot = await RealLibrarySnapshotLoader.LoadAsync(db, ct);

        // assert
        var liked = snapshot.ProfileMovies.Single(movie => movie.MovieId == likedId);
        liked.IsWatched.ShouldBeTrue();
        liked.Review.ShouldBe(Review.Liked);
        liked.Genres.ShouldBe(["Drama"]);

        var launchOnly = snapshot.ProfileMovies.Single(movie => movie.MovieId == launchOnlyId);
        launchOnly.PlayedAt.Length.ShouldBe(1);
        launchOnly.IsWatched.ShouldBeFalse();
    }

    [Fact]
    public async Task HiddenSeedCopy_clears_every_signal_while_retaining_the_target_candidate()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var hiddenId = await SeedMovieAsync("Hidden seed", isWatched: true, Review.Liked, ct, "Drama");
        await SeedMovieAsync("Other candidate", isWatched: false, Review.None, ct, "Comedy");
        await SeedLaunchAsync(hiddenId, ct);

        await using var db = OpenContext();
        var snapshot = await RealLibrarySnapshotLoader.LoadAsync(db, ct);

        // act
        var copy = HiddenSeedEvaluationCopy.Create(snapshot, hiddenId, Options, TimeProvider.System);
        var profile = await copy.ProfileSource.GetTasteProfileAsync(ct);
        var inputs = await copy.ProfileSource.GetProfileInputsAsync([hiddenId], [], ct);
        var hidden = await copy.Repository.GetAsync(hiddenId, ct);
        var browse = await copy.Catalog.BrowseAsync(
            new MovieQuery
            {
                Sort = MovieSort.Title,
                Direction = LibrarySortDirection.Ascending,
                Page = new Page(1, 100),
            },
            ct);

        // assert
        profile.LikedSources.ShouldBeEmpty();
        profile.Liked.IsEmpty.ShouldBeTrue();
        inputs.Movies.ShouldBeEmpty();

        hidden.ShouldNotBeNull();
        hidden.Id.ShouldBe(hiddenId);
        hidden.IsWatched.ShouldBeFalse();
        hidden.Review.ShouldBe(Review.None);

        browse.Items.ShouldContain(summary => summary.Id == hiddenId);
        var hiddenSummary = browse.Items.Single(summary => summary.Id == hiddenId);
        hiddenSummary.IsWatched.ShouldBeFalse();
        hiddenSummary.Review.ShouldBe(Review.None);
    }

    [Fact]
    public async Task Each_seed_gets_its_own_copy_without_leaking_the_other_seed()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var firstId = await SeedMovieAsync("First seed", isWatched: true, Review.Liked, ct, "Drama");
        var secondId = await SeedMovieAsync("Second seed", isWatched: true, Review.Disliked, ct, "Comedy");

        await using var db = OpenContext();
        var snapshot = await RealLibrarySnapshotLoader.LoadAsync(db, ct);

        // act
        var firstCopy = HiddenSeedEvaluationCopy.Create(snapshot, firstId, Options, TimeProvider.System);
        var secondCopy = HiddenSeedEvaluationCopy.Create(snapshot, secondId, Options, TimeProvider.System);

        var firstProfile = await firstCopy.ProfileSource.GetTasteProfileAsync(ct);
        var secondProfile = await secondCopy.ProfileSource.GetTasteProfileAsync(ct);
        var firstView = await firstCopy.Repository.GetAsync(firstId, ct);
        var secondView = await secondCopy.Repository.GetAsync(secondId, ct);

        // assert
        firstCopy.ShouldNotBeSameAs(secondCopy);
        firstCopy.HiddenSeed.ShouldBe(firstId);
        secondCopy.HiddenSeed.ShouldBe(secondId);
        firstProfile.LikedSources.ShouldBeEmpty();
        firstProfile.Disliked.IsEmpty.ShouldBeFalse();
        secondProfile.LikedSources.Select(source => source.MovieId).ShouldBe([firstId]);
        secondProfile.Disliked.IsEmpty.ShouldBeTrue();

        firstView.ShouldNotBeNull();
        firstView.IsWatched.ShouldBeFalse();
        secondView.ShouldNotBeNull();
        secondView.IsWatched.ShouldBeFalse();
        secondView.Review.ShouldBe(Review.None);

        snapshot.ProfileMovies.Single(movie => movie.MovieId == firstId).IsWatched.ShouldBeTrue();
    }

    [Fact]
    public async Task HiddenSeedCopy_ranks_with_the_real_handler_and_never_writes()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var hiddenId = await SeedMovieAsync("Hidden ranking seed", isWatched: true, Review.Liked, ct, "Drama");
        var signalId = await SeedMovieAsync("Signal provider", isWatched: true, Review.Liked, ct, "Drama");
        var otherId = await SeedMovieAsync("Other ranking seed", isWatched: false, Review.None, ct, "Drama");
        await SeedVectorAsync(hiddenId, Vector(offset: 0), ct);
        await SeedVectorAsync(signalId, Vector(offset: 7), ct);
        await SeedVectorAsync(otherId, Vector(offset: 7), ct);

        var interceptor = new RecordingCommandInterceptor();
        await using var db = OpenContext(interceptor);
        var snapshot = await RealLibrarySnapshotLoader.LoadAsync(db, ct);
        interceptor.Reset();

        // act
        var copy = HiddenSeedEvaluationCopy.Create(snapshot, hiddenId, Options, TimeProvider.System);
        var ranked = await copy.RankAsync(take: 10, ct);

        // assert
        ranked.ShouldNotBeEmpty();
        ranked.ShouldContain(movie => movie.Summary.Id == otherId);
        interceptor.WriteCommandCount.ShouldBe(0);
    }

    [Fact]
    public void SnapshotRepository_mutation_members_throw()
    {
        // arrange
        var snapshot = BuildMinimalSnapshot();

        // act
        var copy = HiddenSeedEvaluationCopy.Create(snapshot, new MovieId(1), Options, TimeProvider.System);
        var repository = copy.Repository;

        // assert
        Should.Throw<InvalidOperationException>(() => repository.SaveChangesAsync(CancellationToken.None));
        Should.Throw<InvalidOperationException>(() =>
            repository.AddAsync(snapshot.Movies[0], CancellationToken.None));
        Should.Throw<InvalidOperationException>(() =>
            repository.TryDeleteAsync(new MovieId(1), CancellationToken.None));
        Should.Throw<InvalidOperationException>(() =>
            repository.TryClaimForEnrichmentAsync(new MovieId(1), CancellationToken.None));
    }

    [Fact]
    public void HiddenSeedCopy_rejects_a_seed_outside_the_snapshot()
    {
        // arrange
        var snapshot = BuildMinimalSnapshot();

        // act
        var act = () => HiddenSeedEvaluationCopy.Create(snapshot, new MovieId(99), Options, TimeProvider.System);

        // assert
        Should.Throw<ArgumentException>(act);
    }

    private static RealLibrarySnapshot BuildMinimalSnapshot()
    {
        var movie = Movie.Create(
            new MovieId(1),
            "Snapshot movie",
            new LibraryPath("C:/library/snapshot-movie.mkv"),
            new MediaFormat("mkv"));

        return new RealLibrarySnapshot(
            [movie],
            [],
            ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty,
            new GenreCounts(1, new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)));
    }
    private LamuFlixDbContext OpenContext() => OpenContext(new NoopInterceptor());

    private LamuFlixDbContext OpenContext(DbCommandInterceptor interceptor)
    {
        var connectionString = container?.GetConnectionString()
            ?? throw new InvalidOperationException("The PostgreSQL container is not running.");

        return new LamuFlixDbContext(
            new DbContextOptionsBuilder<LamuFlixDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.UseVector())
                .AddInterceptors(interceptor)
                .Options);
    }

    private async Task<MovieId> SeedMovieAsync(
        string title,
        bool isWatched,
        Review review,
        CancellationToken ct,
        params string[] genres)
    {
        await using var db = OpenContext();
        var record = new MovieRecord
        {
            Title = title,
            LibraryPath = new LibraryPath($"C:/library/{Guid.NewGuid():N}.mkv"),
            Format = new MediaFormat("mkv"),
            IsWatched = isWatched,
            Review = review,
            Status = EnrichmentStatus.Enriched,
            MetadataTitle = title,
        };

        foreach (var genre in genres)
        {
            record.Genres.Add(new GenreRecord { Name = genre });
        }

        db.Movies.Add(record);
        await db.SaveChangesAsync(ct);

        return record.Id ?? throw new InvalidOperationException("The database did not assign a movie id.");
    }

    private async Task SeedLaunchAsync(MovieId movieId, CancellationToken ct)
    {
        await using var db = OpenContext();
        db.PlaybackEvents.Add(new PlaybackEventRecord
        {
            MovieId = movieId,
            PlayedAt = new DateTimeOffset(2026, 9, 1, 20, 0, 0, TimeSpan.Zero),
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedVectorAsync(MovieId movieId, float[] vector, CancellationToken ct)
    {
        await using var db = OpenContext();
        db.MovieFeatures.Add(new MovieFeaturesRecord
        {
            MovieId = movieId,
            Vector = new Vector(vector),
            ComputedAt = new DateTimeOffset(2026, 9, 1, 20, 0, 0, TimeSpan.Zero),
        });
        await db.SaveChangesAsync(ct);
    }

    private static float[] Vector(int offset)
    {
        var values = new float[EmbeddingDimensions.Value];
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = ((index + offset) % 37) / 37f;
        }

        values[0] = 1f;
        return values;
    }

    private sealed class NoopInterceptor : DbCommandInterceptor;

    private sealed class RecordingCommandInterceptor : DbCommandInterceptor
    {
        private int writeCommandCount;

        public int WriteCommandCount => writeCommandCount;

        public void Reset() => writeCommandCount = 0;

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            CountIfWrite(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            CountIfWrite(command);
            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            writeCommandCount++;
            return result;
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            writeCommandCount++;
            return ValueTask.FromResult(result);
        }

        private void CountIfWrite(DbCommand command)
        {
            var text = command.CommandText.AsSpan().TrimStart();
            if (!text.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
                && !text.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
            {
                writeCommandCount++;
            }
        }
    }
}
