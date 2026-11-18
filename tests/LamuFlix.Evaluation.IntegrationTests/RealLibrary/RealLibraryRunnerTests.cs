using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Evaluation.Entry;
using LamuFlix.Evaluation.RealLibrary;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Testcontainers.PostgreSql;

namespace LamuFlix.Evaluation.IntegrationTests.RealLibrary;

public sealed class RealLibraryRunnerTests : IAsyncLifetime
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
    public async Task LeaveOneOut_ranks_against_a_real_snapshot_without_writing()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var likedId = await SeedMovieAsync("Liked seed", isWatched: true, Review.Liked, ct, "Drama");
        var watchedId = await SeedMovieAsync("Watched seed", isWatched: true, Review.None, ct, "Drama");
        var otherId = await SeedMovieAsync("Other candidate", isWatched: false, Review.None, ct, "Drama");
        await SeedVectorAsync(likedId, Vector(1.0), ct);
        await SeedVectorAsync(watchedId, Vector(1.0), ct);
        await SeedVectorAsync(otherId, Vector(0.9), ct);
        await SeedLaunchAsync(likedId, ct);

        var interceptor = new RecordingCommandInterceptor();
        await using var db = OpenContext(interceptor);
        var snapshot = await RealLibrarySnapshotLoader.LoadAsync(db, ct);
        interceptor.Reset();

        // act
        var result = await LeaveOneOutRunner.RunAsync(snapshot, Options, TimeProvider.System, ct);

        // assert
        result.Liked.Attempts.ShouldHaveSingleItem().Seed.ShouldBe(likedId);
        result.Watched.Attempts.Length.ShouldBe(2);
        result.Overlap.ShouldBe(new CohortOverlap(1, 2, 1));
        result.Liked.Metrics.AttemptedCount.ShouldBe(1);
        result.Disclosures.ShouldNotBeEmpty();

        var report = LeaveOneOutReportSerializer.SerializeToUtf8Bytes(result);
        report.ShouldNotBeEmpty();
        interceptor.WriteCommandCount.ShouldBe(0);
    }

    [Fact]
    public async Task Compare_scores_every_other_enriched_movie_without_writing()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var selectedId = await SeedMovieAsync("Selected", isWatched: false, Review.None, ct, "Drama");
        var scoredId = await SeedMovieAsync("Scored", isWatched: false, Review.None, ct, "Drama");
        var pendingId = await SeedMovieAsync("Pending", isWatched: false, Review.None, ct, "Drama", EnrichmentStatus.Pending);
        await SeedVectorAsync(selectedId, Vector(1.0), ct);
        await SeedVectorAsync(scoredId, Vector(0.5), ct);

        var interceptor = new RecordingCommandInterceptor();
        await using var db = OpenContext(interceptor);
        var snapshot = await RealLibrarySnapshotLoader.LoadAsync(db, ct);
        interceptor.Reset();

        // act
        var report = CompareRunner.Run(snapshot, Options, selectedId.Value, otherId: null, ct);

        // assert
        var rows = report.Envelope.Lists.ShouldHaveSingleItem().Rows;
        rows.Select(row => row.MovieId).ShouldBe([scoredId.Value]);
        rows.ShouldNotContain(row => row.MovieId == pendingId.Value);
        var diagnostics = rows[0].Diagnostics.ShouldNotBeNull();
        diagnostics.RawPlotCosine.ShouldNotBeNull();
        diagnostics.PlotRank.ShouldBe(1);
        report.Disclosures.ShouldNotBeEmpty();

        var bytes = RecommendationArtifactSerializer.SerializeToUtf8Bytes(report.Envelope);
        RecommendationArtifactSerializer.ReadFromUtf8Bytes(bytes).ShouldNotBeNull();
        interceptor.WriteCommandCount.ShouldBe(0);
    }

    [Fact]
    public async Task Operator_entry_writes_the_leave_one_out_artifact_for_the_marked_mode()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var likedId = await SeedMovieAsync("Liked seed", isWatched: true, Review.Liked, ct, "Drama");
        var otherId = await SeedMovieAsync("Other candidate", isWatched: false, Review.None, ct, "Drama");
        await SeedVectorAsync(likedId, Vector(1.0), ct);
        await SeedVectorAsync(otherId, Vector(0.9), ct);
        await SeedLaunchAsync(likedId, ct);

        using var output = new TemporaryOutputDirectory();
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [EvaluationEntry.ModeVariable] = EvaluationEntry.LeaveOneOutModeValue,
            [EvaluationEntry.OutputPathVariable] = output.Combine("leave-one-out.json"),
            [RealLibrarySnapshotLoader.CredentialVariableName] = ConnectionString(),
        };

        // act
        var published = await EvaluationEntry.RunRealLibraryAsync(
            name => environment.GetValueOrDefault(name),
            ct);

        // assert
        var path = published.ShouldNotBeNull();
        path.ShouldBe(output.Combine("leave-one-out.json"));

        using var document = JsonDocument.Parse(await File.ReadAllBytesAsync(path, ct));
        var root = document.RootElement;
        root.GetProperty("mode").GetString().ShouldBe("leave-one-out");
        root.GetProperty("cohorts").GetArrayLength().ShouldBe(2);
        root.GetProperty("disclosures").GetArrayLength().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Operator_entry_writes_the_compare_artifact_to_the_selected_id_default_path()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var selectedId = await SeedMovieAsync("Selected", isWatched: false, Review.None, ct, "Drama");
        var scoredId = await SeedMovieAsync("Scored", isWatched: false, Review.None, ct, "Drama");
        await SeedVectorAsync(selectedId, Vector(1.0), ct);
        await SeedVectorAsync(scoredId, Vector(0.5), ct);

        using var output = new TemporaryOutputDirectory();
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [EvaluationEntry.ModeVariable] = EvaluationEntry.CompareModeValue,
            [EvaluationEntry.SelectedIdVariable] = selectedId.Value.ToString(CultureInfo.InvariantCulture),
            [EvaluationEntry.OutputPathVariable] = output.Combine("compare.json"),
            [RealLibrarySnapshotLoader.CredentialVariableName] = ConnectionString(),
        };

        // act
        var published = await EvaluationEntry.RunRealLibraryAsync(
            name => environment.GetValueOrDefault(name),
            ct);

        // assert
        var path = published.ShouldNotBeNull();
        path.ShouldBe(output.Combine("compare.json"));

        var bytes = await File.ReadAllBytesAsync(path, ct);
        var envelope = RecommendationArtifactSerializer.ReadFromUtf8Bytes(bytes);
        envelope.Mode.ShouldBe(RecommendationArtifactContract.CompareMode);
        envelope.Targets.ShouldBe([selectedId.Value]);
        envelope.Lists.ShouldHaveSingleItem().Rows.ShouldHaveSingleItem().MovieId.ShouldBe(scoredId.Value);

        using var document = JsonDocument.Parse(bytes);
        document.RootElement.GetProperty("mode").GetString().ShouldBe(RecommendationArtifactContract.CompareMode);
    }

    private string ConnectionString() =>
        container?.GetConnectionString()
        ?? throw new InvalidOperationException("The PostgreSQL container is not running.");

    private sealed class TemporaryOutputDirectory : IDisposable
    {
        private readonly string fullPath =
            Path.Combine(Path.GetTempPath(), "lamuflix-evaluation-" + Guid.NewGuid().ToString("N"));

        public TemporaryOutputDirectory() => Directory.CreateDirectory(fullPath);

        public string Combine(string name) => Path.Combine(fullPath, name);

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, recursive: true);
                }
            }
            catch (IOException)
            {
                // Cleanup is best-effort; it must never fail an assertion.
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup must never fail an assertion.
            }
        }
    }

    private LamuFlixDbContext OpenContext() => OpenContext(new NoopInterceptor());

    private LamuFlixDbContext OpenContext(Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor interceptor)
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
        System.Threading.CancellationToken ct,
        string genre,
        EnrichmentStatus? status = null)
    {
        await using var db = OpenContext();
        var record = new MovieRecord
        {
            Title = title,
            LibraryPath = new LibraryPath($"C:/library/{Guid.NewGuid():N}.mkv"),
            Format = new MediaFormat("mkv"),
            IsWatched = isWatched,
            Review = review,
            Status = status ?? EnrichmentStatus.Enriched,
            MetadataTitle = title,
        };

        record.Genres.Add(new GenreRecord { Name = genre });

        db.Movies.Add(record);
        await db.SaveChangesAsync(ct);

        return record.Id ?? throw new InvalidOperationException("The database did not assign a movie id.");
    }

    private async Task SeedLaunchAsync(MovieId movieId, System.Threading.CancellationToken ct)
    {
        await using var db = OpenContext();
        db.PlaybackEvents.Add(new PlaybackEventRecord
        {
            MovieId = movieId,
            PlayedAt = new DateTimeOffset(2026, 9, 1, 20, 0, 0, TimeSpan.Zero),
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedVectorAsync(MovieId movieId, float[] vector, System.Threading.CancellationToken ct)
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

    private static float[] Vector(double cosine)
    {
        var values = new float[EmbeddingDimensions.Value];
        values[0] = (float)cosine;
        values[1] = (float)Math.Sqrt(Math.Max(0.0, 1.0 - (cosine * cosine)));
        return values;
    }

    private sealed class NoopInterceptor : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor;

    private sealed class RecordingCommandInterceptor : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
    {
        private int writeCommandCount;

        public int WriteCommandCount => writeCommandCount;

        public void Reset() => writeCommandCount = 0;

        public override Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> NonQueryExecuting(
            System.Data.Common.DbCommand command,
            Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData,
            Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result)
        {
            writeCommandCount++;
            return result;
        }

        public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> NonQueryExecutingAsync(
            System.Data.Common.DbCommand command,
            Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData,
            Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result,
            System.Threading.CancellationToken cancellationToken = default)
        {
            writeCommandCount++;
            return ValueTask.FromResult(result);
        }

        public override Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader> ReaderExecuting(
            System.Data.Common.DbCommand command,
            Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData,
            Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader> result)
        {
            CountIfWrite(command);
            return result;
        }

        public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(
            System.Data.Common.DbCommand command,
            Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData,
            Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader> result,
            System.Threading.CancellationToken cancellationToken = default)
        {
            CountIfWrite(command);
            return ValueTask.FromResult(result);
        }

        private void CountIfWrite(System.Data.Common.DbCommand command)
        {
            var text = command.CommandText.AsSpan().TrimStart();
            if (!text.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
                && !text.StartsWith("WITH", StringComparison.OrdinalIgnoreCase)
                && !text.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
            {
                writeCommandCount++;
            }
        }
    }
}
