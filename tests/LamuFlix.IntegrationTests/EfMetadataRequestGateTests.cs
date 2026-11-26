using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class EfMetadataRequestGateTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset BeforeMidnight = new(2026, 10, 30, 23, 59, 30, TimeSpan.Zero);
    private static readonly DateTimeOffset AfterMidnight = new(2026, 10, 31, 0, 0, 30, TimeSpan.Zero);
    private static readonly DateOnly FirstDate = new(2026, 10, 30);
    private static readonly DateOnly SecondDate = new(2026, 10, 31);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task TryAcquireAsync_WithLimitOfOne_ReturnsTrueOnceThenFalseWithSingleCountedRow()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateMigratedContext();
        var gate = CreateGate(context, BeforeMidnight, 1);

        // act
        var first = await gate.TryAcquireAsync(cancellationToken);
        var second = await gate.TryAcquireAsync(cancellationToken);

        // assert
        first.ShouldBeTrue();
        second.ShouldBeFalse();
        var stored = await ReadUsageAsync(fixture, cancellationToken);
        stored.ShouldHaveSingleItem().RequestCount.ShouldBe(1);
    }

    [Fact]
    public async Task TryAcquireAsync_UtcMidnight_ChargesDistinctDatesAndRetainsThePriorRow()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var firstContext = fixture.CreateMigratedContext();
        var firstGate = CreateGate(firstContext, BeforeMidnight, 1);
        (await firstGate.TryAcquireAsync(cancellationToken)).ShouldBeTrue();

        // act
        await using var secondContext = fixture.CreateMigratedContext();
        var secondGate = CreateGate(secondContext, AfterMidnight, 1);
        var secondDay = await secondGate.TryAcquireAsync(cancellationToken);
        var secondDayRepeat = await secondGate.TryAcquireAsync(cancellationToken);

        // assert
        secondDay.ShouldBeTrue();
        secondDayRepeat.ShouldBeFalse();
        var stored = await ReadUsageAsync(fixture, cancellationToken);
        stored.Length.ShouldBe(2);
        stored[0].UsageDate.ShouldBe(FirstDate);
        stored[0].RequestCount.ShouldBe(1);
        stored[1].UsageDate.ShouldBe(SecondDate);
        stored[1].RequestCount.ShouldBe(1);
    }

    [Fact]
    public async Task TryAcquireAsync_RetryAcrossMidnight_SamplesItsOwnDatePerAcquisition()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var clock = new SteppingTimeProvider { UtcNow = BeforeMidnight };
        await using var context = fixture.CreateMigratedContext();
        var gate = new EfMetadataRequestGate(context, clock, Options.Create(new OmdbQuotaOptions { DailyLimit = 1 }));
        (await gate.TryAcquireAsync(cancellationToken)).ShouldBeTrue();

        // act
        clock.UtcNow = AfterMidnight;
        var afterRollover = await gate.TryAcquireAsync(cancellationToken);

        // assert
        afterRollover.ShouldBeTrue();
        var stored = await ReadUsageAsync(fixture, cancellationToken);
        stored.Select(record => record.UsageDate).ShouldBe([FirstDate, SecondDate]);
        stored.Select(record => record.RequestCount).ShouldBe([1, 1]);
    }

    [Fact]
    public async Task TryAcquireAsync_LoweredPositiveLimit_DeniesWithoutResettingOrClampingTheStoredCount()
    {
        // arrange
        const int spent = 5;
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var spendingContext = fixture.CreateMigratedContext())
        {
            var spendingGate = CreateGate(spendingContext, BeforeMidnight, spent);
            for (var attempt = 0; attempt < spent; attempt++)
            {
                (await spendingGate.TryAcquireAsync(cancellationToken)).ShouldBeTrue();
            }
        }

        await using var loweredContext = fixture.CreateMigratedContext();
        var loweredGate = CreateGate(loweredContext, BeforeMidnight, 2);

        // act
        var admitted = await loweredGate.TryAcquireAsync(cancellationToken);

        // assert
        admitted.ShouldBeFalse();
        var stored = await ReadUsageAsync(fixture, cancellationToken);
        stored.ShouldHaveSingleItem().RequestCount.ShouldBe(spent);
    }

    [Fact]
    public async Task TryAcquireAsync_RaisedLimit_ResumesAdmissionsAgainstTheCarriedCount()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var exhaustedContext = fixture.CreateMigratedContext())
        {
            var exhaustedGate = CreateGate(exhaustedContext, BeforeMidnight, 2);
            (await exhaustedGate.TryAcquireAsync(cancellationToken)).ShouldBeTrue();
            (await exhaustedGate.TryAcquireAsync(cancellationToken)).ShouldBeTrue();
            (await exhaustedGate.TryAcquireAsync(cancellationToken)).ShouldBeFalse();
        }

        await using var raisedContext = fixture.CreateMigratedContext();
        var raisedGate = CreateGate(raisedContext, BeforeMidnight, 4);

        // act
        var firstRaised = await raisedGate.TryAcquireAsync(cancellationToken);
        var secondRaised = await raisedGate.TryAcquireAsync(cancellationToken);
        var exhaustedAgain = await raisedGate.TryAcquireAsync(cancellationToken);

        // assert
        firstRaised.ShouldBeTrue();
        secondRaised.ShouldBeTrue();
        exhaustedAgain.ShouldBeFalse();
        var stored = await ReadUsageAsync(fixture, cancellationToken);
        stored.ShouldHaveSingleItem().RequestCount.ShouldBe(4);
    }

    [Fact]
    public async Task TryAcquireAsync_DatabaseFailure_SurfacesPersistenceFaultAndAdmitsNothing()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var gate = CreateGate(context, BeforeMidnight, 10);

        // act
        var exception = await Should.ThrowAsync<PostgresException>(
            () => gate.TryAcquireAsync(cancellationToken));

        // assert
        exception.SqlState.ShouldBe(PostgresErrorCodes.UndefinedTable);
        var tables = await context.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*)::int AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'omdb_daily_usage'")
            .SingleAsync(cancellationToken);
        tables.ShouldBe(0);
    }

    [Fact]
    public async Task TryAcquireAsync_PreCancelledToken_ThrowsWithoutIncrementing()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateMigratedContext();
        var gate = CreateGate(context, BeforeMidnight, 10);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        // act
        await Should.ThrowAsync<OperationCanceledException>(
            () => gate.TryAcquireAsync(cancelled.Token));

        // assert
        (await ReadUsageAsync(fixture, cancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task TryAcquireAsync_TwoConsumers_SharePoolAndChargeFailedAttemptsPerConsumer()
    {
        // arrange
        const int limit = 3;
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var firstContext = fixture.CreateMigratedContext();
        await using var secondContext = fixture.CreateMigratedContext();
        var firstGate = CreateGate(firstContext, BeforeMidnight, limit);
        var secondGate = CreateGate(secondContext, BeforeMidnight, limit);
        var firstTransport = new FailingHandler();
        var secondTransport = new FailingHandler();
        using var discovery = Consumer(firstGate, firstTransport);
        using var enrichment = Consumer(secondGate, secondTransport);

        // act
        for (var attempt = 0; attempt < 2; attempt++)
        {
            await discovery.GetAsync(new Uri("https://example.test/?i=tt9459701"), cancellationToken);
        }

        await enrichment.GetAsync(new Uri("https://example.test/?t=Big%20Buck%20Bunny"), cancellationToken);
        var denial = await Should.ThrowAsync<MetadataQuotaExhaustedException>(
            () => enrichment.GetAsync(new Uri("https://example.test/?t=Elephants%20Dream"), cancellationToken));

        // assert
        denial.InnerException.ShouldBeNull();
        firstTransport.Calls.ShouldBe(2);
        secondTransport.Calls.ShouldBe(1);
        (await ReadUsageAsync(fixture, cancellationToken))
            .ShouldHaveSingleItem().RequestCount.ShouldBe(limit);
    }

    [Fact]
    public async Task TryAcquireAsync_DisabledAllowance_DeniesWithoutChargingUsage()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateMigratedContext();
        var gate = new EfMetadataRequestGate(
            context,
            new FixedTimeProvider(BeforeMidnight),
            Options.Create(new OmdbQuotaOptions()));

        // act
        var admitted = await gate.TryAcquireAsync(cancellationToken);

        // assert
        admitted.ShouldBeFalse();
        (await ReadUsageAsync(fixture, cancellationToken)).ShouldBeEmpty();
    }

    private static HttpClient Consumer(IOmdbRequestGate gate, HttpMessageHandler transport)
    {
        var services = new ServiceCollection()
            .AddSingleton(gate)
            .BuildServiceProvider();
        return new HttpClient(new MetadataQuotaHandler(
            services.GetRequiredService<IServiceScopeFactory>())
        { InnerHandler = transport });
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }

    private static EfMetadataRequestGate CreateGate(LamuFlixDbContext context, DateTimeOffset now, int dailyLimit) =>
        new(context, new FixedTimeProvider(now), Options.Create(new OmdbQuotaOptions { DailyLimit = dailyLimit }));

    private static async Task<DailyRequestUsageRecord[]> ReadUsageAsync(
        PostgresFixture fixture,
        CancellationToken cancellationToken)
    {
        await using var context = fixture.CreateMigratedContext();
        return await context.DailyRequestUsages
            .AsNoTracking()
            .OrderBy(usage => usage.UsageDate)
            .ToArrayAsync(cancellationToken);
    }

    private sealed class SteppingTimeProvider : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; }

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
