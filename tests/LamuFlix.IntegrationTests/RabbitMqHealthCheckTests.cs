using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Tests.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(RabbitMqCollection))]
public sealed class RabbitMqHealthCheckTests(RabbitMqFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetTopologyAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CheckHealthAsync_TheBrokerIsUp_IsHealthy()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var owner = new RabbitMqConnectionOwner(Options.Create(fixture.Options));

        var result = await new RabbitMqHealthCheck(owner).CheckHealthAsync(Context(), ct);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBe(RabbitMqHealthCheck.HealthyDescription);
    }

    [Fact]
    public async Task CheckHealthAsync_TheBrokerIsUnreachable_IsUnhealthyWithoutCredentials()
    {
        var ct = TestContext.Current.CancellationToken;
        var options = fixture.Options with { Port = 1 };
        await using var owner = new RabbitMqConnectionOwner(Options.Create(options));

        var result = await new RabbitMqHealthCheck(owner).CheckHealthAsync(Context(), ct);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe(RabbitMqHealthCheck.UnhealthyDescription);
        result.Exception.ShouldBeNull();
    }

    private static HealthCheckContext Context() =>
        new() { Registration = new HealthCheckRegistration("rabbitmq", new NoopHealthCheck(), null, null) };

    private sealed class NoopHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(HealthCheckResult.Healthy());
    }
}
