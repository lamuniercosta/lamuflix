using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class EfMetadataRequestGateConcurrencyTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 30);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task TryAcquireAsync_MoreSynchronisedContendersThanLimit_AdmitsExactlyTheLimit()
    {
        // arrange
        const int limit = 7;
        const int contenderCount = 25;
        var cancellationToken = TestContext.Current.CancellationToken;
        var contenders = new (LamuFlixDbContext Context, EfMetadataRequestGate Gate)[contenderCount];
        for (var index = 0; index < contenderCount; index++)
        {
            var context = fixture.CreateMigratedContext();
            contenders[index] = (context, CreateGate(context, Now, limit));
        }

        try
        {
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var attempts = contenders
                .Select(contender => AcquireWhenReleasedAsync(contender.Gate, start.Task, cancellationToken))
                .ToArray();

            // act
            start.SetResult();
            var admitted = await Task.WhenAll(attempts);

            // assert
            admitted.Sum(result => result ? 1 : 0).ShouldBe(limit);
            admitted.Sum(result => result ? 0 : 1).ShouldBe(contenderCount - limit);
            var stored = await ReadUsageAsync(fixture, cancellationToken);
            stored.Length.ShouldBe(1);
            stored.Single().UsageDate.ShouldBe(Today);
            stored.Single().RequestCount.ShouldBe(limit);
        }
        finally
        {
            foreach (var contender in contenders)
            {
                await contender.Context.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task TryAcquireAsync_EmptyTableFirstRowRace_CreatesOneRowWithTheExactCount()
    {
        // arrange
        const int limit = 5;
        const int contenderCount = 20;
        var cancellationToken = TestContext.Current.CancellationToken;
        var contenders = new (LamuFlixDbContext Context, EfMetadataRequestGate Gate)[contenderCount];
        for (var index = 0; index < contenderCount; index++)
        {
            var context = fixture.CreateMigratedContext();
            contenders[index] = (context, CreateGate(context, Now, limit));
        }

        try
        {
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var attempts = contenders
                .Select(contender => AcquireWhenReleasedAsync(contender.Gate, start.Task, cancellationToken))
                .ToArray();

            // act
            start.SetResult();
            var admitted = await Task.WhenAll(attempts);

            // assert
            admitted.Sum(result => result ? 1 : 0).ShouldBe(limit);
            var stored = await ReadUsageAsync(fixture, cancellationToken);
            stored.Length.ShouldBe(1);
            stored.Single().RequestCount.ShouldBe(limit);
        }
        finally
        {
            foreach (var contender in contenders)
            {
                await contender.Context.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task TryAcquireAsync_ExhaustionSurvivesGateAndContextRecreationOnTheSameDatabase()
    {
        // arrange
        const int limit = 3;
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var spendingContext = fixture.CreateMigratedContext())
        {
            var spendingGate = CreateGate(spendingContext, Now, limit);
            for (var attempt = 0; attempt < limit; attempt++)
            {
                (await spendingGate.TryAcquireAsync(cancellationToken)).ShouldBeTrue();
            }
        }

        // act
        await using var recreatedContext = fixture.CreateMigratedContext();
        var recreatedGate = CreateGate(recreatedContext, Now, limit);

        // assert
        (await recreatedGate.TryAcquireAsync(cancellationToken)).ShouldBeFalse();
        var stored = await ReadUsageAsync(fixture, cancellationToken);
        stored.Single().RequestCount.ShouldBe(limit);
    }

    private static async Task<bool> AcquireWhenReleasedAsync(
        EfMetadataRequestGate gate,
        Task start,
        CancellationToken cancellationToken)
    {
        await start;
        return await gate.TryAcquireAsync(cancellationToken);
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
}
