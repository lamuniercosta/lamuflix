using System;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Playback;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(ApiEndToEndCollection))]
public sealed class PlaybackHistoryEndpointTests(ApiEndToEndFixture fixture) : IAsyncLifetime
{
    private const string PlayerPath = "C:/players/vlc.exe";
    private const string ProblemDetailsJson = "application/problem+json";

    private static readonly DateTimeOffset PlayedAtUtc =
        new(2026, 10, 18, 0, 0, 0, TimeSpan.Zero);

    private readonly IMediaLibraryScanner scanner = ResolvingScanner();

    public async ValueTask InitializeAsync() =>
        await fixture.PrepareAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task PlayMovie_LocalPlayEnabled_PersistsExactlyOneEventForTheRequest()
    {
        // arrange
        var starter = Substitute.For<IProcessStarter>();
        var clock = new FixedTimeProvider(PlayedAtUtc);
        await using var host = CreateHost(clock, services =>
        {
            services.RemoveAll<IProcessStarter>();
            services.AddSingleton(starter);
            EnableLocalPlay(services);
        });
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var movie = await SeedAsync("heat");
        var id = movie.Id.ShouldNotBeNull().Value;
        ResolveRealHistory(host).ShouldBeOfType<EfPlaybackHistory>();

        // act
        var response = await client.PostAsync(PlayRoute(id), null, cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldBe(string.Empty);
        starter.Received(1).Start(Arg.Is<ProcessStartInfo>(
            info => info.FileName == PlayerPath && info.ArgumentList.Count == 1));
        var events = await ReadEventsAsync(id);
        events.ShouldHaveSingleItem();
        events[0].MovieId.Value.ShouldBe(id);
        events[0].PlayedAt.ShouldBe(PlayedAtUtc);
        events[0].PlayedAt.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public async Task PlayMovie_LocalPlayDisabled_ReturnsForbiddenWithoutProcessOrEvent()
    {
        // arrange
        var starter = Substitute.For<IProcessStarter>();
        var clock = new FixedTimeProvider(PlayedAtUtc);
        await using var host = CreateHost(clock, services =>
        {
            services.RemoveAll<IProcessStarter>();
            services.AddSingleton(starter);
        });
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var movie = await SeedAsync("disabled");
        var id = movie.Id.ShouldNotBeNull().Value;

        // act
        var response = await client.PostAsync(PlayRoute(id), null, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        problem.GetProperty("title").GetString().ShouldBe("Forbidden");
        problem.TryGetProperty("detail", out _).ShouldBeFalse();
        body.ShouldNotContain("LocalPlay is disabled");
        body.ShouldNotContain(nameof(FeatureDisabledException));
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
        (await ReadEventsAsync(id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task PlayMovie_SelectedFileMissing_ReturnsSafeNotFoundWithoutProcessStartOrEvent()
    {
        // arrange
        var starter = Substitute.For<IProcessStarter>();
        var clock = new FixedTimeProvider(PlayedAtUtc);
        await using var host = CreateHost(clock, services =>
        {
            services.RemoveAll<IProcessStarter>();
            services.AddSingleton(starter);
            EnableLocalPlayWithoutExistingFile(services);
        });
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var movie = await SeedAsync("missing-selected-file");
        var id = movie.Id.ShouldNotBeNull().Value;

        // act
        var response = await client.PostAsync(PlayRoute(id), null, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        problem.GetProperty("title").GetString().ShouldBe("Not Found");
        problem.TryGetProperty("detail", out _).ShouldBeFalse();
        body.ShouldNotContain("Not found.");
        body.ShouldNotContain(nameof(NotFoundException));
        body.ShouldNotContain("primary.mkv");
        body.ShouldNotContain(PlayerPath);
        body.ShouldNotContain("127.0.0.1");
        body.ShouldNotContain("X-Forwarded-For");
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
        (await ReadEventsAsync(id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task PlayMovie_AppendFailsAfterLaunch_DoesNotReturnNoContentAndDoesNotPersist()
    {
        // arrange
        var starter = Substitute.For<IProcessStarter>();
        var history = Substitute.For<IPlaybackHistory>();
        history.RecordAsync(Arg.Any<PlaybackEvent>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("append failed after launch")));
        var clock = new FixedTimeProvider(PlayedAtUtc);
        await using var host = CreateHost(clock, services =>
        {
            services.RemoveAll<IProcessStarter>();
            services.AddSingleton(starter);
            services.RemoveAll<IPlaybackHistory>();
            services.AddSingleton(history);
            EnableLocalPlay(services);
        });
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var movie = await SeedAsync("append-failure");
        var id = movie.Id.ShouldNotBeNull().Value;

        // act
        var response = await client.PostAsync(PlayRoute(id), null, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        // assert
        response.StatusCode.ShouldNotBe(HttpStatusCode.NoContent);
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        payload.ShouldNotContain("append failed after launch");
        starter.Received(1).Start(Arg.Any<ProcessStartInfo>());
        await history.Received(1).RecordAsync(
            Arg.Any<PlaybackEvent>(), Arg.Any<System.Threading.CancellationToken>());
        (await ReadEventsAsync(id)).ShouldBeEmpty();
    }

    private static string PlayRoute(int id) => $"/api/movies/{id}/play";

    private ApiEndToEndFactory CreateHost(TimeProvider clock, Action<IServiceCollection> configureTestServices) =>
        new(fixture.HostSettings(clock), services =>
        {
            services.RemoveAll<IMediaLibraryScanner>();
            services.AddSingleton(scanner);
            configureTestServices(services);
        });

    private static IMediaLibraryScanner ResolvingScanner()
    {
        var stub = Substitute.For<IMediaLibraryScanner>();
        stub.ResolvePrimaryVideo(Arg.Any<LibraryPath>())
            .Returns(call => new PrimaryVideo(
                new LibraryPath($"{call.Arg<LibraryPath>().Value}/primary.mkv"),
                new MediaFormat("mkv")));
        return stub;
    }

    private static IPlaybackHistory ResolveRealHistory(ApiEndToEndFactory host)
    {
        using var scope = host.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IPlaybackHistory>();
    }

    private static void EnableLocalPlay(IServiceCollection services) =>
        EnableLocalPlay(services, ExistingFiles());

    private static void EnableLocalPlayWithoutExistingFile(IServiceCollection services) =>
        EnableLocalPlay(services, MissingFiles());

    private static void EnableLocalPlay(IServiceCollection services, IFileSystem fileSystem)
    {
        services.RemoveAll<IFileSystem>();
        services.AddSingleton(fileSystem);
        services.RemoveAll<IOptions<FeatureOptions>>();
        services.AddSingleton(
            Options.Create(new FeatureOptions { LocalPlay = true }));
        services.RemoveAll<IOptions<PlaybackOptions>>();
        services.AddSingleton(
            Options.Create(new PlaybackOptions
            {
                Players =
                [
                    new PlayerConfig
                    {
                        Name = "vlc",
                        ExecutablePath = PlayerPath,
                        Formats = ["mkv"],
                    },
                ],
            }));
    }

    private static IFileSystem ExistingFiles()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        var file = Substitute.For<IFile>();
        file.Exists(Arg.Any<string>()).Returns(true);
        fileSystem.File.Returns(file);
        return fileSystem;
    }

    private static IFileSystem MissingFiles()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        var file = Substitute.For<IFile>();
        file.Exists(Arg.Any<string>()).Returns(false);
        fileSystem.File.Returns(file);
        return fileSystem;
    }

    private async Task<MovieRecord> SeedAsync(string pathSuffix)
    {
        await using var context = fixture.Postgres.CreateMigratedContext();
        return await MovieCatalogSeed.AddAsync(
            context,
            MovieCatalogSeed.Create("Playback History Endpoint", pathSuffix),
            TestContext.Current.CancellationToken);
    }

    private async Task<PlaybackEventRecord[]> ReadEventsAsync(int movieId)
    {
        await using var context = fixture.Postgres.CreateMigratedContext();
        var id = new MovieId(movieId);
        return await context.PlaybackEvents
            .AsNoTracking()
            .Where(playbackEvent => playbackEvent.MovieId == id)
            .OrderBy(playbackEvent => playbackEvent.Id)
            .ToArrayAsync(TestContext.Current.CancellationToken);
    }
}
