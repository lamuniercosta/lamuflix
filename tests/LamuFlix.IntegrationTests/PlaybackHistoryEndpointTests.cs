using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
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

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
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
        new(fixture.HostSettings(clock), configureTestServices);

    private static IPlaybackHistory ResolveRealHistory(ApiEndToEndFactory host)
    {
        using var scope = host.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IPlaybackHistory>();
    }

    private static void EnableLocalPlay(IServiceCollection services)
    {
        services.RemoveAll<IOptions<FeatureOptions>>();
        services.AddSingleton<IOptions<FeatureOptions>>(
            Options.Create(new FeatureOptions { LocalPlay = true }));
        services.RemoveAll<IOptions<PlaybackOptions>>();
        services.AddSingleton<IOptions<PlaybackOptions>>(
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
