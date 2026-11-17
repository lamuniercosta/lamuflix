using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Import;
using LamuFlix.UnitTests.Pipeline;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LamuFlix.UnitTests.Features.Import;

[Collection(nameof(MetricsCollection))]
public sealed class ImportLibraryFoldersCommandHandlerTests
{
    private const string MeterName = "LamuFlix";
    private const string ImportInstrumentName = "lamuflix.import.count";

    private readonly IMovieFolderImporter importer = Substitute.For<IMovieFolderImporter>();
    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IEnrichmentOutbox outbox = Substitute.For<IEnrichmentOutbox>();

    private static readonly IOptions<LibraryOptions> LibraryRoot = Microsoft.Extensions.Options.Options.Create(
        new LibraryOptions { RootPath = "C:/library" });

    [Fact]
    public async Task HandleAsync_AlreadyImportedFolder_SkipsWithoutCallingImporter()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMoviePathsAsync(ct).Returns(new List<LibraryPath> { new("Heat") });
        var handler = new ImportLibraryFoldersCommandHandler(importer, movies, outbox, LibraryRoot);

        // act
        var result = await handler.HandleAsync(
            new ImportLibraryFoldersCommand(["Heat"], RetryFailed: false), ct);

        // assert
        result.Imported.ShouldBe(0);
        result.Skipped.ShouldBe(1);
        result.Retried.ShouldBe(0);
        result.Failures.ShouldBeEmpty();
        await importer.DidNotReceive().ImportAsync(Arg.Any<LibraryPath>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_OneFault_ImportsRestAndCollectsClosedCodeFailure()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMoviePathsAsync(ct).Returns(new List<LibraryPath>());
        importer.ImportAsync(new LibraryPath("Alpha"), ct).Returns(new MovieId(1));
        importer.ImportAsync(new LibraryPath("Gone"), ct).ThrowsAsync(new InvalidOperationException("boom"));
        importer.ImportAsync(new LibraryPath("Bravo"), ct).Returns(new MovieId(2));
        using var capture = new ImportCountCapture();
        var handler = new ImportLibraryFoldersCommandHandler(importer, movies, outbox, LibraryRoot);

        // act
        var result = await handler.HandleAsync(
            new ImportLibraryFoldersCommand(["Alpha", "Gone", "Bravo"], RetryFailed: false), ct);

        // assert
        result.Imported.ShouldBe(2);
        result.Skipped.ShouldBe(0);
        result.Failures.Select(failure => failure.Folder).ShouldBe(["Gone"]);
        result.Failures.Select(failure => failure.Reason).ShouldBe([ImportFolderFailureReason.ImportFailed]);
        result.Failures.ShouldHaveSingleItem().Reason.Code.ShouldBe("import-failed");
        capture.Measurements.ShouldBeEmpty();
        await importer.Received(1).ImportAsync(new LibraryPath("Alpha"), ct);
        await importer.Received(1).ImportAsync(new LibraryPath("Bravo"), ct);
    }

    [Fact]
    public async Task HandleAsync_DisappearingCandidate_MapsToFolderNotFound()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMoviePathsAsync(ct).Returns(new List<LibraryPath>());
        importer.ImportAsync(new LibraryPath("Gone"), ct).ThrowsAsync(new NotFoundException());
        var handler = new ImportLibraryFoldersCommandHandler(importer, movies, outbox, LibraryRoot);

        // act
        var result = await handler.HandleAsync(
            new ImportLibraryFoldersCommand(["Gone"], RetryFailed: false), ct);

