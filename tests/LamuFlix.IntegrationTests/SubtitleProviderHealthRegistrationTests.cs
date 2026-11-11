using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class SubtitleProviderHealthRegistrationTests
{
    private const string SubtitleProviderCheckName = "subtitle-provider";
    private const string LiveRoute = "/health/live";
    private const string ReadyRoute = "/health/ready";

    [Fact]
    public void SubtitleProviderCheck_IsRegisteredUntaggedSoReadinessAndLivenessStayUnchanged()
    {
        // arrange
        using var host = new ApiHostFactory(retainSweeper: false, retainHealthChecks: true);

        // act
        var registrations = host.Services
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value
            .Registrations;

        // assert
        var subtitleProvider = registrations.Single(registration => registration.Name == SubtitleProviderCheckName);
        subtitleProvider.Tags.ShouldBeEmpty();
        registrations
            .Where(registration => registration.Tags.Contains(HealthCheckTags.Ready))
            .Select(registration => registration.Name)
            .ShouldNotContain(SubtitleProviderCheckName);
    }

    [Fact]
    public async Task LiveProbe_WithUnconfiguredAcquisition_StillReturnsOkAndRunsNoDependencyChecks()
    {
        // arrange
        using var host = new ApiHostFactory(retainSweeper: false, retainHealthChecks: true);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(LiveRoute, TestContext.Current.CancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReadyProbe_WithUnconfiguredAcquisition_ReportsTheOptionalProviderAsHealthy()
    {
        // arrange
        using var host = new ApiHostFactory(retainSweeper: false, retainHealthChecks: true);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(ReadyRoute, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        body.ShouldNotContain(TelemetryConstants.SubtitleDisabledOrUnconfiguredState);
        body.ShouldNotContain("sentinel");
    }
}
