using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WireMock.Server;
using Xunit;

namespace LamuFlix.IntegrationTests;

public abstract class ApiEndToEndTestBase(ApiEndToEndFixture fixture) : IAsyncLifetime
{
    private const int PollDeadlineSeconds = 30;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);

    private readonly AdjustableTimeProvider time = new(TimeProvider.System.GetUtcNow());
    private ApiEndToEndFactory? host;

    protected ApiEndToEndFixture Fixture => fixture;

    protected TimeProvider Clock => time;

    // ReSharper disable once MemberCanBePrivate.Global - derived ApiEndToEnd test classes resolve host services and DI identity through this property.
    protected IServiceProvider Services => RequireHost().Services;

    public virtual ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public virtual ValueTask DisposeAsync() => StopHostAsync();

    protected async Task<HttpClient> StartHostAsync(
        Action<WireMockServer>? installStubs = null,
        Action<IServiceCollection>? configureTestServices = null)
    {
        await StopHostAsync();
        await fixture.PrepareAsync(TestContext.Current.CancellationToken);
        installStubs?.Invoke(fixture.Server);
        var started = new ApiEndToEndFactory(fixture.HostSettings(time), configureTestServices);
        host = started;
        var client = started.CreateClient();
        Services.GetRequiredService<TimeProvider>().ShouldBeSameAs(time);
        return client;
    }

    protected async Task<MovieRecord> SeedAsync(MovieRecord movie)
    {
        await using var context = fixture.Postgres.CreateMigratedContext();
        return await MovieCatalogSeed.AddAsync(context, movie, TestContext.Current.CancellationToken);
    }

    protected async Task WaitForStatusAsync(MovieId id, EnrichmentStatus expected)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var deadline = TimeProvider.System.GetUtcNow() + TimeSpan.FromSeconds(PollDeadlineSeconds);
        EnrichmentStatus? observed;
        while (TimeProvider.System.GetUtcNow() < deadline)
        {
            observed = await ReadPersistedStatusAsync(id, cancellationToken);
            if (observed == expected)
            {
                return;
            }

            await Task.Delay(PollInterval, cancellationToken);
        }

        observed = await ReadPersistedStatusAsync(id, cancellationToken);
        observed.ShouldBe(
            expected,
            $"movie {id.Value} did not reach {expected} within {PollDeadlineSeconds} seconds; " +
            $"last observed {(observed?.ToString() ?? "no persisted row")}");
    }

    protected void AdvanceClock(TimeSpan amount) => time.Advance(amount);

    private async Task<EnrichmentStatus?> ReadPersistedStatusAsync(MovieId id, CancellationToken cancellationToken)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LamuFlixDbContext>();
        return await context.Movies
            .AsNoTracking()
            .Where(movie => movie.Id == id)
            .Select(movie => (EnrichmentStatus?)movie.Status)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private ApiEndToEndFactory RequireHost() => host ?? throw new InvalidOperationException(
        "The ApiEndToEnd test host has not been started. StartHostAsync must run first.");

    private ValueTask StopHostAsync()
    {
        if (host is null)
        {
            return ValueTask.CompletedTask;
        }

        var stopped = host;
        host = null;
        return stopped.DisposeAsync();
    }

    private sealed class AdjustableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private readonly Lock sync = new();
        private DateTimeOffset utcNow = now;

        public override DateTimeOffset GetUtcNow()
        {
            lock (sync)
            {
                return utcNow;
            }
        }

        public void Advance(TimeSpan amount)
        {
            lock (sync)
            {
                utcNow += amount;
            }
        }
    }
}
