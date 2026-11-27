using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.UnitTests.Features;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class OutsideLibraryEvidenceLookupTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ResolveAsync_WithoutAsyncResolver_FallsBackToSyncResolver()
    {
        // arrange
        Dictionary<int, CandidateImdbEvidence> cached = new()
        {
            [201] = Expired(),
        };
        var lookup = new OutsideLibraryEvidenceLookup(
            cached,
            Now,
            _ => Fresh());

        // act
        var resolved = await lookup.ResolveAsync(201, TestContext.Current.CancellationToken);

        // assert
        resolved.ShouldBe(Fresh());
    }

    [Fact]
    public void Peek_EqualExpiry_PrefersCachedBaseline()
    {
        // arrange
        Dictionary<int, CandidateImdbEvidence> cached = new()
        {
            [201] = Baseline(),
        };
        Dictionary<int, CandidateImdbEvidence> fresher = new()
        {
            [201] = FresherSameExpiry(),
        };
        var lookup = new OutsideLibraryEvidenceLookup(cached, Now, fresherEvidence: fresher);

        // act
        var peeked = lookup.Peek(201);

        // assert
        peeked.ShouldBe(Baseline());
    }

    [Fact]
    public void Peek_ExpiryExactlyNow_IsStale()
    {
        // arrange
        Dictionary<int, CandidateImdbEvidence> cached = new()
        {
            [201] = new CandidateImdbEvidence(
                "tt0111161",
                null,
                8.0m,
                5000,
                CandidateEvidenceOutcome.Complete,
                Now),
        };
        var lookup = new OutsideLibraryEvidenceLookup(cached, Now);

        // act
        var peeked = lookup.Peek(201);

        // assert
        peeked.ShouldBeNull();
    }

    [Fact]
    public void Resolve_ExpiryExactlyNow_FallsBackToSyncResolver()
    {
        // arrange
        List<int> lookups = [];
        Dictionary<int, CandidateImdbEvidence> cached = new()
        {
            [201] = new CandidateImdbEvidence(
                "tt0111161",
                null,
                8.0m,
                5000,
                CandidateEvidenceOutcome.Complete,
                Now),
        };
        var lookup = new OutsideLibraryEvidenceLookup(
            cached,
            Now,
            tmdbId =>
            {
                lookups.Add(tmdbId);
                return Fresh();
            });

        // act
        var resolved = lookup.Resolve(201);

        // assert
        resolved.ShouldBe(Fresh());
        lookups.ShouldBe([201]);
    }

    private static CandidateImdbEvidence Expired() =>
        new("tt0111161", null, 8.0m, 5000, CandidateEvidenceOutcome.Complete, Now.AddDays(-1));

    private static CandidateImdbEvidence Fresh() =>
        new("tt0111161", null, 8.0m, 5000, CandidateEvidenceOutcome.Complete, Now.AddDays(30));

    private static CandidateImdbEvidence Baseline() =>
        new("tt0000001", 12, 8.0m, 5000, CandidateEvidenceOutcome.Complete, Now.AddDays(30));

    private static CandidateImdbEvidence FresherSameExpiry() =>
        new("tt0000002", 99, 9.0m, 9000, CandidateEvidenceOutcome.Complete, Now.AddDays(30));
}
