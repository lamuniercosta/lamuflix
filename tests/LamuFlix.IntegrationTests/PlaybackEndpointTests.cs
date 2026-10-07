using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Playback;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class PlaybackEndpointTests(ApiHostFactory factory) : IClassFixture<ApiHostFactory>
{
    private const int KnownId = 7;
    private const int UnknownId = 999;
    private const int NonpositiveId = 0;
    private const string MoviePath = "C:/library/Heat/Heat.mkv";
    private const string PlayerPath = "C:/players/vlc.exe";
    private const string ProblemDetailsJson = "application/problem+json";

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly IProcessStarter starter = Substitute.For<IProcessStarter>();

    [Fact]
    public async Task PlayMovie_LocalPlayEnabled_ReturnsEmptyNoContentAndRecordsOneProcessStart()
    {
        // arrange
        catalog.GetDetailsAsync(new MovieId(KnownId), Arg.Any<CancellationToken>())
            .Returns(KnownDetails());
        await using var host = CreateEnabledHost(".mkv");
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync(PlayRoute(KnownId), null, cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldBe(string.Empty);
        starter.Received(1).Start(Arg.Is<ProcessStartInfo>(
            info => info.FileName == PlayerPath
                && info.UseShellExecute
                && info.Arguments == string.Empty
                && info.ArgumentList.Count == 1
                && info.ArgumentList[0] == MoviePath));
    }

    [Fact]
    public async Task PlayMovie_LocalPlayDisabled_ReturnsForbiddenWithoutProcessStart()
    {
        // arrange
        catalog.GetDetailsAsync(new MovieId(KnownId), Arg.Any<CancellationToken>())
            .Returns(KnownDetails());
        await using var host = CreateDisabledHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync(PlayRoute(KnownId), null, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("title").GetString().ShouldBe("Forbidden");
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
    }

    [Fact]
    public async Task PlayMovie_UnknownId_ReturnsNotFoundWithoutProcessStart()
    {
        // arrange
        await using var host = CreateEnabledHost(".mkv");
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync(PlayRoute(UnknownId), null, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        payload.ShouldNotContain(nameof(NotFoundException));
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
    }

    [Fact]
    public async Task PlayMovie_NonpositiveId_ReturnsUnprocessableEntityProblem()
    {
        // arrange
        await using var host = CreateDisabledHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync(PlayRoute(NonpositiveId), null, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("errors").EnumerateObject().Select(property => property.Name).ShouldContain("id");
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
    }

    [Fact]
    public async Task PlayMovie_UnmappedFormat_FailsClosedWithGenericProblemAndNoProcessStart()
    {
        // arrange
        catalog.GetDetailsAsync(new MovieId(KnownId), Arg.Any<CancellationToken>())
            .Returns(KnownDetails());
        await using var host = CreateEnabledHost(".mp4");
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync(PlayRoute(KnownId), null, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        payload.ShouldNotContain("No configured player supports format");
        payload.ShouldNotContain(nameof(InvalidOperationException));
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
    }

    private static string PlayRoute(int id) => $"/api/movies/{id}/play";

    private static MovieDetails KnownDetails() =>
        new(new MovieId(KnownId), "Heat", new LibraryPath(MoviePath), new MediaFormat(".mkv"), null);

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
        services.AddSingleton(starter);
    }
}
