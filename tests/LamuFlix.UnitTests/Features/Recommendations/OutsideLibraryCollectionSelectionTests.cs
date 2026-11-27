using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class OutsideLibraryCollectionSelectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Admit_FirstEligibleTitleRepresentsItsCollection()
    {
        // arrange
        Dictionary<int, CandidateImdbEvidence> evidence = new()
        {
            [201] = Complete(12),
            [202] = Complete(12),
        };
        var lookup = new OutsideLibraryEvidenceLookup(evidence, Now);

        // act
        var cards = OutsideLibraryCollectionSelection.Admit(
            Snapshot(Contributions(201, 2)),
            Options(),
            lookup);

        // assert
        cards.Select(card => card.TmdbId).ShouldBe([201]);
    }

    [Fact]
    public void Admit_KnownDuplicateCollection_IsSkippedBeforeAnyLookup()
    {
        // arrange
        List<int> lookups = [];
        Dictionary<int, CandidateImdbEvidence> evidence = new()
        {
            [201] = Complete(77),
            [202] = Complete(77),
        };
        var lookup = new OutsideLibraryEvidenceLookup(
            evidence,
            Now,
            tmdbId =>
            {
                lookups.Add(tmdbId);
                return null;
            });

        // act
        var cards = OutsideLibraryCollectionSelection.Admit(
            Snapshot(Contributions(201, 2)),
            Options(),
            lookup);

        // assert
        cards.Select(card => card.TmdbId).ShouldBe([201]);
        lookups.ShouldBeEmpty();
    }

    [Fact]
    public void Admit_UncollectedTitles_RemainIndependentlyEligible()
    {
        // arrange
        Dictionary<int, CandidateImdbEvidence> evidence = new()
        {
            [201] = Complete(null),
            [202] = Complete(null),
            [203] = Complete(null),
        };
        var lookup = new OutsideLibraryEvidenceLookup(evidence, Now);

        // act
        var cards = OutsideLibraryCollectionSelection.Admit(
            Snapshot(Contributions(201, 3)),
            Options(),
            lookup);

        // assert
        cards.Select(card => card.TmdbId).ShouldBe([201, 202, 203]);
    }

    [Fact]
    public void Admit_TwentyRepresentatives_IsTheVisibleCap()
    {
        // arrange
        var evidence = Enumerable.Range(300, 25)
            .ToDictionary(id => id, id => Complete(1000 + id));
        var lookup = new OutsideLibraryEvidenceLookup(evidence, Now);

        // act
        var cards = OutsideLibraryCollectionSelection.Admit(
            Snapshot(Contributions(300, 25)),
            Options(),
            lookup);

        // assert
        cards.Count.ShouldBe(20);
        cards.Select(card => card.TmdbId).ShouldBe(Enumerable.Range(300, 20));
    }

    [Fact]
    public void Admit_BackfillsBeyondTheFormerTopTwentyCutOff()
    {
        // arrange
        var evidence = Enumerable.Range(400, 25)
            .ToDictionary(id => id, id => id < 420 ? Failure() : Complete(null));
        var lookup = new OutsideLibraryEvidenceLookup(evidence, Now);

        // act
        var cards = OutsideLibraryCollectionSelection.Admit(
            Snapshot(Contributions(400, 25)),
            Options(),
            lookup);

        // assert
        cards.Select(card => card.TmdbId).ShouldBe(Enumerable.Range(420, 5));
    }

    [Fact]
    public void Admit_MembershipKnownDuringResolution_IsRecheckedBeforeAdmission()
    {
        // arrange
        List<int> lookups = [];
        var evidence = new Dictionary<int, CandidateImdbEvidence>
        {
            [201] = Complete(45),
        };
        var lookup = new OutsideLibraryEvidenceLookup(
            evidence,
            Now,
            tmdbId =>
            {
                lookups.Add(tmdbId);
                return Complete(45);
            });

        // act
        var cards = OutsideLibraryCollectionSelection.Admit(
            Snapshot(
            [
                Contribution(1, 201, liked: true),
                Contribution(2, 201, liked: true),
                Contribution(3, 202, liked: false),
                Contribution(4, 202, liked: false),
            ]),
            Options(),
            lookup);

        // assert
        cards.Select(card => card.TmdbId).ShouldBe([201]);
        lookups.ShouldBe([202]);
    }

    [Fact]
    public void Admit_FreshCachedMembership_IsUsedUntilExpiryWithoutARefresh()
    {
        // arrange
        List<int> lookups = [];
        var evidence = new Dictionary<int, CandidateImdbEvidence>
        {
            [201] = Complete(12, Now.AddDays(30)),
            [202] = Complete(12, Now.AddDays(30)),
        };
        var lookup = new OutsideLibraryEvidenceLookup(
            evidence,
            Now,
            tmdbId =>
            {
                lookups.Add(tmdbId);
                return null;
            });

        // act
        var cards = OutsideLibraryCollectionSelection.Admit(
            Snapshot(Contributions(201, 2)),
            Options(),
            lookup);

        // assert
        cards.Select(card => card.TmdbId).ShouldBe([201]);
        lookups.ShouldBeEmpty();
    }

    [Fact]
    public void Admit_FresherLocalMembership_WinsWithoutARemoteRefresh()
    {
        // arrange
        List<int> lookups = [];
        var evidence = new Dictionary<int, CandidateImdbEvidence>
        {
            [201] = Complete(12, Now.AddDays(30)),
            [202] = Complete(99, Now.AddDays(1)),
        };
        Dictionary<int, CandidateImdbEvidence> fresher = new()
        {
            [202] = Complete(12, Now.AddDays(30)),
        };
        var lookup = new OutsideLibraryEvidenceLookup(
            evidence,
            Now,
            tmdbId =>
            {
                lookups.Add(tmdbId);
                return null;
            },
            fresher);

        // act
        var cards = OutsideLibraryCollectionSelection.Admit(
            Snapshot(Contributions(201, 2)),
            Options(),
            lookup);

        // assert
        cards.Select(card => card.TmdbId).ShouldBe([201]);
        lookups.ShouldBeEmpty();
    }

    [Fact]
    public void Admit_ExpiredCachedMembership_AllowsResolutionAndStillSkipsADuplicate()
    {
        // arrange
        List<int> lookups = [];
        var evidence = new Dictionary<int, CandidateImdbEvidence>
        {
            [201] = Complete(12, Now.AddDays(30)),
            [202] = Complete(12, Now.AddDays(-1)),
        };
        var lookup = new OutsideLibraryEvidenceLookup(
            evidence,
            Now,
            tmdbId =>
            {
                lookups.Add(tmdbId);
                return Complete(12, Now.AddDays(30));
            });

        // act
        var cards = OutsideLibraryCollectionSelection.Admit(
            Snapshot(Contributions(201, 2)),
            Options(),
            lookup);

        // assert
        cards.Select(card => card.TmdbId).ShouldBe([201]);
        lookups.ShouldBe([202]);
    }

    [Fact]
    public void Admit_NullSnapshot_ThrowsArgumentNullException()
    {
        // act
        var failure = Should.Throw<ArgumentNullException>(() =>
            // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
            OutsideLibraryCollectionSelection.Admit(null!, Options(), new OutsideLibraryEvidenceLookup(new Dictionary<int, CandidateImdbEvidence>(), Now)));

        // assert
        failure.ParamName.ShouldBe("snapshot");
    }

    [Fact]
    public void Admit_NullOptions_ThrowsArgumentNullException()
    {
        // act
        var failure = Should.Throw<ArgumentNullException>(() =>
            OutsideLibraryCollectionSelection.Admit(
                Snapshot(Contributions(201, 2)),
                // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
                null!,
                new OutsideLibraryEvidenceLookup(new Dictionary<int, CandidateImdbEvidence>(), Now)));

        // assert
        failure.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Admit_NullEvidence_ThrowsArgumentNullException()
    {
        // act
        var failure = Should.Throw<ArgumentNullException>(() =>
            // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
            OutsideLibraryCollectionSelection.Admit(Snapshot(Contributions(201, 2)), Options(), null!));

        // assert
        failure.ParamName.ShouldBe("evidence");
    }

    [Fact]
    public async Task AdmitAsync_NullSnapshot_ThrowsArgumentNullException()
    {
        // act
        var failure = await Should.ThrowAsync<ArgumentNullException>(() =>
            OutsideLibraryCollectionSelection.AdmitAsync(
                // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
                null!,
                Options(),
                new OutsideLibraryEvidenceLookup(new Dictionary<int, CandidateImdbEvidence>(), Now),
                TestContext.Current.CancellationToken));

        // assert
        failure.ParamName.ShouldBe("snapshot");
    }

    [Fact]
    public async Task AdmitAsync_NullOptions_ThrowsArgumentNullException()
    {
        // act
        var failure = await Should.ThrowAsync<ArgumentNullException>(() =>
            OutsideLibraryCollectionSelection.AdmitAsync(
                Snapshot(Contributions(201, 2)),
                // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
                null!,
                new OutsideLibraryEvidenceLookup(new Dictionary<int, CandidateImdbEvidence>(), Now),
                TestContext.Current.CancellationToken));

        // assert
        failure.ParamName.ShouldBe("options");
    }

    [Fact]
    public async Task AdmitAsync_NullEvidence_ThrowsArgumentNullException()
    {
        // act
        var failure = await Should.ThrowAsync<ArgumentNullException>(() =>
            OutsideLibraryCollectionSelection.AdmitAsync(
                Snapshot(Contributions(201, 2)),
                Options(),
                // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
                null!,
                TestContext.Current.CancellationToken));

        // assert
        failure.ParamName.ShouldBe("evidence");
    }

    [Fact]
    public async Task AdmitAsync_TwentyRepresentatives_IsTheVisibleCap()
    {
        // arrange
        var evidence = Enumerable.Range(300, 25)
            .ToDictionary(id => id, id => Complete(1000 + id));
        var lookup = new OutsideLibraryEvidenceLookup(evidence, Now);

        // act
        var cards = await OutsideLibraryCollectionSelection.AdmitAsync(
            Snapshot(Contributions(300, 25)),
            Options(),
            lookup,
            TestContext.Current.CancellationToken);

        // assert
        cards.Count.ShouldBe(20);
        cards.Select(card => card.TmdbId).ShouldBe(Enumerable.Range(300, 20));
    }

    [Fact]
    public async Task AdmitAsync_KnownDuplicateCollection_IsSkippedBeforeAnyLookup()
    {
        // arrange
        List<int> lookups = [];
        Dictionary<int, CandidateImdbEvidence> evidence = new()
        {
            [201] = Complete(77),
            [202] = Complete(77),
        };
        var lookup = new OutsideLibraryEvidenceLookup(
            evidence,
            Now,
            tmdbId =>
            {
                lookups.Add(tmdbId);
                return null;
            });

        // act
        var cards = await OutsideLibraryCollectionSelection.AdmitAsync(
            Snapshot(Contributions(201, 2)),
            Options(),
            lookup,
            TestContext.Current.CancellationToken);

        // assert
        cards.Select(card => card.TmdbId).ShouldBe([201]);
        lookups.ShouldBeEmpty();
    }

    [Fact]
    public void Admit_ZeroCollectionId_RemainsIndependentlyEligible()
    {
        // arrange
        Dictionary<int, CandidateImdbEvidence> evidence = new()
        {
            [201] = Complete(0),
            [202] = Complete(0),
        };
        var lookup = new OutsideLibraryEvidenceLookup(evidence, Now);

        // act
        var cards = OutsideLibraryCollectionSelection.Admit(
            Snapshot(Contributions(201, 2)),
            Options(),
            lookup);

        // assert
        cards.Select(card => card.TmdbId).ShouldBe([201, 202]);
    }

    private static CandidateImdbEvidence Complete(int? collectionId, DateTimeOffset? expiresAtUtc = null) =>
        new(
            "tt0111161",
            collectionId,
            8.0m,
            5000,
            CandidateEvidenceOutcome.Complete,
            expiresAtUtc ?? Now.AddDays(30));

    private static CandidateImdbEvidence Failure() =>
        new("tt0111161", null, 3.0m, 5000, CandidateEvidenceOutcome.Complete, Now.AddDays(30));

    private static IEnumerable<OutsideLibraryRecommendationContribution> Contributions(
        int firstTmdbId,
        int count) =>
        Enumerable.Range(0, count)
            .SelectMany(index => (OutsideLibraryRecommendationContribution[])
            [
                Contribution(index * 2 + 1, firstTmdbId + index, liked: true),
                Contribution(index * 2 + 2, firstTmdbId + index, liked: true),
            ]);

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
