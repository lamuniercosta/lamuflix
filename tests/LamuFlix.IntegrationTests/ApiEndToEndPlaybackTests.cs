using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using LamuFlix.Infrastructure.Playback;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(ApiEndToEndCollection))]
public sealed class ApiEndToEndPlaybackTests(ApiEndToEndFixture fixture) : ApiEndToEndTestBase(fixture)
{
    private const string ProblemDetailsJson = "application/problem+json";

    [Fact]
    public async Task PlayMovie_LocalPlayDisabled_ReturnsForbiddenProblemWithoutProcessStart()
    {
        // arrange
        var starter = Substitute.For<IProcessStarter>();
        var client = await StartHostAsync(configureTestServices: services =>
        {
            services.RemoveAll<IProcessStarter>();
            services.AddSingleton(starter);
        });
        var movie = await SeedAsync(MovieCatalogSeed.Create("Heat", "play-heat"));
        var id = movie.Id.ShouldNotBeNull().Value;
        var resolved = Services.GetServices<IProcessStarter>().ToList();
        resolved.ShouldHaveSingleItem().ShouldBeSameAs(starter);
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync(PlayRoute(id), null, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("title").GetString().ShouldBe("Forbidden");
        starter.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
    }

    private static string PlayRoute(int id) => $"/api/movies/{id}/play";
}
