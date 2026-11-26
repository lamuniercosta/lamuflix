using System;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Infrastructure.Adapters;

namespace LamuFlix.UnitTests.Adapters;

public sealed class CandidateEvidenceParserTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);

    [Fact]
    public void Parse_AtBothFloors_IsCompletePassFreshForThirtyDays()
    {
        // arrange
        var payload = new CandidateEvidencePayload(false, "tt0111161", "6.5", "1000", 12);

        // act
        var evidence = CandidateEvidenceParser.Parse(payload, Now);

        // assert
        evidence.Outcome.ShouldBe(CandidateEvidenceOutcome.Complete);
        evidence.HasKnownFloorFailure.ShouldBeFalse();
        evidence.Rating.ShouldBe(6.5m);
        evidence.Votes.ShouldBe(1000);
        evidence.ImdbId.ShouldBe("tt0111161");
        evidence.CollectionId.ShouldBe(12);
        evidence.ExpiresAtUtc.ShouldBe(Now.AddDays(30));
    }

    [Theory]
    [InlineData("6.4", "1000")]
    [InlineData("6.5", "999")]
    [InlineData("0", "0")]
    public void Parse_KnownValueBelowFloor_IsCompleteFailureFreshForThirtyDays(string rating, string votes)
    {
        // act
        var evidence = CandidateEvidenceParser.Parse(new CandidateEvidencePayload(false, null, rating, votes), Now);

        // assert
        evidence.Outcome.ShouldBe(CandidateEvidenceOutcome.Complete);
        evidence.HasKnownFloorFailure.ShouldBeTrue();
        evidence.ExpiresAtUtc.ShouldBe(Now.AddDays(30));
    }

    [Theory]
    [InlineData("5.0", null)]
    [InlineData(null, "12")]
    public void Parse_KnownFailureWithUnknownCounterpart_IsCompleteFailure(string? rating, string? votes)
    {
        // act
        var evidence = CandidateEvidenceParser.Parse(new CandidateEvidencePayload(false, null, rating, votes), Now);

        // assert
        evidence.Outcome.ShouldBe(CandidateEvidenceOutcome.Complete);
        evidence.HasKnownFloorFailure.ShouldBeTrue();
        evidence.ExpiresAtUtc.ShouldBe(Now.AddDays(30));
    }

    [Fact]
    public void Parse_ProviderConfirmedAbsence_IsPreservedForSevenDays()
    {
        // act
        var evidence = CandidateEvidenceParser.Parse(new CandidateEvidencePayload(true, null, null, null), Now);

        // assert
        evidence.Outcome.ShouldBe(CandidateEvidenceOutcome.ConfirmedAbsence);
        evidence.HasKnownFloorFailure.ShouldBeFalse();
        evidence.ExpiresAtUtc.ShouldBe(Now.AddDays(7));
    }

    [Fact]
    public void Parse_ConfirmedAbsenceWithKnownFailure_KnownFailureWins()
    {
        // act
        var evidence = CandidateEvidenceParser.Parse(new CandidateEvidencePayload(true, null, "6.4", null), Now);

        // assert
        evidence.Outcome.ShouldBe(CandidateEvidenceOutcome.Complete);
        evidence.HasKnownFloorFailure.ShouldBeTrue();
        evidence.ExpiresAtUtc.ShouldBe(Now.AddDays(30));
    }

    [Fact]
    public void Parse_ValidRatingWithUnknownVotes_IsUnresolvedWithoutExpiry()
    {
        // act
        var evidence = CandidateEvidenceParser.Parse(new CandidateEvidencePayload(false, "tt0111161", "8.1", null), Now);

        // assert
        evidence.Outcome.ShouldBe(CandidateEvidenceOutcome.Unresolved);
        evidence.Rating.ShouldBe(8.1m);
        evidence.Votes.ShouldBeNull();
        evidence.ExpiresAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Parse_MalformedOrTransientValues_AreUnresolvedNotConfirmedAbsence()
    {
        // act
        var evidence = CandidateEvidenceParser.Parse(new CandidateEvidencePayload(false, null, "N/A", "N/A"), Now);

        // assert
        evidence.Outcome.ShouldBe(CandidateEvidenceOutcome.Unresolved);
        evidence.Rating.ShouldBeNull();
        evidence.Votes.ShouldBeNull();
        evidence.ExpiresAtUtc.ShouldBeNull();
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("N/A", null)]
    [InlineData("n/a", null)]
    [InlineData("6,5", null)]
    [InlineData("6.5/10", null)]
    [InlineData("1e1", null)]
    [InlineData("+6.5", null)]
    [InlineData("-1", null)]
    [InlineData("11", null)]
    [InlineData("999999999999999999999999999999", null)]
    [InlineData(" 6.5 ", 6.5)]
    public void Parse_AdversarialRating_UsesStrictGrammarAndBounds(string rating, double? expected)
    {
        var expectedRating = expected is { } value ? (decimal?)value : null;

        // act
        var evidence = CandidateEvidenceParser.Parse(new CandidateEvidencePayload(false, null, rating, "1000"), Now);

        // assert
        evidence.Rating.ShouldBe(expectedRating);
        if (expectedRating is null)
        {
            evidence.Outcome.ShouldBe(CandidateEvidenceOutcome.Unresolved);
        }
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("N/A", null)]
    [InlineData("1,000", null)]
    [InlineData("1 000", null)]
    [InlineData("-1", null)]
    [InlineData("2147483648", null)]
    [InlineData(" 1000 ", 1000)]
    public void Parse_AdversarialVotes_UsesStrictGrammar(string votes, int? expected)
    {
        // act
        var evidence = CandidateEvidenceParser.Parse(new CandidateEvidencePayload(false, null, "7.0", votes), Now);

        // assert
        evidence.Votes.ShouldBe(expected);
        if (expected is null)
        {
            evidence.Outcome.ShouldBe(CandidateEvidenceOutcome.Unresolved);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Parse_NonPositiveCollectionId_IsAbsent(int collectionId)
    {
        // act
        var evidence = CandidateEvidenceParser.Parse(
            new CandidateEvidencePayload(false, null, "7.0", "1000", collectionId),
            Now);

        // assert
        evidence.CollectionId.ShouldBeNull();
    }
}
