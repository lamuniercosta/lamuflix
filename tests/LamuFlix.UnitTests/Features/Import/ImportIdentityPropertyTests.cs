using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Import;

/// <summary>
/// DEV-522 T013: the identity and idempotency invariants of the import orchestration, in memory.
/// Library path/key is the only identity an import operation carries (T001 evidence 2b), so every
/// property here is about a known path: a repeated or re-entrant import of the same library path
/// leaves one row and one enrichment enqueue, and a case variant of a stored key is the same
/// identity. Real query and constraint behaviour belongs to the Testcontainers regressions (T003),
/// never to these properties.
/// </summary>
public sealed class ImportIdentityPropertyTests
{
    private const string Root = "C:/library";
    private const int MaxFolders = 6;

    private static readonly string[] Palette = ["Heat", "Inception", "Alien", "Dune"];

    /// <summary>
    /// Importing any sequence of folders, where a folder may repeat and may differ only by case,
    /// persists exactly one row per distinct identity and enqueues exactly one enrichment per
    /// distinct identity. Every repeat is the existing already-present skip and never a second
    /// row, a second enqueue, or a second save.
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool HandleAsync_AnySequenceOfFolders_PersistsOneRowAndOneEnqueuePerDistinctPath(int[]? picks)
    {
        var folders = Folders(picks);
        var subject = new SingleFolderSubject();
        var distinct = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var folder in folders)
        {
            var firstImport = distinct.Add(folder);
            var asExpected = firstImport
                ? subject.ImportSucceeds(folder)
                : subject.ImportIsSkipped(folder);

            if (!asExpected)
            {
                return false;
            }
        }

