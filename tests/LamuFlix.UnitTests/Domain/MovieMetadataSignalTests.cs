using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieMetadataSignalTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Keyword_PositiveIdAndName_ExposesBoth()
    {
        // act
        var keyword = new MovieKeyword(9715, "superhero");

        // assert
        keyword.Id.ShouldBe(9715);
        keyword.Name.ShouldBe("superhero");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public void Keyword_NonPositiveId_ThrowsArgumentOutOfRangeException(int id)
    {
        // act
        var thrown = Should.Throw<ArgumentOutOfRangeException>(() => new MovieKeyword(id, "name"));

        // assert
        thrown.ParamName.ShouldBe("id");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Keyword_BlankName_ThrowsArgumentException(string name)
    {
        // act
        var thrown = Should.Throw<ArgumentException>(() => new MovieKeyword(1, name));

        // assert
        thrown.ParamName.ShouldBe("name");
    }

    [Fact]
    public void Keyword_EqualValues_AreEqualWithEqualHash()
    {
        // arrange
        var left = new MovieKeyword(1, "heist");
        var right = new MovieKeyword(1, "heist");

        // act & assert
        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Theory]
    [InlineData(2, "heist")]
    [InlineData(1, "bank robbery")]
    public void Keyword_DifferingIdOrName_IsNotEqual(int id, string name)
    {
        // arrange
        var baseline = new MovieKeyword(1, "heist");

        // act
        var changed = new MovieKeyword(id, name);

        // assert
        changed.ShouldNotBe(baseline);
        baseline.ShouldNotBe(changed);
    }

    [Fact]
    public void Snapshot_NoArguments_IsUnavailableWithEmptySignals()
    {
        // act
        var snapshot = new MovieSignalSnapshot();

        // assert
        snapshot.KeywordAvailability.ShouldBe(SignalAvailability.Unavailable);
        snapshot.Keywords.ShouldBeEmpty();
        snapshot.FullCastAvailability.ShouldBe(SignalAvailability.Unavailable);
        snapshot.FullCast.ShouldBeEmpty();
        snapshot.CollectionAvailability.ShouldBe(SignalAvailability.Unavailable);
    }

    [Fact]
    public void Snapshot_DefaultFullCast_UsesOrdinalIgnoreCaseMembership()
    {
        // act
        var snapshot = new MovieSignalSnapshot();

        // assert
        snapshot.FullCast.KeyComparer.ShouldBe(StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Snapshot_AvailableKeywords_AreExposedInSuppliedOrder()
    {
        // act
        var snapshot = new MovieSignalSnapshot(
            SignalAvailability.Available,
            [new MovieKeyword(2, "sequel"), new MovieKeyword(1, "heist")]);

        // assert
        snapshot.KeywordAvailability.ShouldBe(SignalAvailability.Available);
        snapshot.Keywords.Select(keyword => keyword.Id).ShouldBe([2, 1]);
    }

    [Fact]
    public void Snapshot_AvailableKeywords_CopyInputSoLaterSourceMutationDoesNotLeak()
    {
        // arrange
        var keywords = new List<MovieKeyword> { new(1, "heist") };

        // act
        var snapshot = new MovieSignalSnapshot(SignalAvailability.Available, keywords);
        keywords.Add(new MovieKeyword(2, "sequel"));

        // assert
        snapshot.Keywords.Length.ShouldBe(1);
    }

    [Fact]
    public void Snapshot_AvailableKeywordsWithoutValues_ThrowsArgumentException()
    {
        // act
        var thrown = Should.Throw<ArgumentException>(
            () => new MovieSignalSnapshot(SignalAvailability.Available, []));

        // assert
        thrown.ParamName.ShouldBe("keywords");
    }

    [Theory]
    [InlineData(SignalAvailability.KnownEmpty)]
    [InlineData(SignalAvailability.Unavailable)]
    public void Snapshot_NonAvailableKeywordsWithValues_ThrowsArgumentException(SignalAvailability availability)
    {
        // act
        var thrown = Should.Throw<ArgumentException>(
            () => new MovieSignalSnapshot(availability, [new MovieKeyword(1, "heist")]));

        // assert
        thrown.ParamName.ShouldBe("keywords");
    }

    [Fact]
    public void Snapshot_DuplicateKeywordIds_ThrowsArgumentException()
    {
        // act
        var thrown = Should.Throw<ArgumentException>(
            () => new MovieSignalSnapshot(
                SignalAvailability.Available,
                [new MovieKeyword(1, "heist"), new MovieKeyword(1, "bank robbery")]));

        // assert
        thrown.ParamName.ShouldBe("keywords");
    }

    [Fact]
    public void Snapshot_NullKeywordEntry_ThrowsArgumentException()
    {
        // act
        var thrown = Should.Throw<ArgumentException>(
            () => new MovieSignalSnapshot(SignalAvailability.Available, [NullKeyword()]));

        // assert
        thrown.Message.ShouldContain("must not be null");
    }

    [Fact]
    public void Snapshot_AvailableFullCast_DedupesCaseInsensitively()
    {
        // act
        var snapshot = new MovieSignalSnapshot(
            fullCastAvailability: SignalAvailability.Available,
            fullCast: ["Actor A", "ACTOR A", "Actor B"]);

        // assert
        snapshot.FullCast.Count.ShouldBe(2);
        snapshot.FullCast.Contains("Actor A").ShouldBeTrue();
        snapshot.FullCast.Contains("actor b").ShouldBeTrue();
    }

    [Fact]
    public void Snapshot_AvailableFullCastCopy_IsDefensiveAgainstSourceMutation()
    {
        // arrange
        var cast = new List<string> { "Actor A" };

        // act
        var snapshot = new MovieSignalSnapshot(fullCastAvailability: SignalAvailability.Available, fullCast: cast);
        cast.Add("Actor B");

        // assert
        snapshot.FullCast.Count.ShouldBe(1);
    }

    [Fact]
    public void Snapshot_AvailableFullCastWithoutValues_ThrowsArgumentException()
    {
        // act
        var thrown = Should.Throw<ArgumentException>(
            () => new MovieSignalSnapshot(fullCastAvailability: SignalAvailability.Available));

        // assert
        thrown.ParamName.ShouldBe("fullCast");
    }

    [Fact]
    public void Snapshot_KnownEmptyFullCastWithValues_ThrowsArgumentException()
    {
        // act
        var thrown = Should.Throw<ArgumentException>(
            () => new MovieSignalSnapshot(fullCastAvailability: SignalAvailability.KnownEmpty, fullCast: ["Actor A"]));

        // assert
        thrown.ParamName.ShouldBe("fullCast");
    }

    [Fact]
    public void Snapshot_NullFullCastEntry_ThrowsArgumentException()
    {
        // act
        var thrown = Should.Throw<ArgumentException>(
            () => new MovieSignalSnapshot(
                fullCastAvailability: SignalAvailability.Available,
                fullCast: [NullName()]));

        // assert
        thrown.Message.ShouldContain("must not be null");
    }

    [Theory]
    [InlineData(SignalAvailability.KnownEmpty)]
    [InlineData(SignalAvailability.Available)]
    public void Snapshot_CollectionAvailability_IsCarriedWithoutIdentity(SignalAvailability availability)
    {
        // act
        var snapshot = new MovieSignalSnapshot(collectionAvailability: availability);

        // assert
        snapshot.CollectionAvailability.ShouldBe(availability);
        typeof(MovieSignalSnapshot).GetProperty("CollectionId").ShouldBeNull();
        typeof(MovieSignalSnapshot).GetProperty("CollectionName").ShouldBeNull();
    }

    [Fact]
    public void Snapshot_EquivalentKeywordSetsInDifferentOrder_AreEqualWithEqualHash()
    {
        // arrange
        var left = new MovieSignalSnapshot(
            SignalAvailability.Available,
            [new MovieKeyword(1, "heist"), new MovieKeyword(2, "sequel")]);
        var right = new MovieSignalSnapshot(
            SignalAvailability.Available,
            [new MovieKeyword(2, "sequel"), new MovieKeyword(1, "heist")]);

        // act & assert
        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void Snapshot_EquivalentFullCastCaseVariants_AreEqualWithEqualHash()
    {
        // arrange
        var left = new MovieSignalSnapshot(fullCastAvailability: SignalAvailability.Available, fullCast: ["Actor A"]);
        var right = new MovieSignalSnapshot(fullCastAvailability: SignalAvailability.Available, fullCast: ["actor a"]);

        // act & assert
        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void Snapshot_DifferingKeywordValues_IsNotEqual()
    {
        // arrange
        var baseline = new MovieSignalSnapshot(SignalAvailability.Available, [new MovieKeyword(1, "heist")]);

        // act
        var changed = new MovieSignalSnapshot(SignalAvailability.Available, [new MovieKeyword(1, "bank robbery")]);

        // assert
        changed.ShouldNotBe(baseline);
        baseline.ShouldNotBe(changed);
    }

    [Fact]
    public void Snapshot_DifferingAvailability_IsNotEqual()
    {
        // arrange
        var baseline = new MovieSignalSnapshot(keywordAvailability: SignalAvailability.KnownEmpty);

        // act
        var changed = new MovieSignalSnapshot(keywordAvailability: SignalAvailability.Unavailable);

        // assert
        changed.ShouldNotBe(baseline);
        baseline.ShouldNotBe(changed);
    }

    [Fact]
    public void Snapshot_DifferingCollectionAvailability_IsNotEqual()
    {
        // arrange
        var baseline = new MovieSignalSnapshot(collectionAvailability: SignalAvailability.KnownEmpty);

        // act
        var changed = new MovieSignalSnapshot(collectionAvailability: SignalAvailability.Available);

        // assert
        changed.ShouldNotBe(baseline);
        baseline.ShouldNotBe(changed);
    }

    [Fact]
    public void Snapshot_NullOther_IsNotEqual()
    {
        // arrange
        var snapshot = new MovieSignalSnapshot();

        // act & assert
        // ReSharper disable once RedundantCast // the cast selects the MovieSignalSnapshot? overload.
        snapshot.Equals((MovieSignalSnapshot?)null).ShouldBeFalse();
    }

    [Fact]
    public void Metadata_SignalsOmitted_HasNoSnapshot()
    {
        // act
        var metadata = new MovieMetadata("The Matrix");

        // assert
        metadata.Signals.ShouldBeNull();
    }

    [Fact]
    public void Metadata_WithSignals_CarriesSnapshotAndCollectionIdentitySeparately()
    {
        // arrange
        var signals = new MovieSignalSnapshot(
            SignalAvailability.Available,
            [new MovieKeyword(1, "heist")],
            SignalAvailability.Available,
            ["Actor A"],
            SignalAvailability.Available);

        // act
        var metadata = new MovieMetadata(
            "The Matrix",
            collectionId: 2344,
            collectionName: "The Matrix Collection",
            signals: signals);

        // assert
        metadata.Signals.ShouldBe(signals);
        metadata.CollectionId.ShouldBe(2344);
        metadata.CollectionName.ShouldBe("The Matrix Collection");
    }

    [Fact]
    public void Metadata_DifferingSignalSnapshot_IsNotEqualAndTracksChange()
    {
        // arrange
        var baseline = new MovieMetadata(
            "The Matrix",
            signals: new MovieSignalSnapshot(SignalAvailability.Available, [new MovieKeyword(1, "heist")]));
        var changed = new MovieMetadata(
            "The Matrix",
            signals: new MovieSignalSnapshot(SignalAvailability.Available, [new MovieKeyword(2, "sequel")]));

        // act & assert
        changed.ShouldNotBe(baseline);
        baseline.ShouldNotBe(changed);
    }

    [Fact]
    public void Metadata_EquivalentSignalsWithDifferentKeywordOrder_IsEqualWithEqualHash()
    {
        // arrange
        var left = new MovieMetadata(
            "The Matrix",
            releaseYear: new ReleaseYear(1999, Now),
            signals: new MovieSignalSnapshot(
                SignalAvailability.Available,
                [new MovieKeyword(1, "heist"), new MovieKeyword(2, "sequel")]));
        var right = new MovieMetadata(
            "The Matrix",
            releaseYear: new ReleaseYear(1999, Now),
            signals: new MovieSignalSnapshot(
                SignalAvailability.Available,
                [new MovieKeyword(2, "sequel"), new MovieKeyword(1, "heist")]));

        // act & assert
        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void Metadata_SignalSnapshotRemoved_IsNotEqual()
    {
        // arrange
        var withSignals = new MovieMetadata(
            "The Matrix",
            signals: new MovieSignalSnapshot(collectionAvailability: SignalAvailability.Available));
        var withoutSignals = new MovieMetadata("The Matrix");

        // act & assert
        withSignals.ShouldNotBe(withoutSignals);
        withoutSignals.ShouldNotBe(withSignals);
    }

    private static MovieKeyword NullKeyword()
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed // the null member is the subject of this test.
        return null!;
    }

    private static string NullName()
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed // the null member is the subject of this test.
        return null!;
    }
}
