using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class OutsideLibraryEligibilityTests
{
    [Fact]
    public void Evaluate_KnownFloorFailures_AreDecisiveWithUnknownCounterpart()
    {
        // arrange
        var lowRating = Evidence(6.4m, null);
        var lowVotes = Evidence(null, 999);
        var lowRatingWithPassingVotes = Evidence(6.49m, 1000);
        var passingRatingWithLowVotes = Evidence(6.5m, 999);

        // act
        var verdicts = new[]
        {
            OutsideLibraryEligibility.Evaluate(lowRating),
            OutsideLibraryEligibility.Evaluate(lowVotes),
            OutsideLibraryEligibility.Evaluate(lowRatingWithPassingVotes),
            OutsideLibraryEligibility.Evaluate(passingRatingWithLowVotes),
        };

        // assert
        verdicts.ShouldAllBe(verdict => verdict == OutsideLibraryVerdict.Reject);
    }

    [Fact]
    public void Evaluate_FloorsAreInclusive()
    {
        // arrange
        var atFloors = Evidence(6.5m, 1000);
        var aboveFloors = Evidence(10.0m, 1001);
        var belowRating = Evidence(6.4m, 1000);
        var belowVotes = Evidence(6.5m, 999);

        // act
        var verdicts = new[]
        {
            OutsideLibraryEligibility.Evaluate(atFloors),
            OutsideLibraryEligibility.Evaluate(aboveFloors),
            OutsideLibraryEligibility.Evaluate(belowRating),
            OutsideLibraryEligibility.Evaluate(belowVotes),
        };

        // assert
        verdicts.ShouldBe([
            OutsideLibraryVerdict.Admit,
            OutsideLibraryVerdict.Admit,
            OutsideLibraryVerdict.Reject,
            OutsideLibraryVerdict.Reject,
        ]);
    }

    [Fact]
    public void Evaluate_ValidRatingWithUnknownVotes_Defers()
    {
        // arrange
        var evidence = new CandidateImdbEvidence("tt0111161", null, 7.5m, null, CandidateEvidenceOutcome.Unresolved);

        // act
        var verdict = OutsideLibraryEligibility.Evaluate(evidence);

        // assert
        verdict.ShouldBe(OutsideLibraryVerdict.Defer);
    }

    [Fact]
    public void Evaluate_ConfirmedAbsence_PreservesBaselineUnlessKnownFailureExists()
    {
        // arrange
        var confirmedAbsence = new CandidateImdbEvidence(
            null,
            null,
            null,
            null,
            CandidateEvidenceOutcome.ConfirmedAbsence);
        var confirmedAbsenceWithLowRating = new CandidateImdbEvidence(
            "tt0111161",
            null,
            3.0m,
            null,
            CandidateEvidenceOutcome.ConfirmedAbsence);

        // act
        var verdict = OutsideLibraryEligibility.Evaluate(confirmedAbsence);
        var withKnownFailure = OutsideLibraryEligibility.Evaluate(confirmedAbsenceWithLowRating);

        // assert
        verdict.ShouldBe(OutsideLibraryVerdict.Admit);
        withKnownFailure.ShouldBe(OutsideLibraryVerdict.Reject);
    }

    [Fact]
    public void Evaluate_UnresolvedOrAbsentEvidence_DefersWithoutANegativeVerdict()
    {
        // arrange
        var unresolved = new CandidateImdbEvidence(
            null,
            null,
            null,
            null,
            CandidateEvidenceOutcome.Unresolved);

        // act
        var verdicts = new[]
        {
            OutsideLibraryEligibility.Evaluate(null),
            OutsideLibraryEligibility.Evaluate(unresolved),
        };

        // assert
        verdicts.ShouldAllBe(verdict => verdict == OutsideLibraryVerdict.Defer);
    }

    [Fact]
    public void RankAdmitted_DeferredCandidate_RestoresOriginalSortPositionWhenEvidenceResolves()
    {
        // arrange
        var contributions = Contributions();
        var deferred = new Dictionary<int, CandidateImdbEvidence>
        {
            [201] = Evidence(null, null, CandidateEvidenceOutcome.Unresolved),
            [202] = Evidence(8.0m, 5000),
            [203] = Evidence(7.0m, 2000),
        };
        var resolved = new Dictionary<int, CandidateImdbEvidence>(deferred)
        {
            [201] = Evidence(9.0m, 9000),
        };

        // act
        var whileDeferred = OutsideLibraryRanking.RankAdmitted(Snapshot(contributions), Options(), deferred);
        var afterResolution = OutsideLibraryRanking.RankAdmitted(Snapshot(contributions), Options(), resolved);

        // assert
        whileDeferred.Select(card => card.TmdbId).ShouldBe([202, 203]);
        afterResolution.Select(card => card.TmdbId).ShouldBe([201, 202, 203]);
    }

    [Fact]
    public void RankAdmitted_KnownFloorFailure_AdmitsTheNextEligibleCandidate()
    {
        // arrange
        var contributions = Contributions();
        var evidence = new Dictionary<int, CandidateImdbEvidence>
        {
            [201] = Evidence(4.0m, 5000),
            [202] = Evidence(8.0m, 5000),
            [203] = Evidence(7.0m, 2000),
        };

        // act
        var cards = OutsideLibraryRanking.RankAdmitted(Snapshot(contributions), Options(), evidence);

        // assert
        cards.Select(card => card.TmdbId).ShouldBe([202, 203]);
    }

    [Fact]
    public void RankAdmitted_CompleteEvidence_HonoursTheTwentyCardCutOff()
    {
        // arrange
        var contributions = Enumerable.Range(1, 25)
            .SelectMany(source => new[]
            {
                Contribution(source, 100 + source, liked: source % 2 == 0),
                Contribution(source + 25, 100 + source, liked: source % 2 == 0),
            })
            .ToArray();
        var evidence = contributions
            .Select(contribution => contribution.TmdbId)
            .Distinct()
            .ToDictionary(id => id, _ => Evidence(8.0m, 5000));

        // act
        var cards = OutsideLibraryRanking.RankAdmitted(Snapshot(contributions), Options(), evidence);

        // assert
        var expected = Enumerable.Range(101, 25)
            .OrderByDescending(id => (id - 100) % 2 == 0 ? 4L : 2L)
            .ThenBy(id => id)
            .Take(20);

        cards.Count.ShouldBe(20);
        cards.Select(card => card.TmdbId).ShouldBe(expected);
    }

    [Fact]
    public void RankAdmitted_NullEvidence_ThrowsArgumentNullException()
    {
        // act
        var failure = Should.Throw<ArgumentNullException>(() =>
            OutsideLibraryRanking.RankAdmitted(Snapshot(Contributions()), Options(), null!));

        // assert
        failure.ParamName.ShouldBe("evidenceByTmdbId");
    }

    [Fact]
    public async Task RankAdmittedAsync_NullEvidence_ThrowsArgumentNullException()
    {
        // act
        var failure = await Should.ThrowAsync<ArgumentNullException>(() =>
            OutsideLibraryRanking.RankAdmittedAsync(
                Snapshot(Contributions()),
                Options(),
                null!,
                null,
                TestContext.Current.CancellationToken));

        // assert
        failure.ParamName.ShouldBe("evidenceByTmdbId");
    }

    [Fact]
    public void RankAdmitted_CustomTimeProvider_UsesProvidedClock()
    {
        // arrange
        var systemNow = TimeProvider.System.GetUtcNow();
        Dictionary<int, CandidateImdbEvidence> evidence = new()
        {
            [201] = Fresh(systemNow.AddDays(30)),
            [202] = Fresh(systemNow.AddDays(30)),
            [203] = Fresh(systemNow.AddDays(30)),
        };

        // act
        var cards = OutsideLibraryRanking.RankAdmitted(
            Snapshot(Contributions()),
            Options(),
            evidence,
            new FixedTimeProvider(systemNow.AddDays(60)));

        // assert
        cards.ShouldBeEmpty();
    }

    private static CandidateImdbEvidence Evidence(
        decimal? rating,
        int? votes,
        CandidateEvidenceOutcome outcome = CandidateEvidenceOutcome.Complete) =>
        new("tt0111161", null, rating, votes, outcome);

    private static CandidateImdbEvidence Fresh(DateTimeOffset expiresAtUtc) =>
        new("tt0111161", null, 8.0m, 5000, CandidateEvidenceOutcome.Complete, expiresAtUtc);

    private static OutsideLibraryRecommendationContribution[] Contributions() =>
    [
        Contribution(1, 201, liked: true),
        Contribution(2, 201, liked: true),
        Contribution(1, 202, liked: true),
        Contribution(3, 202, liked: false),
        Contribution(1, 203, liked: false),
        Contribution(4, 203, liked: false),
    ];

    private static OutsideLibraryRecommendationContribution Contribution(
        int sourceId,
        int tmdbId,
        bool liked) =>
        new(
            new MovieId(sourceId),
            $"Seed {sourceId}",
            liked ? Review.Liked : Review.None,
            IsWatched: !liked,
            IsInWatchlist: false,
            tmdbId,
            $"Candidate {tmdbId}",
            2001,
            null);

    private static OutsideLibraryRecommendationSnapshot Snapshot(
        IEnumerable<OutsideLibraryRecommendationContribution> contributions) =>
        new([.. contributions], []);

    private static RecommendationOptions Options() =>
        new() { DiscoveryWeightLiked = 2, DiscoveryWeightWatched = 1 };
}