        return subject.AddedPaths.Count == distinct.Count
            && subject.EnqueuedMovieIds.Distinct().Count() == distinct.Count
            && subject.SaveCount == distinct.Count;
    }

    /// <summary>
    /// A bulk request that repeats paths, including case variants, imports each distinct path exactly
    /// once and reports every repeat as the existing skipped outcome, with no failure and no second
    /// importer call for the same identity.
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool HandleAsync_BulkRequestWithRepeatedPaths_ImportsEachDistinctPathOnceAndSkipsTheRepeats(int[]? picks)
    {
        var folders = Folders(picks);
        var subject = new BulkSubject([]);
        var result = subject.Run(folders, retryFailed: false);
        var distinct = folders.Distinct(StringComparer.OrdinalIgnoreCase).Count();

        return result.Imported == distinct
            && result.Skipped == folders.Length - distinct
            && result.Retried == 0
            && result.Failures.IsEmpty
            && subject.ImportedPaths.Count == distinct
            && subject.ImportedPaths.Distinct(StringComparer.OrdinalIgnoreCase).Count() == distinct;
    }

    /// <summary>
    /// Two spellings of the same folder — differing only in case, a leading current-directory segment,
    /// a trailing separator, or being written as an absolute path under the configured root — resolve to
    /// one identity: the second import is the existing already-present skip, leaving one row, one
    /// enrichment enqueue and one save. No new path normalization is implied; every shape here is one
    /// the existing canonicalization already agrees on.
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool HandleAsync_EquivalentSpellingsOfOneFolder_AreOneIdentity(int seed)
    {
        var name = Palette[(int)((uint)seed % (uint)Palette.Length)];
        var shapes = ShapesOf(name);
        var subject = new SingleFolderSubject();

        for (var index = 0; index < shapes.Length; index++)
        {
            var asExpected = index == 0
                ? subject.ImportSucceeds(shapes[index])
                : subject.ImportIsSkipped(shapes[index]);

            if (!asExpected)
            {
                return false;
            }
        }

        return subject.AddedPaths.Count == 1
            && subject.EnqueuedMovieIds.Count == 1
            && subject.SaveCount == 1;
    }

    /// <summary>
    /// Retrying failed enrichments stays independent of duplicate skipping: each Failed row is
    /// requeued once and moves back to Pending, no movie is added, and a second retry run stages
    /// nothing further for rows already requeued.
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool HandleAsync_RetryFailedRepeatedly_StagesOneEnqueuePerRowAndNeverAddsMovies(int countSeed)
    {
        var count = (int)((uint)countSeed % 5);
        var failed = Enumerable.Range(1, count).Select(FailedMovie).ToList();
        var subject = new BulkSubject(failed);

        var first = subject.Run([], retryFailed: true);
        var firstEnqueueCount = subject.Enqueued.Count;
        var statusesAfterFirstRun = failed.Select(movie => movie.Status).ToList();
        var second = subject.Run([], retryFailed: true);

        return first.Retried == count
            && firstEnqueueCount == count
            && subject.Enqueued.All(request => request.Attempt == 1)
            && subject.Enqueued.Select(request => request.MovieId).Distinct().Count() == count
            && statusesAfterFirstRun.All(status => ReferenceEquals(status, EnrichmentStatus.Pending))
            && second.Retried == 0
            && subject.Enqueued.Count == firstEnqueueCount
            && subject.AddCount == 0;
    }

    private static string[] Folders(int[]? picks)
    {
        var supplied = picks ?? [];
        var length = Math.Min(supplied.Length, MaxFolders);
        var folders = new string[length];

        for (var index = 0; index < length; index++)
        {
            var pick = (uint)supplied[index];
            var name = Palette[(int)(pick % (uint)Palette.Length)];
            folders[index] = ((pick >> 2) & 1) == 1 ? name.ToUpperInvariant() : name;
        }

        return folders;
    }

    /// <summary>
    /// Equivalent spellings of one folder under the configured root. The first entry is the spelling
    /// stored by the first import; the rest must be recognized as the same identity by the existing
    /// canonicalization and key comparison.
    /// </summary>
    private static string[] ShapesOf(string name) =>
    [
        $"{Root}/{name}",
        $"C:\\library\\{name}",
        $"{Root}/./{name}",
        $"{Root}/{name}/",
    ];

    private static Movie FailedMovie(int id) => Movie.Rehydrate(
        new MovieId(id),
        "Heat",
        new LibraryPath($"Heat{id}"),
        new MediaFormat("mkv"),
        isInWatchlist: false,
        metadata: null,
        status: EnrichmentStatus.Failed,
        enrichedAt: null,
        enrichmentAttempts: 1,
        lastFailureCategory: EnrichmentFailureCategory.DuplicateImdbId,
        lastAttemptAt: null);

    private static IOptions<LibraryOptions> Library() =>
        Microsoft.Extensions.Options.Options.Create(new LibraryOptions { RootPath = Root });

    /// <summary>Drives the single-folder import with a repository that remembers what it stored.</summary>
    private sealed class SingleFolderSubject
    {
        private readonly List<LibraryPath> stored = [];
        private readonly List<LibraryPath> added = [];
        private readonly List<MovieId> enqueued = [];
        private int nextId;
        private int saveCount;

        public SingleFolderSubject()
        {
            var scanner = Substitute.For<IMediaLibraryScanner>();
            var movies = Substitute.For<IMovieRepository>();
            var outbox = Substitute.For<IEnrichmentOutbox>();

            scanner.Scan(Arg.Any<LibraryPath>()).Returns(call => Scanned(call.Arg<LibraryPath>()));
            movies.SnapshotMoviePathsAsync(Arg.Any<CancellationToken>())
                .Returns(_ => (IReadOnlyList<LibraryPath>)[.. stored]);
            movies.NextIdentityAsync(Arg.Any<CancellationToken>())
                .Returns(_ => new MovieId(++nextId));
            movies.AddAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    added.Add(call.Arg<Movie>().Path);
                    stored.Add(call.Arg<Movie>().Path);
                    return Task.CompletedTask;
                });
            movies.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    saveCount++;
                    return Task.CompletedTask;
                });
            outbox.When(stage => stage.Stage(Arg.Any<EnrichmentRequested>()))
                .Do(call => enqueued.Add(call.Arg<EnrichmentRequested>().MovieId));

            Handler = new ImportMovieFolderCommandHandler(scanner, movies, outbox, Library());
        }

        public IReadOnlyList<LibraryPath> AddedPaths => added;

        public IReadOnlyList<MovieId> EnqueuedMovieIds => enqueued;

        public int SaveCount => saveCount;

        private ImportMovieFolderCommandHandler Handler { get; }

        public bool ImportSucceeds(string folder)
        {
            var adds = added.Count;
            var enqueues = enqueued.Count;
            var saves = saveCount;
            var canonical = Canonical(folder);

            var id = Handler
                .HandleAsync(new ImportMovieFolderCommand(new LibraryPath(folder)), CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            return added.Count == adds + 1
                && added[^1] == canonical
                && enqueued.Count == enqueues + 1
                && enqueued[^1] == id
                && saveCount == saves + 1;
        }

        public bool ImportIsSkipped(string folder)
        {
            var adds = added.Count;
            var enqueues = enqueued.Count;
            var saves = saveCount;

            try
            {
                Handler
                    .HandleAsync(new ImportMovieFolderCommand(new LibraryPath(folder)), CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (DuplicateLibraryPathException)
            {
                return added.Count == adds && enqueued.Count == enqueues && saveCount == saves;
            }

            return false;
        }

        private static ScannedMovie Scanned(LibraryPath folder) =>
            new(folder, folder.Value, new MediaFormat("mkv"), null, 1024L);

        private static LibraryPath Canonical(string folder) =>
            LibraryPath.TryCanonicalizeUnderRoot(folder, Root, out var canonical)
                ? new LibraryPath(canonical)
                : throw new ArgumentException($"Not a library folder: {folder}", nameof(folder));
    }

    /// <summary>Drives the bulk import with a recorder importer and a repository that never persists.</summary>
    private sealed class BulkSubject
    {
        private readonly List<string> imported = [];
        private readonly List<EnrichmentRequested> enqueued = [];
        private int addCount;

        public BulkSubject(IReadOnlyList<Movie> failed)
        {
            var importer = Substitute.For<IMovieFolderImporter>();
            var movies = Substitute.For<IMovieRepository>();
            var outbox = Substitute.For<IEnrichmentOutbox>();

            movies.SnapshotMoviePathsAsync(Arg.Any<CancellationToken>())
                .Returns(_ => (IReadOnlyList<LibraryPath>)[]);
            movies.FindFailedMoviesAsync(Arg.Any<CancellationToken>())
                .Returns(_ => failed);
            movies.AddAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    addCount++;
                    return Task.CompletedTask;
                });
            importer.ImportAsync(Arg.Any<LibraryPath>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    imported.Add(call.Arg<LibraryPath>().Value);
                    return new MovieId(imported.Count);
                });
            outbox.When(stage => stage.Stage(Arg.Any<EnrichmentRequested>()))
                .Do(call => enqueued.Add(call.Arg<EnrichmentRequested>()));

            Handler = new ImportLibraryFoldersCommandHandler(importer, movies, outbox, Library());
        }

        public IReadOnlyList<string> ImportedPaths => imported;

        public IReadOnlyList<EnrichmentRequested> Enqueued => enqueued;

        public int AddCount => addCount;

        private ImportLibraryFoldersCommandHandler Handler { get; }

        public ImportLibraryFoldersResult Run(string[] folders, bool retryFailed) =>
            Handler
                .HandleAsync(new ImportLibraryFoldersCommand([.. folders], retryFailed), CancellationToken.None)
                .GetAwaiter()
                .GetResult();
    }
}
