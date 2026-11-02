using System;
using System.Collections.Immutable;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class MovieFeaturesPropertyTests
{
    [Property(MaxTest = 100, Replay = "336001,100003")]
    [Trait("Category", "Property")]
    public bool Construction_PermutedCaseVariedDuplicatedSets_CompareEqualWithEqualHash(
        string[]? genres,
        string[]? directors,
        string[]? writers,
        string[]? cast)
    {
        // arrange
        var left = new MovieFeatures(Expand(genres), Expand(directors), Expand(writers), Expand(cast));
        var right = new MovieFeatures(Vary(genres), Vary(directors), Vary(writers), Vary(cast));

        // act & assert
        return left.Equals(right)
            && right.Equals(left)
            && left.GetHashCode() == right.GetHashCode();
    }

    [Property(MaxTest = 100, Replay = "336002,200003")]
    [Trait("Category", "Property")]
    public bool Construction_NullElementInEachSet_IsPreserved(
        string[]? genres,
        string[]? directors,
        string[]? writers,
        string[]? cast)
    {
        // arrange
        var features = new MovieFeatures(Expand(genres), Expand(directors), Expand(writers), Expand(cast));

        // act & assert
        return ContainsNull(features.Genres)
            && ContainsNull(features.Directors)
            && ContainsNull(features.Writers)
            && ContainsNull(features.Cast);
    }

    [Property(MaxTest = 100, Replay = "336003,300007")]
    [Trait("Category", "Property")]
    public bool Construction_RepeatedNullElements_CollapseToOneMemberInEachSet(
        string[]? genres,
        string[]? directors,
        string[]? writers,
        string[]? cast)
    {
        // arrange
        var features = new MovieFeatures(RepeatNull(genres), RepeatNull(directors), RepeatNull(writers), RepeatNull(cast));

        // act & assert
        return NullCount(features.Genres) == 1
            && NullCount(features.Directors) == 1
            && NullCount(features.Writers) == 1
            && NullCount(features.Cast) == 1;
    }

    [Property(MaxTest = 100, Replay = "336004,400009")]
    [Trait("Category", "Property")]
    public bool From_PermutedCaseVariedDuplicatedCollections_MapToEqualValuesWithEqualHash(
        string[]? genres,
        string[]? directors,
        string[]? writers,
        string[]? actors)
    {
        // arrange
        var left = MovieFeaturesMapper.From(CreateMovie(Expand(genres), Expand(directors), Expand(writers), Expand(actors)));
        var right = MovieFeaturesMapper.From(CreateMovie(Vary(genres), Vary(directors), Vary(writers), Vary(actors)));

        // act & assert
        return left.Equals(right)
            && right.Equals(left)
            && left.GetHashCode() == right.GetHashCode();
    }

    [Property(MaxTest = 100, Replay = "336005,500001")]
    [Trait("Category", "Property")]
    public bool From_NullElementInEachMappedCollection_IsPreserved(
        string[]? genres,
        string[]? directors,
        string[]? writers,
        string[]? actors)
    {
        // arrange
        var features = MovieFeaturesMapper.From(
            CreateMovie(Expand(genres), Expand(directors), Expand(writers), Expand(actors)));

        // act & assert
        return ContainsNull(features.Genres)
            && ContainsNull(features.Directors)
            && ContainsNull(features.Writers)
            && ContainsNull(features.Cast);
    }

    [Property(MaxTest = 100, Replay = "336006,600011")]
    [Trait("Category", "Property")]
    public bool From_RepeatedNullElements_CollapseToOneMemberInEachMappedSet(
        string[]? genres,
        string[]? directors,
        string[]? writers,
        string[]? actors)
    {
        // arrange
        var features = MovieFeaturesMapper.From(
            CreateMovie(RepeatNull(genres), RepeatNull(directors), RepeatNull(writers), RepeatNull(actors)));

        // act & assert
        return NullCount(features.Genres) == 1
            && NullCount(features.Directors) == 1
            && NullCount(features.Writers) == 1
            && NullCount(features.Cast) == 1;
    }

    [Property(MaxTest = 100, Replay = "336007,700003")]
    [Trait("Category", "Property")]
    public bool From_EquivalentMappedValuesWithNullMember_AreEqualWithEqualHash(
        string[]? genres,
        string[]? directors,
        string[]? writers,
        string[]? actors)
    {
        // arrange
        var left = MovieFeaturesMapper.From(CreateMovie(Expand(genres), Expand(directors), Expand(writers), Expand(actors)));
        var right = MovieFeaturesMapper.From(CreateMovie(Vary(genres), Vary(directors), Vary(writers), Vary(actors)));

        // act & assert
        return ContainsNull(left.Genres)
            && left.Equals(right)
            && left.GetHashCode() == right.GetHashCode();
    }

    [Property(MaxTest = 100, Replay = "336008,800009")]
    [Trait("Category", "Property")]
    public bool Construction_PermutedDuplicatedKeywordIds_CompareEqualWithEqualHash(int[]? keywords)
    {
        // arrange
        var left = new MovieFeatures(keywords: ExpandKeywords(keywords));
        var right = new MovieFeatures(keywords: VaryKeywords(keywords));

        // act & assert
        return left.Equals(right)
            && right.Equals(left)
            && left.GetHashCode() == right.GetHashCode();
    }

    [Property(MaxTest = 100, Replay = "336009,900007")]
    [Trait("Category", "Property")]
    public bool Construction_AnyCollectionId_RoundTripsAndEqualityTracksValue(int seed)
    {
        // arrange
        var collectionId = (int)(Math.Abs((long)seed) % 100_000) + 1;
        var left = new MovieFeatures(collectionId: collectionId);
        var right = new MovieFeatures(collectionId: collectionId);
        var different = new MovieFeatures(collectionId: collectionId + 1);

        // act & assert
        return left.CollectionId == collectionId
            && left.Equals(right)
            && left.GetHashCode() == right.GetHashCode()
            && !left.Equals(different);
    }

    [Property(MaxTest = 100, Replay = "336010,100019")]
    [Trait("Category", "Property")]
    public bool From_EnabledSignals_MergeFullCastIntoBaseCast(int[]? keywords, int castSeed)
    {
        // arrange
        var positiveKeywords = PositiveKeywords(keywords);
        var fullCast = new[] { "Actor " + castSeed, "ACTOR " + castSeed };
        var metadata = new MovieMetadata(
            "Title",
            actors: ["Base Actor"],
            collectionId: 42,
            collectionName: "Collection",
            signals: new MovieSignalSnapshot(
                SignalAvailability.Available,
                positiveKeywords.Select(id => new MovieKeyword(id, "name " + id)),
                SignalAvailability.Available,
                fullCast,
                SignalAvailability.Available));

        // act
        var disabled = MovieFeaturesMapper.From(CreateMovie(metadata));
        var enabled = MovieFeaturesMapper.From(CreateMovie(metadata), includeSignals: true);

        // assert
        return disabled.Cast.SetEquals(["Base Actor"])
            && disabled.Keywords.Count == 0
            && disabled.CollectionId is null
            && enabled.Cast.SetEquals(["Base Actor", fullCast[0]])
            && enabled.Keywords.SetEquals(positiveKeywords)
            && enabled.CollectionId == 42;
    }

    private static int[] ExpandKeywords(int[]? values) =>
        values is null || values.Length == 0 ? [] : [.. values, values[0]];

    private static int[] VaryKeywords(int[]? values)
    {
        var source = values ?? [];
        return [.. source.Reverse(), .. source];
    }

    private static int[] PositiveKeywords(int[]? keywords)
    {
        var positive = (keywords ?? []).Where(id => id > 0).Distinct().Order().ToArray();
        return positive.Length == 0 ? [1] : positive;
    }

    private static Movie CreateMovie(MovieMetadata metadata) =>
        Movie.Rehydrate(
            new MovieId(1),
            "Title",
            new LibraryPath("C:/library/title.mkv"),
            new MediaFormat("mkv"),
            false,
            metadata,
            EnrichmentStatus.Enriched,
            new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero),
            1,
            null,
            null);

    private static Movie CreateMovie(
        string[]? genres,
        string[]? directors,
        string[]? writers,
        string[]? actors) =>
        Movie.Rehydrate(
            new MovieId(1),
            "Title",
            new LibraryPath("C:/library/title.mkv"),
            new MediaFormat("mkv"),
            false,
            new MovieMetadata(
                "Title",
                genres: genres,
                directors: directors,
                writers: writers,
                actors: actors),
            EnrichmentStatus.Enriched,
            new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero),
            1,
            null,
            null);

    private static string[] Expand(string[]? values) =>
        [.. (values ?? []), NullElement()];

    private static string[] RepeatNull(string[]? values) =>
        [.. (values ?? []), NullElement(), NullElement()];

    private static string[] Vary(string[]? values)
    {
        var source = values ?? [];
        var combined = source
            .Concat(source.Reverse())
            .Select(VaryAsciiCase)
            .Append(NullElement());
        return [.. combined];
    }

    private static string NullElement()
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed // the null member is the subject of this test.
        return null!;
    }

    private static string VaryAsciiCase(string value) =>
        new([.. value.Select(VaryAsciiCase)]);

    private static char VaryAsciiCase(char value) =>
        value switch
        {
            >= 'a' and <= 'z' => (char)(value - 32),
            >= 'A' and <= 'Z' => (char)(value + 32),
            _ => value,
        };

    private static bool ContainsNull(ImmutableHashSet<string> values) =>
        NullCount(values) > 0;

    private static int NullCount(ImmutableHashSet<string> values) =>
        values.Count(IsNull);

    private static bool IsNull(object? value) => value is null;
}
