using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Enrichment;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class OutboxRetentionTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly DateTimeOffset StartedAt = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Cutoff = StartedAt - new OutboxRelayOptions().ProcessedRetention;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask InitializeAsync() => await postgres.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Cleanup_KeepsUnprocessedRecentFutureAndCutoffEqualRowsAndDeletesOnlyStrictlyOldProcessedRows()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var unprocessed = await InsertRowAsync(StartedAt.AddDays(-30), processedAt: null, cancellationToken);
        var recent = await InsertRowAsync(StartedAt.AddDays(-30), Cutoff.AddMinutes(1), cancellationToken);
        var future = await InsertRowAsync(StartedAt.AddDays(-30), StartedAt.AddDays(1), cancellationToken);
        var cutoffEqual = await InsertRowAsync(StartedAt.AddDays(-30), Cutoff, cancellationToken);
        var expiredWithFutureCreatedAt = await InsertRowAsync(
            StartedAt.AddDays(30),
            Cutoff.AddMinutes(-1),
            cancellationToken);
        await using var provider = CreateRelayProvider();
        var worker = ActivatorUtilities.CreateInstance<OutboxRelayHostedService>(provider);

        // act
        var cleanup = await worker.RunCleanupAsync(cancellationToken);

        // assert
        cleanup.Result.ShouldBe(OutboxRelayCleanupResult.Succeeded);
        cleanup.DeletedCount.ShouldBe(1);
        var remaining = await ReadIdsAsync(cancellationToken);
        remaining.ShouldBe([unprocessed, recent, future, cutoffEqual]);
        remaining.ShouldNotContain(expiredWithFutureCreatedAt);
        (await ReadProcessedAtAsync(cutoffEqual, cancellationToken)).ShouldBe(Cutoff);
        (await ReadProcessedAtAsync(unprocessed, cancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task Cleanup_DeletesAtMostCleanupBatchSizeRowsInProcessedAtThenIdOrderPerScheduledRun()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var oldest = await InsertRowAsync(StartedAt, Cutoff.AddDays(-5), cancellationToken);
        var tiedFirst = await InsertRowAsync(StartedAt, Cutoff.AddDays(-3), cancellationToken);
        var tiedSecond = await InsertRowAsync(StartedAt, Cutoff.AddDays(-3), cancellationToken);
        var newer = await InsertRowAsync(StartedAt, Cutoff.AddDays(-1), cancellationToken);
        await using var provider = CreateRelayProvider(new Dictionary<string, string?>
        {
            [$"{OutboxRelayOptions.SectionName}:CleanupBatchSize"] = "2",
        });
        var worker = ActivatorUtilities.CreateInstance<OutboxRelayHostedService>(provider);

        // act
        var firstRun = await worker.RunCleanupAsync(cancellationToken);

        // assert
        firstRun.Result.ShouldBe(OutboxRelayCleanupResult.Succeeded);
        firstRun.DeletedCount.ShouldBe(2);
        (await ReadIdsAsync(cancellationToken)).ShouldBe([tiedSecond, newer]);
        (await ReadIdsAsync(cancellationToken)).ShouldNotContain(oldest);
        (await ReadIdsAsync(cancellationToken)).ShouldNotContain(tiedFirst);

        // act
        var secondRun = await worker.RunCleanupAsync(cancellationToken);

        // assert
        secondRun.Result.ShouldBe(OutboxRelayCleanupResult.Succeeded);
        secondRun.DeletedCount.ShouldBe(2);
        (await ReadIdsAsync(cancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Cleanup_RechecksTheCutoffInTheDeleteSoARowThatBecomesIneligibleIsKept()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var first = await InsertRowAsync(StartedAt, Cutoff.AddDays(-1), cancellationToken);
        var second = await InsertRowAsync(StartedAt, Cutoff.AddDays(-1), cancellationToken);
        await using var provider = CreateRelayProvider();
        var worker = ActivatorUtilities.CreateInstance<ScriptedRetentionWorker>(provider);
        worker.AfterReadExpired = async rowIds =>
        {
            await using var context = postgres.CreateMigratedContext();
            var rewritten = await context.OutboxMessages.SingleAsync(
                message => message.Id == rowIds[0],
                cancellationToken);
            rewritten.ProcessedAt = Cutoff;
            await context.SaveChangesAsync(cancellationToken);
        };

        // act
        var cleanup = await worker.RunCleanupAsync(cancellationToken);

        // assert
        cleanup.Result.ShouldBe(OutboxRelayCleanupResult.Succeeded);
        cleanup.DeletedCount.ShouldBe(1);
        (await ReadIdsAsync(cancellationToken)).ShouldBe([first]);
        (await ReadProcessedAtAsync(first, cancellationToken)).ShouldBe(Cutoff);
        second.ShouldBeGreaterThan(first);
    }

    [Fact]
    public async Task CleanupFailure_IsIsolatedFromPublicationAndSucceedsOnALaterRun()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var expired = await InsertRowAsync(StartedAt.AddDays(-2), Cutoff.AddDays(-1), cancellationToken);
        await using var provider = CreateRelayProvider();
        var worker = ActivatorUtilities.CreateInstance<ScriptedRetentionWorker>(provider);
        worker.DeleteFault = (_, _, _, _) =>
            Task.FromException<int>(new InvalidOperationException("cleanup failed"));

        // act
        var failed = await worker.RunCleanupAsync(cancellationToken);

        // assert
        failed.Result.ShouldBe(OutboxRelayCleanupResult.Failed);
        failed.DeletedCount.ShouldBe(0);
        (await ReadIdsAsync(cancellationToken)).ShouldBe([expired]);

        // act: publication keeps working while cleanup is failing
        var pending = await InsertRowAsync(StartedAt, processedAt: null, cancellationToken);
        var pass = await worker.RunPassAsync(cancellationToken);

        // assert
        pass.ShouldBe(OutboxRelayPassResult.Succeeded);
        (await ReadProcessedAtAsync(pending, cancellationToken)).ShouldBe(StartedAt);
        (await ReadIdsAsync(cancellationToken)).ShouldContain(expired);

        // act: a later cleanup retries and drains the expired row
        worker.DeleteFault = null;
        var retried = await worker.RunCleanupAsync(cancellationToken);

        // assert
        retried.Result.ShouldBe(OutboxRelayCleanupResult.Succeeded);
        retried.DeletedCount.ShouldBe(1);
        (await ReadIdsAsync(cancellationToken)).ShouldBe([pending]);
    }

    [Fact]
    public async Task Cleanup_LeavesTheOutboxSchemaUnchanged()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var before = await ReadOutboxSchemaAsync(cancellationToken);
        await InsertRowAsync(StartedAt.AddDays(-2), Cutoff.AddDays(-1), cancellationToken);
        await using var provider = CreateRelayProvider();
        var worker = ActivatorUtilities.CreateInstance<OutboxRelayHostedService>(provider);

        // act
        var cleanup = await worker.RunCleanupAsync(cancellationToken);

        // assert
        cleanup.Result.ShouldBe(OutboxRelayCleanupResult.Succeeded);
        var after = await ReadOutboxSchemaAsync(cancellationToken);
        before.Columns.ShouldBe(["id", "payload", "created_at", "processed_at"]);
        after.Columns.ShouldBe(before.Columns);
        after.Indexes.ShouldBe(before.Indexes);
    }

    private ServiceProvider CreateRelayProvider(Dictionary<string, string?>? settings = null)
    {
        var values = new Dictionary<string, string?>
        {
            [$"{OutboxRelayOptions.SectionName}:PollInterval"] = "00:00:00.050",
        };
        foreach (var (key, value) in settings ?? [])
        {
            values[key] = value;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var queue = Substitute.For<IEnrichmentQueue>();
        queue.EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<LamuFlixDbContext>(options =>
            options.UseNpgsql(postgres.ConnectionString, builder => builder.UseVector()));
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(StartedAt));
        services.AddSingleton(queue);
        services.AddLogging();
        services.AddLamuFlixOutboxRelay();
        return services.BuildServiceProvider();
    }

    private async Task<long> InsertRowAsync(
        DateTimeOffset createdAt,
        DateTimeOffset? processedAt,
        CancellationToken cancellationToken)
    {
        await using var context = postgres.CreateMigratedContext();
        var record = new OutboxMessageRecord
        {
            Payload = JsonSerializer.SerializeToUtf8Bytes(
                new EnrichmentRequested(new MovieId(1), 1),
                SerializerOptions),
            CreatedAt = createdAt,
            ProcessedAt = processedAt,
        };
        context.OutboxMessages.Add(record);
        await context.SaveChangesAsync(cancellationToken);
        return record.Id;
    }

    private async Task<List<long>> ReadIdsAsync(CancellationToken cancellationToken)
    {
        await using var context = postgres.CreateMigratedContext();
        return await context.OutboxMessages
            .AsNoTracking()
            .OrderBy(message => message.Id)
            .Select(message => message.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<DateTimeOffset?> ReadProcessedAtAsync(long id, CancellationToken cancellationToken)
    {
        await using var context = postgres.CreateMigratedContext();
        return await context.OutboxMessages
            .Where(message => message.Id == id)
            .Select(message => message.ProcessedAt)
            .SingleAsync(cancellationToken);
    }

    private async Task<OutboxSchema> ReadOutboxSchemaAsync(CancellationToken cancellationToken)
    {
        await using var context = postgres.CreateMigratedContext();
        var columns = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT column_name AS "Value"
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'outbox_messages'
                ORDER BY ordinal_position
                """)
            .ToListAsync(cancellationToken);
        var indexes = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT indexdef AS "Value"
                FROM pg_indexes
                WHERE schemaname = 'public' AND tablename = 'outbox_messages'
                ORDER BY indexdef
                """)
            .ToListAsync(cancellationToken);
        return new OutboxSchema(columns, indexes);
    }

    private sealed record OutboxSchema(List<string> Columns, List<string> Indexes);

    private sealed class ScriptedRetentionWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxRelayOptions> options,
        TimeProvider timeProvider,
        IEnrichmentQueue queue,
        OutboxRelayState state,
        ILogger<OutboxRelayHostedService> logger)
        : OutboxRelayHostedService(scopeFactory, options, timeProvider, queue, state, logger)
    {
        public Func<IReadOnlyList<long>, Task> AfterReadExpired { get; set; } = _ => Task.CompletedTask;

        public Func<IServiceProvider, IReadOnlyList<long>, DateTimeOffset, CancellationToken, Task<int>>? DeleteFault
        {
            get;
            set;
        }

        internal override async Task<IReadOnlyList<long>> ReadExpiredProcessedRowIdsAsync(
            IServiceProvider scopedProvider,
            DateTimeOffset cutoff,
            CancellationToken cancellationToken)
        {
            var rowIds = await base.ReadExpiredProcessedRowIdsAsync(scopedProvider, cutoff, cancellationToken);
            if (rowIds.Count > 0)
            {
                await AfterReadExpired(rowIds);
            }

            return rowIds;
        }

        internal override Task<int> DeleteExpiredProcessedRowsAsync(
            IServiceProvider scopedProvider,
            IReadOnlyList<long> rowIds,
            DateTimeOffset cutoff,
            CancellationToken cancellationToken) =>
            DeleteFault is null
                ? base.DeleteExpiredProcessedRowsAsync(scopedProvider, rowIds, cutoff, cancellationToken)
                : DeleteFault(scopedProvider, rowIds, cutoff, cancellationToken);
    }
}