        // assert
        result.Imported.ShouldBe(0);
        result.Failures.ShouldHaveSingleItem().Reason.ShouldBe(ImportFolderFailureReason.FolderNotFound);
    }

    [Theory]
    [InlineData("NoVideo", "no-video")]
    [InlineData("Invalid", "invalid-folder")]
    public async Task HandleAsync_OtherFaults_MapToClosedCodes(string folder, string expectedCode)
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMoviePathsAsync(ct).Returns(new List<LibraryPath>());
        if (folder == "NoVideo")
        {
            importer.ImportAsync(new LibraryPath(folder), ct)
                .ThrowsAsync(new InvalidTransitionException("Library folder contains no eligible top-level video file."));
        }
        else
        {
            importer.ImportAsync(new LibraryPath(folder), ct)
                .ThrowsAsync(new ValidationException(
                    new Dictionary<string, string[]> { ["folders"] = ["bad"] }));
        }

        var handler = new ImportLibraryFoldersCommandHandler(importer, movies, outbox, LibraryRoot);

        // act
        var result = await handler.HandleAsync(
            new ImportLibraryFoldersCommand([folder], RetryFailed: false), ct);

        // assert
        result.Failures.ShouldHaveSingleItem().Reason.Code.ShouldBe(expectedCode);
    }

    [Fact]
    public async Task HandleAsync_RetryFailed_RequeuesOnlyFailedMoviesWithOutboxStaged()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMoviePathsAsync(ct).Returns(new List<LibraryPath>());
        var failed = MovieAt(1, EnrichmentStatus.Failed);
        var pending = MovieAt(2, EnrichmentStatus.Pending);
        movies.FindFailedMoviesAsync(ct).Returns([failed, pending]);
        var handler = new ImportLibraryFoldersCommandHandler(importer, movies, outbox, LibraryRoot);

        // act
        var result = await handler.HandleAsync(
            new ImportLibraryFoldersCommand([], RetryFailed: true), ct);

        // assert
        result.Retried.ShouldBe(1);
        failed.Status.ShouldBe(EnrichmentStatus.Pending);
        pending.Status.ShouldBe(EnrichmentStatus.Pending);
        outbox.Received(1).Stage(new EnrichmentRequested(failed.Id, 1));
        outbox.DidNotReceive().Stage(new EnrichmentRequested(pending.Id, 1));
        await movies.Received(1).SaveChangesAsync(ct);
    }

    [Fact]
    public async Task HandleAsync_DuplicateFolderInRequest_ImportsOnceAndSkipsRepeat()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMoviePathsAsync(ct).Returns(new List<LibraryPath>());
        importer.ImportAsync(new LibraryPath("Heat"), ct).Returns(new MovieId(1));
        var handler = new ImportLibraryFoldersCommandHandler(importer, movies, outbox, LibraryRoot);

        // act
        var result = await handler.HandleAsync(
            new ImportLibraryFoldersCommand(["Heat", "heat"], RetryFailed: false), ct);

        // assert
        result.Imported.ShouldBe(1);
        result.Skipped.ShouldBe(1);
        result.Failures.ShouldBeEmpty();
        await importer.Received(1).ImportAsync(Arg.Any<LibraryPath>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CancelledDuringImport_PropagatesInsteadOfRecordingFailure()
    {
        // arrange
        using var source = new CancellationTokenSource();
        var token = source.Token;
        Action cancel = source.Cancel;
        movies.SnapshotMoviePathsAsync(Arg.Any<CancellationToken>()).Returns(new List<LibraryPath>());
        importer.ImportAsync(Arg.Any<LibraryPath>(), Arg.Any<CancellationToken>())
            .Returns<MovieId>(_ =>
            {
                cancel();
                throw new OperationCanceledException("cancelled");
            });
        var handler = new ImportLibraryFoldersCommandHandler(importer, movies, outbox, LibraryRoot);

        // act
        var act = () => handler.HandleAsync(
            new ImportLibraryFoldersCommand(["Heat"], RetryFailed: false), token);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task HandleAsync_SameFolderDifferentSpelling_ImportsOnceAndSkipsRepeat()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMoviePathsAsync(ct).Returns(new List<LibraryPath>());
        importer.ImportAsync(new LibraryPath("Heat"), ct).Returns(new MovieId(1));
        var handler = new ImportLibraryFoldersCommandHandler(importer, movies, outbox, LibraryRoot);

        // act
        var result = await handler.HandleAsync(
            new ImportLibraryFoldersCommand(["Heat/", "Heat"], RetryFailed: false), ct);

        // assert
        result.Imported.ShouldBe(1);
        result.Skipped.ShouldBe(1);
        result.Failures.ShouldBeEmpty();
        await importer.Received(1).ImportAsync(Arg.Any<LibraryPath>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FaultAfterAnotherPassImported_CountsAsSkippedAndContinues()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var stored = new List<LibraryPath>();
        movies.SnapshotMoviePathsAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<LibraryPath>>([.. stored]));
        importer.ImportAsync(new LibraryPath("Heat"), ct)
            .Returns<MovieId>(_ =>
            {
                stored.Add(new LibraryPath("Heat"));
                throw new InvalidOperationException("duplicate key value violates unique constraint");
            });
        importer.ImportAsync(new LibraryPath("Bravo"), ct).Returns(new MovieId(2));
        var handler = new ImportLibraryFoldersCommandHandler(importer, movies, outbox, LibraryRoot);

        // act
        var result = await handler.HandleAsync(
            new ImportLibraryFoldersCommand(["Heat", "Bravo"], RetryFailed: false), ct);

        // assert
        result.Imported.ShouldBe(1);
        result.Skipped.ShouldBe(1);
        result.Failures.ShouldBeEmpty();
        await importer.Received(1).ImportAsync(new LibraryPath("Bravo"), ct);
    }

    [Fact]
    public async Task HandleAsync_InvalidFolderInRequest_RecordsInvalidFolderAndContinues()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMoviePathsAsync(ct).Returns(new List<LibraryPath>());
        importer.ImportAsync(new LibraryPath("Heat"), ct).Returns(new MovieId(1));
        var handler = new ImportLibraryFoldersCommandHandler(importer, movies, outbox, LibraryRoot);

        // act
        var result = await handler.HandleAsync(
            new ImportLibraryFoldersCommand(["../secret", "Heat"], RetryFailed: false), ct);

        // assert
        result.Imported.ShouldBe(1);
        result.Failures.Select(failure => failure.Folder).ShouldBe(["../secret"]);
        result.Failures.Select(failure => failure.Reason).ShouldBe([ImportFolderFailureReason.InvalidFolder]);
        await importer.Received(1).ImportAsync(new LibraryPath("Heat"), ct);
    }

    [Fact]
    public async Task HandleAsync_AbsolutePathAlreadyImported_SkipsCanonicalIdentity()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMoviePathsAsync(ct).Returns(new List<LibraryPath> { new("Alpha") });
        var handler = new ImportLibraryFoldersCommandHandler(importer, movies, outbox, LibraryRoot);

        // act
        var result = await handler.HandleAsync(
            new ImportLibraryFoldersCommand(["C:/library/Alpha"], RetryFailed: false), ct);

        // assert
        result.Imported.ShouldBe(0);
        result.Skipped.ShouldBe(1);
        result.Failures.ShouldBeEmpty();
        await importer.DidNotReceive().ImportAsync(Arg.Any<LibraryPath>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AbsolutePath_ImportsCanonicalRelativeIdentity()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMoviePathsAsync(ct).Returns(new List<LibraryPath>());
        importer.ImportAsync(new LibraryPath("Alpha"), ct).Returns(new MovieId(1));
        var handler = new ImportLibraryFoldersCommandHandler(importer, movies, outbox, LibraryRoot);

        // act
        var result = await handler.HandleAsync(
            new ImportLibraryFoldersCommand(["C:/library/Alpha"], RetryFailed: false), ct);

        // assert
        result.Imported.ShouldBe(1);
        result.Failures.ShouldBeEmpty();
        await importer.Received(1).ImportAsync(new LibraryPath("Alpha"), ct);
    }

    [Fact]
    public async Task HandleAsync_AbsolutePathRacedAfterFault_CountsAsSkipped()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var stored = new List<LibraryPath>();
        movies.SnapshotMoviePathsAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<LibraryPath>>([.. stored]));
        importer.ImportAsync(new LibraryPath("Alpha"), ct)
            .Returns<MovieId>(_ =>
            {
                stored.Add(new LibraryPath("Alpha"));
                throw new InvalidOperationException("duplicate key value violates unique constraint");
            });
        var handler = new ImportLibraryFoldersCommandHandler(importer, movies, outbox, LibraryRoot);

        // act
        var result = await handler.HandleAsync(
            new ImportLibraryFoldersCommand(["C:/library/Alpha"], RetryFailed: false), ct);

        // assert
        result.Imported.ShouldBe(0);
        result.Skipped.ShouldBe(1);
        result.Failures.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_DuplicatePathConflictFromImporter_CountsAsSkippedAndKeepsRemainingItems()
    {
        // arrange: the durable uniqueness backstop rejects the second path with the existing
        // duplicate-path translation, which must read as skipped/already-present, not a failure.
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMoviePathsAsync(ct).Returns(new List<LibraryPath>());
        importer.ImportAsync(new LibraryPath("Heat"), ct)
            .ThrowsAsync(new DuplicateLibraryPathException("A movie already holds this library path."));
        importer.ImportAsync(new LibraryPath("Bravo"), ct).Returns(new MovieId(2));
        var handler = new ImportLibraryFoldersCommandHandler(importer, movies, outbox, LibraryRoot);

        // act
        var result = await handler.HandleAsync(
            new ImportLibraryFoldersCommand(["Heat", "Bravo"], RetryFailed: false), ct);

        // assert
        result.Imported.ShouldBe(1);
        result.Skipped.ShouldBe(1);
        result.Failures.ShouldBeEmpty();
        outbox.DidNotReceive().Stage(Arg.Any<EnrichmentRequested>());
        await importer.Received(1).ImportAsync(new LibraryPath("Bravo"), ct);
    }

    [Fact]
    public async Task HandleAsync_TwoNewPathsSharingAKnownImdb_BothImportUnderExistingSemantics()
    {
        // arrange: import carries no incoming IMDb identity (T001 evidence section 2b), so two new
        // paths cannot be compared at import. Both rows are created and the collision is settled by
        // enrichment; nothing is dropped and the remaining items still process.
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMoviePathsAsync(ct).Returns(new List<LibraryPath> { new("Inception (2010) [1080p]") });
        importer.ImportAsync(new LibraryPath("Inception[2010]"), ct).Returns(new MovieId(11));
        importer.ImportAsync(new LibraryPath("Inception 2010 1080p"), ct).Returns(new MovieId(12));
        var handler = new ImportLibraryFoldersCommandHandler(importer, movies, outbox, LibraryRoot);

        // act
        var result = await handler.HandleAsync(
            new ImportLibraryFoldersCommand(["Inception[2010]", "Inception 2010 1080p"], RetryFailed: false), ct);

        // assert
        result.Imported.ShouldBe(2);
        result.Skipped.ShouldBe(0);
        result.Failures.ShouldBeEmpty();
        await importer.Received(2).ImportAsync(Arg.Any<LibraryPath>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RetryFailed_RequeuesNonRetryableDuplicateFailedRowWithoutAddingMovies()
    {
        // arrange: retry stays available independently of duplicate skipping; a non-retryable
        // duplicate_imdb_id row is re-requested, no movie is created and nothing loops.
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMoviePathsAsync(ct).Returns(new List<LibraryPath>());
        var duplicate = MovieAt(7, EnrichmentStatus.Pending);
        duplicate.MarkFailed(EnrichmentFailureCategory.DuplicateImdbId, DateTimeOffset.UnixEpoch);
        movies.FindFailedMoviesAsync(ct).Returns([duplicate]);
        var handler = new ImportLibraryFoldersCommandHandler(importer, movies, outbox, LibraryRoot);

        // act
        var result = await handler.HandleAsync(
            new ImportLibraryFoldersCommand([], RetryFailed: true), ct);

        // assert
        result.Retried.ShouldBe(1);
        result.Imported.ShouldBe(0);
        duplicate.Status.ShouldBe(EnrichmentStatus.Pending);
        outbox.Received(1).Stage(new EnrichmentRequested(duplicate.Id, 1));
        await importer.DidNotReceive().ImportAsync(Arg.Any<LibraryPath>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Validator_EmptyFoldersWithoutRetry_Fails()
    {
        // arrange
        var validator = new ImportLibraryFoldersCommandValidator();

        // act
        var result = await validator.ValidateAsync(
            new ImportLibraryFoldersCommand([], RetryFailed: false),
            TestContext.Current.CancellationToken);

        // assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Validator_RetryOnly_Passes()
    {
        // arrange
        var validator = new ImportLibraryFoldersCommandValidator();

        // act
        var result = await validator.ValidateAsync(
            new ImportLibraryFoldersCommand([], RetryFailed: true),
            TestContext.Current.CancellationToken);

        // assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Validator_BadFolder_Fails()
    {
        // arrange
        var validator = new ImportLibraryFoldersCommandValidator();

        // act
        var result = await validator.ValidateAsync(
            new ImportLibraryFoldersCommand(["../secret"], RetryFailed: false),
            TestContext.Current.CancellationToken);

        // assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Validator_DefaultFoldersWithoutRetry_FailsInsteadOfThrowing()
    {
        // arrange
        var validator = new ImportLibraryFoldersCommandValidator();

        // act
        var result = await validator.ValidateAsync(
            new ImportLibraryFoldersCommand(default, RetryFailed: false),
            TestContext.Current.CancellationToken);

        // assert
        result.IsValid.ShouldBeFalse();
    }

    private static Movie MovieAt(int id, EnrichmentStatus status) =>
        Movie.Rehydrate(
            new MovieId(id),
            "Heat",
            new LibraryPath($"Heat{id}"),
            new MediaFormat("mkv"),
            isInWatchlist: false,
            metadata: null,
            status: status,
            enrichedAt: null,
            enrichmentAttempts: 1,
            lastFailureCategory: null,
            lastAttemptAt: null);

    private sealed class ImportCountCapture : IDisposable
    {
        private readonly ConcurrentQueue<CapturedImport> measurements = [];
        private readonly MeterListener listener = new();

        public ImportCountCapture()
        {
            listener.SetMeasurementEventCallback<long>(
                (instrument, value, tags, _) => measurements.Enqueue(new(instrument.Name, value, tags.ToArray())));
            listener.InstrumentPublished = (instrument, publishedListener) =>
            {
                if (string.Equals(instrument.Meter.Name, MeterName, StringComparison.Ordinal)
                    && string.Equals(instrument.Name, ImportInstrumentName, StringComparison.Ordinal))
                {
                    publishedListener.EnableMeasurementEvents(instrument);
                }
            };
            listener.Start();
        }

        public IReadOnlyList<CapturedImport> Measurements => [.. measurements];

        public void Dispose() => listener.Dispose();
    }

    // ReSharper disable NotAccessedPositionalProperty.Local // Captured by the meter listener; only counts are asserted.
    private sealed record CapturedImport(string InstrumentName, long Value, KeyValuePair<string, object?>[] Tags);
    // ReSharper restore NotAccessedPositionalProperty.Local
}
