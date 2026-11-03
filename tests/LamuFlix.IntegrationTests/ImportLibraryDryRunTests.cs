using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.FileSystem;
using LamuFlix.Infrastructure.Import;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Pipeline;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

/// <summary>
/// DEV-333 US4 / AC4: the real bulk import handler and the real enrichment settlement handler
/// compose against real PostgreSQL for 500 candidates, and every staged enrichment completes
/// against a fake provider that returns Found.
/// </summary>
[Collection(nameof(PostgresCollection))]
public sealed class ImportLibraryDryRunTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const int CandidateCount = 500;
    private const int MaxConcurrency = 4;
    private static readonly JsonSerializerOptions WebDefaults = new(JsonSerializerDefaults.Web);

    public async ValueTask InitializeAsync() =>
        await postgres.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task DryRun_500Candidates_ImportsAndEnrichesAllWithZeroRateLimitedFailedNotFoundOrDuplicates()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateLibrary();
        try
        {
            for (var index = 0; index < CandidateCount; index++)
            {
                WriteCandidate(root, index);
            }

            var probe = new ConcurrencyProbe();
            var provider = new ScriptedMetadataProvider(FoundForEveryLookup);
            await using var services = BuildProvider(root, probe, provider);

            // act
            long importedCount;
            using (var importScope = services.CreateScope())
            {
                var handler = importScope.ServiceProvider
                    .GetRequiredService<ICommandHandler<ImportLibraryCommand, ImportLibraryResult>>();
                importedCount = (await handler.HandleAsync(new ImportLibraryCommand(), cancellationToken))
                    .ImportedCount;
            }

            // assert
            importedCount.ShouldBe(CandidateCount);
            probe.MaxObserved.ShouldBeLessThanOrEqualTo(MaxConcurrency);
            probe.MaxObserved.ShouldBeGreaterThanOrEqualTo(1);

            var staged = await ReadStagedRequestsAsync(cancellationToken);
            staged.Count.ShouldBe(CandidateCount);
            staged.ShouldAllBe(request => request.Attempt == 1);
            staged.Select(request => request.MovieId).Distinct().Count().ShouldBe(CandidateCount);

            // act
            await Parallel.ForEachAsync(
                staged,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = MaxConcurrency,
                    CancellationToken = cancellationToken,
                },
                async (request, token) =>
                {
                    await using var enrichmentScope = services.CreateAsyncScope();
                    var handler = enrichmentScope.ServiceProvider
                        .GetRequiredService<ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>>();
                    var outcome = await handler.HandleAsync(
                        new ProcessEnrichmentCommand(request.MovieId, request.Attempt),
                        token);
                    outcome.ShouldBe(new ProcessEnrichmentOutcome.Completed(Claimed: true));
                });

            // assert
            await using var database = postgres.CreateMigratedContext();
            var movies = await database.Movies.AsNoTracking().ToListAsync(cancellationToken);
            movies.Count.ShouldBe(CandidateCount);
            movies.ShouldAllBe(movie => movie.Status == EnrichmentStatus.Enriched);
            movies.ShouldAllBe(movie => movie.EnrichmentFailureCategory == null);
            movies.ShouldAllBe(movie => movie.EnrichmentAttempts == 1);
            movies.ShouldAllBe(movie => movie.EnrichmentDeferredUntil == null);
            movies.Select(movie => movie.ImdbId).Distinct().Count().ShouldBe(CandidateCount);
            (await database.OutboxMessages.AsNoTracking().CountAsync(cancellationToken))
                .ShouldBe(CandidateCount);
        }
        finally
        {
            DeleteLibrary(root);
        }
    }

    private static MetadataLookupResult FoundForEveryLookup(MetadataLookup lookup)
    {
        var sequence = Interlocked.Increment(ref foundSequence);
        return new MetadataLookupResult.Found(new MovieMetadata(
            lookup.Title,
            imdbId: new ImdbId($"tt{1_000_000 + sequence:D7}")));
    }

    private static int foundSequence;

    private async Task<IReadOnlyList<EnrichmentRequested>> ReadStagedRequestsAsync(CancellationToken cancellationToken)
    {
        await using var database = postgres.CreateMigratedContext();
        var payloads = await database.OutboxMessages
            .AsNoTracking()
            .Select(message => message.Payload)
            .ToListAsync(cancellationToken);
        return
        [
            .. payloads
                .Select(payload => JsonSerializer.Deserialize<EnrichmentRequested>(payload, WebDefaults))
                .OfType<EnrichmentRequested>(),
        ];
    }

    private ServiceProvider BuildProvider(
        string root,
        ConcurrencyProbe probe,
        ScriptedMetadataProvider provider)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IFileSystem>(new FileSystem());
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(Substitute.For<IEnrichmentStatusNotifier>());
        services.AddSingleton<IMetadataProvider>(provider);
        services.AddSingleton<IEnrichmentQueue>(new RecordingEnrichmentQueue());
        services.AddSingleton(
            Options.Create(new EnrichmentOptions
            {
                MaxAttempts = 3,
                ClaimLease = TimeSpan.FromMinutes(5),
            }));
        services.AddSingleton(
            Options.Create(new LibraryOptions { RootPath = root, MaxConcurrency = MaxConcurrency }));
        services.AddDbContext<LamuFlixDbContext>(
            options => options.UseNpgsql(postgres.ConnectionString, database => database.UseVector()),
            optionsLifetime: ServiceLifetime.Singleton);
        services.AddScoped<IMovieRepository, EfMovieRepository>();
        services.AddScoped<IEnrichmentOutbox, EfEnrichmentOutbox>();
        services.AddScoped<IMediaLibraryScanner, DirectoryMediaLibraryScanner>();
        services.AddScoped<ILibraryFolderDiscovery, FileSystemLibraryFolderDiscovery>();
        services.AddScoped<ScopedMovieFolderImporter>();
        services.AddScoped<IMovieFolderImporter>(serviceProvider => new InstrumentedMovieFolderImporter(
            serviceProvider.GetRequiredService<ScopedMovieFolderImporter>(),
            probe));
        services.AddHandler<ImportMovieFolderCommandHandler, ImportMovieFolderCommand, MovieId>();
        services.AddHandler<ImportLibraryCommandHandler, ImportLibraryCommand, ImportLibraryResult>();
        services.AddHandler<ProcessEnrichmentCommandHandler, ProcessEnrichmentCommand, ProcessEnrichmentOutcome>();
        return services.BuildServiceProvider();
    }

    private static string CreateLibrary()
    {
        var root = Path.Combine(Path.GetTempPath(), $"lamuflix-dev333-dryrun-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }

    private static void WriteCandidate(string root, int index)
    {
        var directory = Path.Combine(root, $"Movie-{index:D3}");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, $"movie-{index:D3}.mkv"), "video");
    }

    private static void DeleteLibrary(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class InstrumentedMovieFolderImporter(
        IMovieFolderImporter inner,
        ConcurrencyProbe probe) : IMovieFolderImporter
    {
        public async Task<MovieId> ImportAsync(LibraryPath folder, CancellationToken cancellationToken)
        {
            probe.Enter();
            try
            {
                return await inner.ImportAsync(folder, cancellationToken);
            }
            finally
            {
                probe.Exit();
            }
        }
    }

    private sealed class ConcurrencyProbe
    {
        private int current;
        private int maxObserved;

        public int MaxObserved => Volatile.Read(ref maxObserved);

        public void Enter()
        {
            var running = Interlocked.Increment(ref current);
            int observed;
            do
            {
                observed = Volatile.Read(ref maxObserved);
                if (running <= observed)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(ref maxObserved, running, observed) != observed);
        }

        public void Exit() => Interlocked.Decrement(ref current);
    }

    private sealed class RecordingEnrichmentQueue : IEnrichmentQueue
    {
        public Task EnqueueAsync(EnrichmentRequested message, CancellationToken ct) => Task.CompletedTask;

        public Task EnqueueAsync(FeaturesRequested message, CancellationToken ct) => Task.CompletedTask;
    }
}
