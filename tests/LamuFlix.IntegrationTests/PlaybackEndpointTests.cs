using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Playback;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class PlaybackEndpointTests(ApiHostFactory factory) : IClassFixture<ApiHostFactory>
{
    private const int KnownId = 7;
    private const int UnknownId = 999;
    private const int NonpositiveId = 0;
    private const string MovieFolder = "Heat";
    private const string ResolvedFile = "Heat/Heat.mkv";
    private const string PlayerPath = "C:/players/vlc.exe";
    private const string ProblemDetailsJson = "application/problem+json";

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();
    private readonly IProcessStarter starter = Substitute.For<IProcessStarter>();
    private readonly IPlaybackHistory history = Substitute.For<IPlaybackHistory>();
    private readonly TimeProvider clock = new FixedTimeProvider(
        new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task PlayMovie_LocalPlayEnabled_ReturnsEmptyNoContentAndRecordsOneProcessStart()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        DetailsReturning(KnownDetails("avi"));
        scanner.ResolvePrimaryVideo(new LibraryPath(MovieFolder)).Returns(Resolved("mkv"));
        await using var host = CreateEnabledHost(".mkv");
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PlayRoute(KnownId), null, ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync(ct)).ShouldBe(string.Empty);
        starter.Received(1).Start(Arg.Is<ProcessStartInfo>(
            info => info.FileName == PlayerPath
                && info.UseShellExecute
                && info.Arguments == string.Empty
                && info.ArgumentList.Count == 1
                && info.ArgumentList[0] == ResolvedFile));
        await history.Received(1).RecordAsync(
            Arg.Is<PlaybackEvent>(playbackEvent => playbackEvent.MovieId == new MovieId(KnownId)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlayMovie_StaleStoredFormat_SelectsPlayerFromResolvedExtension()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        DetailsReturning(KnownDetails(".avi"));
        scanner.ResolvePrimaryVideo(new LibraryPath(MovieFolder)).Returns(Resolved("mkv"));
        await using var host = CreateEnabledHost(".mkv");
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PlayRoute(KnownId), null, ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        starter.Received(1).Start(Arg.Is<ProcessStartInfo>(
            info => info.FileName == PlayerPath && info.ArgumentList[0] == ResolvedFile));
    }

    [Fact]
    public async Task PlayMovie_LocalPlayDisabled_ReturnsForbiddenWithoutDiskProbeOrProcessStart()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        DetailsReturning(KnownDetails("mkv"));
        await using var host = CreateDisabledHost();
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PlayRoute(KnownId), null, ct);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("title").GetString().ShouldBe("Forbidden");
        scanner.DidNotReceive().ResolvePrimaryVideo(Arg.Any<LibraryPath>());
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
        await history.DidNotReceive().RecordAsync(Arg.Any<PlaybackEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlayMovie_UnknownId_ReturnsNotFoundWithoutProcessStart()
    {
        // arrange
        await using var host = CreateEnabledHost(".mkv");
        using var client = host.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync(PlayRoute(UnknownId), null, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        payload.ShouldNotContain(nameof(NotFoundException));
        scanner.DidNotReceive().ResolvePrimaryVideo(Arg.Any<LibraryPath>());
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
    }

    [Fact]
    public async Task PlayMovie_MissingFolder_ReturnsNotFoundProblemWithoutProcessStart()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        DetailsReturning(KnownDetails("mkv"));
        scanner.ResolvePrimaryVideo(new LibraryPath(MovieFolder)).Throws(new NotFoundException());
        await using var host = CreateEnabledHost(".mkv");
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PlayRoute(KnownId), null, ct);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("title").GetString().ShouldBe("Not Found");
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
        await history.DidNotReceive().RecordAsync(Arg.Any<PlaybackEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlayMovie_NoEligibleVideo_ReturnsConflictProblemWithoutProcessStart()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        DetailsReturning(KnownDetails("mkv"));
        scanner.ResolvePrimaryVideo(new LibraryPath(MovieFolder))
            .Throws(new InvalidTransitionException($"Library folder '{MovieFolder}' contains no eligible top-level video file."));
        await using var host = CreateEnabledHost(".mkv");
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PlayRoute(KnownId), null, ct);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("status").GetInt32().ShouldBe(409);
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
        await history.DidNotReceive().RecordAsync(Arg.Any<PlaybackEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlayMovie_NonpositiveId_ReturnsUnprocessableEntityProblem()
    {
        // arrange
        await using var host = CreateDisabledHost();
        using var client = host.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync(PlayRoute(NonpositiveId), null, ct);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("errors").EnumerateObject().Select(property => property.Name).ShouldContain("id");
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
    }

    [Fact]
    public async Task PlayMovie_UnmappedFormat_ReturnsUnprocessableEntityProblemWithoutProcessStartOrLeak()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        DetailsReturning(KnownDetails("mkv"));
        scanner.ResolvePrimaryVideo(new LibraryPath(MovieFolder)).Returns(Resolved("mp4"));
        await using var host = CreateEnabledHost(".mkv");
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PlayRoute(KnownId), null, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        var problem = JsonSerializer.Deserialize<JsonElement>(payload);
        problem.GetProperty("title").GetString().ShouldBe("Unprocessable Entity");
        problem.TryGetProperty("detail", out _).ShouldBeFalse();
        problem.GetProperty("errors").GetProperty("format").EnumerateArray()
            .Select(value => value.GetString() ?? string.Empty)
            .ShouldContain(value => value.Contains("mp4", StringComparison.Ordinal));
        payload.ShouldNotContain(PlayerPath);
        payload.ShouldNotContain(ResolvedFile);
        payload.ShouldNotContain(nameof(ValidationException));
        payload.ShouldNotContain(nameof(InvalidOperationException));
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
        await history.DidNotReceive().RecordAsync(Arg.Any<PlaybackEvent>(), Arg.Any<CancellationToken>());
    }

    private void DetailsReturning(MovieDetails details) =>
        catalog.GetDetailsAsync(new MovieId(KnownId), Arg.Any<CancellationToken>()).Returns(details);

    [Fact]
    public async Task PlayMovie_RelativeStoredPath_PassesTheStoredIdentityToTheScannerWithoutRewritingIt()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var details = new MovieDetails(
            new MovieId(KnownId),
            "Heat",
            new LibraryPath("Collections/Heat (1995)"),
            new MediaFormat(".avi"),
            null,
            false,
            false,
            Review.None,
            EnrichmentStatus.Enriched);
        DetailsReturning(details);
        scanner.ResolvePrimaryVideo(new LibraryPath("Collections/Heat (1995)"))
            .Returns(new PrimaryVideo(new LibraryPath("Collections/Heat (1995)/Heat.mkv"), new MediaFormat("mkv")));
        await using var host = CreateEnabledHost(".mkv");
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PlayRoute(KnownId), null, ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        scanner.Received(1).ResolvePrimaryVideo(new LibraryPath("Collections/Heat (1995)"));
        starter.Received(1).Start(Arg.Is<ProcessStartInfo>(info =>
            info.ArgumentList.Count == 1
            && info.ArgumentList[0] == "Collections/Heat (1995)/Heat.mkv"));
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("::ffff:127.0.0.1")]
    public async Task PlayMovie_ProvenLoopbackPeer_AllowsTheLaunch(string peer)
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        DetailsReturning(KnownDetails("mkv"));
        scanner.ResolvePrimaryVideo(new LibraryPath(MovieFolder)).Returns(Resolved("mkv"));
        await using var host = CreateEnabledHost(".mkv");
        using var client = host.CreateClient();
        using var request = RequestWithPeer(PlayRoute(KnownId), peer);

        // act
        var response = await client.SendAsync(request, ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        starter.Received(1).Start(Arg.Is<ProcessStartInfo>(info => info.ArgumentList[0] == ResolvedFile));
        await history.Received(1).RecordAsync(
            Arg.Is<PlaybackEvent>(playbackEvent => playbackEvent.MovieId == new MovieId(KnownId)),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("10.0.0.5")]
    [InlineData("192.168.1.20")]
    [InlineData("::ffff:10.0.0.5")]
    [InlineData(PlaybackPeerStartupFilter.NullPeerValue)]
    public async Task PlayMovie_UnprovenPeer_ReturnsForbiddenBeforeAnySideEffect(string peer)
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        DetailsReturning(KnownDetails("mkv"));
        scanner.ResolvePrimaryVideo(new LibraryPath(MovieFolder)).Returns(Resolved("mkv"));
        await using var host = CreateEnabledHost(".mkv");
        using var client = host.CreateClient();
        using var request = RequestWithPeer(PlayRoute(KnownId), peer);

        // act
        var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        assertBoundaryRefusal(response, body);
    }

    [Theory]
    [InlineData("Forwarded", "for=10.0.0.5")]
    [InlineData("forwarded", "for=10.0.0.5")]
    [InlineData("forwarded", "")]
    [InlineData("Forwarded", "for=10.0.0.5, for=10.0.0.6")]
    [InlineData("X-Forwarded-For", "10.0.0.5")]
    [InlineData("x-forwarded-for", "")]
    [InlineData("X-Forwarded-For", "10.0.0.5, 10.0.0.6")]
    [InlineData("X-Forwarded-Host", "remote.example")]
    [InlineData("x-forwarded-host", "")]
    [InlineData("X-Forwarded-Proto", "https")]
    [InlineData("x-forwarded-proto", "")]
    [InlineData("X-Real-IP", "10.0.0.5")]
    [InlineData("x-real-ip", "")]
    public async Task PlayMovie_LoopbackPeerWithProvenanceHeader_ReturnsForbiddenBeforeAnySideEffect(
        string headerName,
        string headerValue)
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        DetailsReturning(KnownDetails("mkv"));
        scanner.ResolvePrimaryVideo(new LibraryPath(MovieFolder)).Returns(Resolved("mkv"));
        await using var host = CreateEnabledHost(".mkv");
        using var client = host.CreateClient();
        using var request = RequestWithPeer(PlayRoute(KnownId), "127.0.0.1");
        request.Headers.TryAddWithoutValidation(PlaybackPeerStartupFilter.ProvenanceNameHeader, headerName)
            .ShouldBeTrue();
        request.Headers.TryAddWithoutValidation(PlaybackPeerStartupFilter.ProvenanceValueHeader, headerValue)
            .ShouldBeTrue();

        // act
        var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        assertBoundaryRefusal(response, body);
        body.ShouldNotContain(headerName);
    }

    private void assertBoundaryRefusal(HttpResponseMessage response, string body)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        problem.GetProperty("title").GetString().ShouldBe("Forbidden");
        problem.TryGetProperty("detail", out _).ShouldBeFalse();
        body.ShouldNotContain("LocalPlay is disabled");
        body.ShouldNotContain(nameof(FeatureDisabledException));
        body.ShouldNotContain("proven loopback peer");
        catalog.DidNotReceive().GetDetailsAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
        scanner.DidNotReceive().ResolvePrimaryVideo(Arg.Any<LibraryPath>());
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
        history.DidNotReceive().RecordAsync(Arg.Any<PlaybackEvent>(), Arg.Any<CancellationToken>());
    }

    private static HttpRequestMessage RequestWithPeer(string route, string peer)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, route);
        request.Headers.TryAddWithoutValidation(PlaybackPeerStartupFilter.PeerHeader, peer).ShouldBeTrue();
        return request;
    }

    private static string PlayRoute(int id) => $"/api/movies/{id}/play";

    private static MovieDetails KnownDetails(string storedFormat) =>
        new(new MovieId(KnownId), "Heat", new LibraryPath(MovieFolder), new MediaFormat(storedFormat), null, false, false, Review.None, EnrichmentStatus.Enriched);

    private static PrimaryVideo Resolved(string extension) =>
        new(new LibraryPath($"{MovieFolder}/Heat.{extension}"), new MediaFormat(extension));

    private WebApplicationFactory<Program> CreateDisabledHost() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(AddDoubles));

    private WebApplicationFactory<Program> CreateEnabledHost(string mappedFormat) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Features:LocalPlay"] = "true",
                    ["Playback:Players:0:Name"] = "vlc",
                    ["Playback:Players:0:ExecutablePath"] = PlayerPath,
                    ["Playback:Players:0:Formats:0"] = mappedFormat,
                }));
            builder.ConfigureTestServices(AddDoubles);
        });

    private void AddDoubles(IServiceCollection services)
    {
        services.AddSingleton(catalog);
        services.AddSingleton(scanner);
        services.AddSingleton(starter);
        services.AddSingleton(history);
        services.AddSingleton(clock);
        services.AddSingleton(ExistingFiles());
        services.AddSingleton<IStartupFilter, PlaybackPeerStartupFilter>();
    }

    private static IFileSystem ExistingFiles()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        var file = Substitute.For<IFile>();
        file.Exists(Arg.Any<string>()).Returns(true);
        fileSystem.File.Returns(file);
        return fileSystem;
    }
}
