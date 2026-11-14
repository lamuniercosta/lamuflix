using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
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
/// DEV-333 US1: the real bounded handler, real filesystem discovery, real per-folder scopes and
/// real PostgreSQL persistence produce exactly one movie and one outbox row per candidate.
/// </summary>
[Collection(nameof(PostgresCollection))]
public sealed class ImportLibraryCommandHandlerPersistenceTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions WebDefaults = new(JsonSerializerDefaults.Web);

    public async ValueTask InitializeAsync() =>
        await postgres.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task HandleAsync_NestedCandidates_PersistsDistinctMoviesAndOutboxRowsOnceEach()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateLibrary();
        try
        {
            WriteVideo(root, "root");
            WriteVideo(Path.Combine(root, "Parent"), "parent");
            WriteVideo(Path.Combine(root, "Parent", "Child"), "child");
            Directory.CreateDirectory(Path.Combine(root, "Container"));
            var expectedPaths = new HashSet<string>(StringComparer.Ordinal)
            {
                ".",
                "Parent",
                "Parent/Child",
            };
            await using var provider = BuildProvider(root, maxConcurrency: 2);
            using var scope = provider.CreateScope();
            var handler = scope.ServiceProvider
                .GetRequiredService<ICommandHandler<ImportLibraryCommand, ImportLibraryResult>>();

            // act
            var result = await handler.HandleAsync(new ImportLibraryCommand(), cancellationToken);

            // assert
            result.ImportedCount.ShouldBe(3);
            await using var database = postgres.CreateMigratedContext();
            var movies = await database.Movies.AsNoTracking().ToListAsync(cancellationToken);
            movies.Count.ShouldBe(3);
            movies.Select(movie => movie.LibraryPath.Value).ShouldBe(expectedPaths, ignoreOrder: true);
            movies.ShouldAllBe(movie => movie.LibraryPathKey == movie.LibraryPath.Value.ToUpperInvariant());
            var outbox = await database.OutboxMessages.AsNoTracking().ToListAsync(cancellationToken);
            outbox.Count.ShouldBe(3);
            var requests = outbox
                .Select(message => JsonSerializer.Deserialize<EnrichmentRequested>(message.Payload, WebDefaults))
                .OfType<EnrichmentRequested>()
                .ToList();
            requests.Count.ShouldBe(3);
            requests.ShouldAllBe(request => request.Attempt == 1);
            requests.Select(request => request.MovieId).Distinct().Count().ShouldBe(3);
            requests.Select(request => request.MovieId)
                .ShouldBe(movies.Select(movie => movie.Id), ignoreOrder: true);
        }
        finally
        {
            DeleteLibrary(root);
        }
    }

    [Fact]
    public async Task HandleAsync_FreshRerunDuplicateLibraryPath_FaultsOnRealDatabase()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateLibrary();
        try
        {
            WriteVideo(root, "root");
            WriteVideo(Path.Combine(root, "Parent"), "parent");
            WriteVideo(Path.Combine(root, "Parent", "Child"), "child");
            await using var provider = BuildProvider(root, maxConcurrency: 2);
            using (var firstScope = provider.CreateScope())
            {
                var first = firstScope.ServiceProvider
                    .GetRequiredService<ICommandHandler<ImportLibraryCommand, ImportLibraryResult>>();
                var firstResult = await first.HandleAsync(new ImportLibraryCommand(), cancellationToken);
                firstResult.ImportedCount.ShouldBe(3);
            }

            using var secondScope = provider.CreateScope();
            var second = secondScope.ServiceProvider
                .GetRequiredService<ICommandHandler<ImportLibraryCommand, ImportLibraryResult>>();

            // act
            await Should.ThrowAsync<DuplicateLibraryPathException>(
                () => second.HandleAsync(new ImportLibraryCommand(), cancellationToken));

            // assert
            await using var database = postgres.CreateMigratedContext();
            (await database.Movies.AsNoTracking().CountAsync(cancellationToken)).ShouldBe(3);
            (await database.OutboxMessages.AsNoTracking().CountAsync(cancellationToken)).ShouldBe(3);
        }
        finally
        {
            DeleteLibrary(root);
        }
    }

    private ServiceProvider BuildProvider(string root, int maxConcurrency)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IFileSystem>(new FileSystem());
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(Substitute.For<IEnrichmentStatusNotifier>());
        services.AddSingleton(
            Options.Create(new EnrichmentOptions
            {
                MaxAttempts = 3,
                ClaimLease = TimeSpan.FromSeconds(5),
            }));
        services.AddSingleton(
            Options.Create(new LibraryOptions { RootPath = root, MaxConcurrency = maxConcurrency }));
        services.AddDbContext<LamuFlixDbContext>(
            options => options.UseNpgsql(postgres.ConnectionString, database => database.UseVector()));
        services.AddScoped<IMovieRepository, EfMovieRepository>();
        services.AddScoped<IEnrichmentOutbox, EfEnrichmentOutbox>();
        services.AddScoped<IMediaLibraryScanner, DirectoryMediaLibraryScanner>();
        services.AddScoped<ILibraryFolderDiscovery, FileSystemLibraryFolderDiscovery>();
        services.AddScoped<IMovieFolderImporter, ScopedMovieFolderImporter>();
        services.AddHandler<ImportMovieFolderCommandHandler, ImportMovieFolderCommand, MovieId>();
        services.AddHandler<ImportLibraryCommandHandler, ImportLibraryCommand, ImportLibraryResult>();
        return services.BuildServiceProvider();
    }

    private static string CreateLibrary()
    {
        var root = Path.Combine(Path.GetTempPath(), $"lamuflix-dev333-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }

    private static void WriteVideo(string directory, string name)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, $"{name}.mkv"), "video");
    }

    private static void DeleteLibrary(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
