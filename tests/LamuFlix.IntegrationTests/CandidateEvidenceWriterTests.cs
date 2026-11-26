using System;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class CandidateEvidenceWriterTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 13, 0, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Theory]
    [InlineData("8.1", "25000")]
    [InlineData("5.0", null)]
    [InlineData(null, "12")]
    public async Task RecordAsync_DurableOutcome_PersistsCompleteEvidenceForThirtyDays(
        string? rating,
        string? votes)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateMigratedContext();
        var writer = Writer(context);

        // act
        var evidence = await writer.RecordAsync(
            201,
            new CandidateEvidencePayload(false, "tt0111161", rating, votes, 17),
            cancellationToken);
        var stored = await Store(context).ReadAsync(201, cancellationToken);

        // assert
        evidence.Outcome.ShouldBe(CandidateEvidenceOutcome.Complete);
        stored.ShouldBe(evidence);
        if (stored is not { } persisted)
        {
            throw new InvalidOperationException("Complete evidence was not persisted.");
        }

        persisted.ExpiresAtUtc.ShouldBe(Now.AddDays(30));
        persisted.ImdbId.ShouldBe("tt0111161");
        persisted.CollectionId.ShouldBe(17);
    }

    [Fact]
    public async Task RecordAsync_ConfirmedAbsence_PersistsSevenDayEvidence()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateMigratedContext();
        var writer = Writer(context);

        // act
        var evidence = await writer.RecordAsync(
            202,
            new CandidateEvidencePayload(true, null, null, null),
            cancellationToken);
        var stored = await Store(context).ReadAsync(202, cancellationToken);

        // assert
        evidence.Outcome.ShouldBe(CandidateEvidenceOutcome.ConfirmedAbsence);
        stored.ShouldBe(evidence);
        if (stored is not { } persisted)
        {
            throw new InvalidOperationException("Confirmed absence was not persisted.");
        }

        persisted.ExpiresAtUtc.ShouldBe(Now.AddDays(7));
    }

    [Theory]
    [InlineData("8.1", null)]
    [InlineData("N/A", "N/A")]
    [InlineData("6,5", "1000")]
    public async Task RecordAsync_UnresolvedOutcome_WritesNoRow(string rating, string? votes)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateMigratedContext();
        var writer = Writer(context);

        // act
        var evidence = await writer.RecordAsync(
            203,
            new CandidateEvidencePayload(false, null, rating, votes),
            cancellationToken);

        // assert
        evidence.Outcome.ShouldBe(CandidateEvidenceOutcome.Unresolved);
        (await context.CandidateEvidences.CountAsync(cancellationToken)).ShouldBe(0);
        (await Store(context).ReadAsync(203, cancellationToken)).ShouldBeNull();
    }

    private static CandidateEvidenceWriter Writer(LamuFlixDbContext context) =>
        new(Store(context), new FixedTimeProvider(Now));

    private static EfCandidateEvidenceStore Store(LamuFlixDbContext context) =>
        new(context, new FixedTimeProvider(Now));
}
