using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class TestHostNetworkIsolationTests
{
    private const string RealProviderUrl = "https://api.themoviedb.org/3/configuration";
    private const string RealProviderHost = "api.themoviedb.org";
    private const string UnregisteredClientName = "an-unregistered-client";
    private const string UnreachableHost = "example.invalid";
    private const string PingPath = "/ping";

    [Theory]
    [InlineData(nameof(OmdbMetadataProvider))]
    [InlineData(nameof(TmdbMetadataProvider))]
    [InlineData(UnregisteredClientName)]
    public async Task FactoryClient_InATestHost_RefusesANonLoopbackHostBeforeSending(string clientName)
    {
        // arrange
        await using var host = new ApiHostFactory();
        var client = host.Services.GetRequiredService<IHttpClientFactory>().CreateClient(clientName);

        // act
        var failure = await Should.ThrowAsync<HttpRequestException>(
            () => client.GetAsync(new Uri(RealProviderUrl), TestContext.Current.CancellationToken));

        // assert
        failure.Message.ShouldBe(TestHostNetworkIsolation.BlockedRequestMessagePrefix + RealProviderHost);
    }

    [Fact]
    public async Task FactoryClient_InATestHost_StillReachesALoopbackStub()
    {
        // arrange
        using var stub = LoopbackWireMock.Start();
        stub.Given(Request.Create().WithPath(PingPath).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK));
        await using var host = new ApiHostFactory();
        var client = host.Services.GetRequiredService<IHttpClientFactory>().CreateClient(UnregisteredClientName);

        // act
        using var response = await client.GetAsync(
            new Uri($"{stub.Urls[0]}{PingPath}"),
            TestContext.Current.CancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        stub.LogEntries.Count().ShouldBe(1);
    }

    [Fact]
    public void UserSecretsPredicate_MatchesTheSourceADevelopmentApiHostAdds()
    {
        // arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(Program).Assembly.GetName().Name,
            EnvironmentName = Environments.Development,
        });

        // act
        var sources = ((IConfigurationBuilder)builder.Configuration).Sources;

        // assert
        sources.ShouldContain(source => TestHostNetworkIsolation.IsUserSecretsSource(source));
    }

    [Fact]
    public void ApiHost_BuildsItsConfigurationWithoutTheUserSecretsSource()
    {
        // arrange
        using var host = new ApiHostFactory();

        // act
        var root = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();

        // assert
        root.Providers
            .OfType<Microsoft.Extensions.Configuration.Json.JsonConfigurationProvider>()
            .Select(provider => provider.Source)
            .ShouldNotContain(source => TestHostNetworkIsolation.IsUserSecretsSource(source));
    }

    [Fact]
    public void ApiHost_KeepsTmdbDisabledAndOffTheRealUrl()
    {
        // arrange
        using var host = new ApiHostFactory();

        // act
        var tmdb = host.Services.GetRequiredService<IOptions<TmdbOptions>>().Value;

        // assert
        tmdb.Enabled.ShouldBeFalse();
        tmdb.ApiKey.ShouldBeEmpty();
        new Uri(tmdb.BaseUrl).Host.ShouldBe(UnreachableHost);
    }
}
