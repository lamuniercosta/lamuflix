using System;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class EfCandidateEvidenceStoreTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task WriteAsync_ThenReadFromAnotherScope_ReturnsCompleteEvidence()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var evidence = new CandidateImdbEvidence(
            "tt0111161",
            42,
            8.1m,
            25000,
            CandidateEvidenceOutcome.Complete,
            Now.AddDays(30));

        await using (var writeContext = fixture.CreateMigratedContext())
        {
            await new EfCandidateEvidenceStore(writeContext, new FixedTimeProvider(Now))
                .WriteAsync(101, evidence, cancellationToken);
        }

        // act
        await using var readContext = fixture.CreateMigratedContext();
        var stored = await new EfCandidateEvidenceStore(readContext, new FixedTimeProvider(Now))
            .ReadAsync(101, cancellationToken);

        // assert
        stored.ShouldBe(evidence);
    }

    [Fact]
    public async Task WriteAsync_DuplicateTmdbId_UpdatesTheSingleRow()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateMigratedContext();
        var store = new EfCandidateEvidenceStore(context, new FixedTimeProvider(Now));
        await store.WriteAsync(102, Evidence(CandidateEvidenceOutcome.Complete, Now.AddDays(30)), cancellationToken);

        // act
        var replacement = new CandidateImdbEvidence(
            "tt0068646",
            7,
            9.2m,
            90000,
            CandidateEvidenceOutcome.Complete,
            Now.AddDays(30));
        await store.WriteAsync(102, replacement, cancellationToken);

        // assert
        (await context.CandidateEvidences.CountAsync(cancellationToken)).ShouldBe(1);
        (await store.ReadAsync(102, cancellationToken)).ShouldBe(replacement);
    }

    [Fact]
    public async Task WriteAsync_ConfirmedAbsence_UsesTheSevenDayExpiry()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateMigratedContext();
        var store = new EfCandidateEvidenceStore(context, new FixedTimeProvider(Now));

        // act
        await store.WriteAsync(
            103,
            Evidence(CandidateEvidenceOutcome.ConfirmedAbsence, Now.AddDays(7)),
            cancellationToken);
        var stored = await store.ReadAsync(103, cancellationToken);

        // assert
        stored.ShouldNotBeNull();
        stored.Outcome.ShouldBe(CandidateEvidenceOutcome.ConfirmedAbsence);
        stored.ExpiresAtUtc.ShouldBe(Now.AddDays(7));
    }

    [Fact]
    public async Task WriteAsync_UnresolvedEvidence_WritesNoDurableRow()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateMigratedContext();
        var store = new EfCandidateEvidenceStore(context, new FixedTimeProvider(Now));

        // act
        await store.WriteAsync(
            104,
            new CandidateImdbEvidence(null, null, 7.0m, null, CandidateEvidenceOutcome.Unresolved),
            cancellationToken);

        // assert
        (await context.CandidateEvidences.CountAsync(cancellationToken)).ShouldBe(0);
        (await store.ReadAsync(104, cancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task ReadAsync_ExpiredEvidence_IsMissingAndRemoved()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateMigratedContext();
        var store = new EfCandidateEvidenceStore(context, new FixedTimeProvider(Now));
        await store.WriteAsync(105, Evidence(CandidateEvidenceOutcome.Complete, Now.AddDays(30)), cancellationToken);

        // act
        var expiredStore = new EfCandidateEvidenceStore(context, new FixedTimeProvider(Now.AddDays(31)));
        var stored = await expiredStore.ReadAsync(105, cancellationToken);

        // assert
        stored.ShouldBeNull();
        (await context.CandidateEvidences.CountAsync(cancellationToken)).ShouldBe(0);
    }

    private static CandidateImdbEvidence Evidence(CandidateEvidenceOutcome outcome, DateTimeOffset expiresAtUtc) =>
        new("tt0133093", 3, 8.7m, 120000, outcome, expiresAtUtc);
}
