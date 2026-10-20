using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class MovieFeaturesMapperTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void From_FullMetadata_MapsAllNineFields()
    {
        // arrange
        var metadata = new MovieMetadata(
            "Metadata title",
            synopsis: "A linguist works with the military.",
            releaseYear: new ReleaseYear(2016, Now),
            runtime: new Runtime(116),
            imdbRating: new ImdbRating(8.0m),
            metaScore: 94,
            directors: new[] { "Director A", "Director B" },
            actors: new[] { "Actor A", "Actor B" },
            genres: new[] { "Drama", "Sci-Fi" },
            writers: new[] { "Writer A" });
        var movie = CreateMovie(metadata);

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.Genres.ShouldBe(["Drama", "Sci-Fi"], ignoreOrder: true);
        features.Directors.ShouldBe(["Director A", "Director B"], ignoreOrder: true);
        features.Writers.ShouldBe(["Writer A"]);
        features.Cast.ShouldBe(["Actor A", "Actor B"], ignoreOrder: true);
        features.ReleaseYear.ShouldBe(new ReleaseYear(2016, Now));
        features.Runtime.ShouldBe(new Runtime(116));
        features.ImdbRating.ShouldBe(new ImdbRating(8.0m));
        features.MetaScore.ShouldBe(94);
        features.FullPlot.ShouldBe("A linguist works with the military.");
    }

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)100)]
    [InlineData(null)]
    public void From_PersistedMetaScore_MapsToNullableInt(short? metaScore)
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata("Title", metaScore: metaScore));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        // ReSharper disable once RedundantCast // the cast widens short? to int? before boxing so ShouldBe compares equal numeric types.
        features.MetaScore.ShouldBe((int?)metaScore);
    }

    [Fact]
    public void From_NullMetadata_YieldsFourEmptySetsAndFiveNullScalars()
    {
        // arrange
        var movie = Movie.Rehydrate(
            new MovieId(1),
            "Persisted title",
            new LibraryPath("C:/library/persisted.mkv"),
            new MediaFormat("mkv"),
            false,
            null,
            EnrichmentStatus.Pending,
            null,
            0,
            null,
            null,
            parsedReleaseYear: new ReleaseYear(2014, Now));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.Genres.ShouldBeEmpty();
        features.Directors.ShouldBeEmpty();
        features.Writers.ShouldBeEmpty();
        features.Cast.ShouldBeEmpty();
        features.Genres.KeyComparer.ShouldBe(StringComparer.OrdinalIgnoreCase);
        features.Directors.KeyComparer.ShouldBe(StringComparer.OrdinalIgnoreCase);
        features.Writers.KeyComparer.ShouldBe(StringComparer.OrdinalIgnoreCase);
        features.Cast.KeyComparer.ShouldBe(StringComparer.OrdinalIgnoreCase);
        features.ReleaseYear.ShouldBeNull();
        features.Runtime.ShouldBeNull();
        features.ImdbRating.ShouldBeNull();
        features.MetaScore.ShouldBeNull();
        features.FullPlot.ShouldBeNull();
    }

    [Fact]
    public void From_NullMovie_ThrowsArgumentNullException()
    {
        // act & assert
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard.
        Should.Throw<ArgumentNullException>(() => MovieFeaturesMapper.From(null!));
    }

    [Fact]
    public void From_OnlyGenres_PreservesGenresAndLeavesOtherFieldsEmpty()
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata("Title", genres: new[] { "Drama" }));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.Genres.ShouldBe(["Drama"]);
        features.Directors.ShouldBeEmpty();
        features.Writers.ShouldBeEmpty();
        features.Cast.ShouldBeEmpty();
        features.ReleaseYear.ShouldBeNull();
        features.Runtime.ShouldBeNull();
        features.ImdbRating.ShouldBeNull();
        features.MetaScore.ShouldBeNull();
        features.FullPlot.ShouldBeNull();
    }

    [Fact]
    public void From_OnlyDirectors_PreservesDirectorsAndLeavesOtherFieldsEmpty()
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata("Title", directors: new[] { "Director A" }));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.Directors.ShouldBe(["Director A"]);
        features.Genres.ShouldBeEmpty();
        features.Writers.ShouldBeEmpty();
        features.Cast.ShouldBeEmpty();
        features.ReleaseYear.ShouldBeNull();
        features.Runtime.ShouldBeNull();
        features.ImdbRating.ShouldBeNull();
        features.MetaScore.ShouldBeNull();
        features.FullPlot.ShouldBeNull();
    }

    [Fact]
    public void From_OnlyWriters_PreservesWritersAndLeavesOtherFieldsEmpty()
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata("Title", writers: new[] { "Writer A" }));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.Writers.ShouldBe(["Writer A"]);
        features.Genres.ShouldBeEmpty();
        features.Directors.ShouldBeEmpty();
        features.Cast.ShouldBeEmpty();
        features.ReleaseYear.ShouldBeNull();
        features.Runtime.ShouldBeNull();
        features.ImdbRating.ShouldBeNull();
        features.MetaScore.ShouldBeNull();
        features.FullPlot.ShouldBeNull();
    }

    [Fact]
    public void From_OnlyActors_PreservesCastAndLeavesOtherFieldsEmpty()
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata("Title", actors: new[] { "Actor A" }));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.Cast.ShouldBe(["Actor A"]);
        features.Genres.ShouldBeEmpty();
        features.Directors.ShouldBeEmpty();
        features.Writers.ShouldBeEmpty();
        features.ReleaseYear.ShouldBeNull();
        features.Runtime.ShouldBeNull();
        features.ImdbRating.ShouldBeNull();
        features.MetaScore.ShouldBeNull();
        features.FullPlot.ShouldBeNull();
    }

    [Fact]
    public void From_OnlyReleaseYear_PreservesReleaseYearAndLeavesOtherFieldsEmpty()
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata("Title", releaseYear: new ReleaseYear(2016, Now)));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.ReleaseYear.ShouldBe(new ReleaseYear(2016, Now));
        features.Genres.ShouldBeEmpty();
        features.Directors.ShouldBeEmpty();
        features.Writers.ShouldBeEmpty();
        features.Cast.ShouldBeEmpty();
        features.Runtime.ShouldBeNull();
        features.ImdbRating.ShouldBeNull();
        features.MetaScore.ShouldBeNull();
        features.FullPlot.ShouldBeNull();
    }

    [Fact]
    public void From_OnlyRuntime_PreservesRuntimeAndLeavesOtherFieldsEmpty()
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata("Title", runtime: new Runtime(116)));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.Runtime.ShouldBe(new Runtime(116));
        features.Genres.ShouldBeEmpty();
        features.Directors.ShouldBeEmpty();
        features.Writers.ShouldBeEmpty();
        features.Cast.ShouldBeEmpty();
        features.ReleaseYear.ShouldBeNull();
        features.ImdbRating.ShouldBeNull();
        features.MetaScore.ShouldBeNull();
        features.FullPlot.ShouldBeNull();
    }

    [Fact]
    public void From_OnlyImdbRating_PreservesImdbRatingAndLeavesOtherFieldsEmpty()
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata("Title", imdbRating: new ImdbRating(8.0m)));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.ImdbRating.ShouldBe(new ImdbRating(8.0m));
        features.Genres.ShouldBeEmpty();
        features.Directors.ShouldBeEmpty();
        features.Writers.ShouldBeEmpty();
        features.Cast.ShouldBeEmpty();
        features.ReleaseYear.ShouldBeNull();
        features.Runtime.ShouldBeNull();
        features.MetaScore.ShouldBeNull();
        features.FullPlot.ShouldBeNull();
    }

    [Fact]
    public void From_OnlySynopsis_PreservesFullPlotAndLeavesOtherFieldsEmpty()
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata("Title", synopsis: "A plot."));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.FullPlot.ShouldBe("A plot.");
        features.Genres.ShouldBeEmpty();
        features.Directors.ShouldBeEmpty();
        features.Writers.ShouldBeEmpty();
        features.Cast.ShouldBeEmpty();
        features.ReleaseYear.ShouldBeNull();
        features.Runtime.ShouldBeNull();
        features.ImdbRating.ShouldBeNull();
        features.MetaScore.ShouldBeNull();
    }

    [Fact]
    public void From_EmptyCollections_YieldsFourEmptySetsAndNullScalars()
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata(
            "Title",
            genres: [],
            directors: [],
            writers: [],
            actors: []));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.Genres.ShouldBeEmpty();
        features.Directors.ShouldBeEmpty();
        features.Writers.ShouldBeEmpty();
        features.Cast.ShouldBeEmpty();
        features.ReleaseYear.ShouldBeNull();
        features.Runtime.ShouldBeNull();
        features.ImdbRating.ShouldBeNull();
        features.MetaScore.ShouldBeNull();
        features.FullPlot.ShouldBeNull();
    }

    [Fact]
    public void From_GenresWithNullElement_KeepsNullMember()
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata("Title", genres: new[] { "Drama", NullElement() }));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.Genres.Count.ShouldBe(2);
        features.Genres.Any(IsNull).ShouldBeTrue();
    }

    [Fact]
    public void From_DirectorsWithNullElement_KeepsNullMember()
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata("Title", directors: new[] { "Director A", NullElement() }));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.Directors.Count.ShouldBe(2);
        features.Directors.Any(IsNull).ShouldBeTrue();
    }

    [Fact]
    public void From_WritersWithNullElement_KeepsNullMember()
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata("Title", writers: new[] { "Writer A", NullElement() }));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.Writers.Count.ShouldBe(2);
        features.Writers.Any(IsNull).ShouldBeTrue();
    }

    [Fact]
    public void From_ActorsWithNullElement_KeepsNullMemberInCast()
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata("Title", actors: new[] { "Actor A", NullElement() }));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.Cast.Count.ShouldBe(2);
        features.Cast.Any(IsNull).ShouldBeTrue();
    }

    [Fact]
    public void From_GenresWithRepeatedNullElements_CollapsesToSingleNullMember()
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata("Title", genres: new[] { NullElement(), NullElement(), "Drama" }));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.Genres.Count(IsNull).ShouldBe(1);
        features.Genres.Count.ShouldBe(2);
    }

    [Fact]
    public void From_DirectorsWithRepeatedNullElements_CollapsesToSingleNullMember()
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata("Title", directors: new[] { NullElement(), NullElement(), "Director A" }));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.Directors.Count(IsNull).ShouldBe(1);
        features.Directors.Count.ShouldBe(2);
    }

    [Fact]
    public void From_WritersWithRepeatedNullElements_CollapsesToSingleNullMember()
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata("Title", writers: new[] { NullElement(), NullElement(), "Writer A" }));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.Writers.Count(IsNull).ShouldBe(1);
        features.Writers.Count.ShouldBe(2);
    }

    [Fact]
    public void From_ActorsWithRepeatedNullElements_CollapsesToSingleNullMemberInCast()
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata("Title", actors: new[] { NullElement(), NullElement(), "Actor A" }));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.Cast.Count(IsNull).ShouldBe(1);
        features.Cast.Count.ShouldBe(2);
    }

    [Fact]
    public void From_CaseEquivalentDirectorDuplicates_CollapseRegardlessOfOrder()
    {
        // arrange
        var first = CreateMovie(new MovieMetadata("Title", directors: new[] { "Director A", "director a", "DIRECTOR A" }));
        var second = CreateMovie(new MovieMetadata("Title", directors: new[] { "DIRECTOR A", "Director A", "director a" }));

        // act
        var firstFeatures = MovieFeaturesMapper.From(first);
        var secondFeatures = MovieFeaturesMapper.From(second);

        // assert
        firstFeatures.Directors.Count.ShouldBe(1);
        secondFeatures.Directors.Count.ShouldBe(1);
        firstFeatures.ShouldBe(secondFeatures);
    }

    [Fact]
    public void From_CaseEquivalentActorDuplicates_CollapseRegardlessOfOrder()
    {
        // arrange
        var first = CreateMovie(new MovieMetadata("Title", actors: new[] { "Actor A", "actor a", "ACTOR A" }));
        var second = CreateMovie(new MovieMetadata("Title", actors: new[] { "ACTOR A", "Actor A", "actor a" }));

        // act
        var firstFeatures = MovieFeaturesMapper.From(first);
        var secondFeatures = MovieFeaturesMapper.From(second);

        // assert
        firstFeatures.Cast.Count.ShouldBe(1);
        secondFeatures.Cast.Count.ShouldBe(1);
        firstFeatures.ShouldBe(secondFeatures);
    }

    [Fact]
    public void From_DirectorsWithBlankAndSpacedText_KeepsTextWithoutTrimmingOrCaseRewrite()
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata("Title", directors: new[] { "", "  Director A  " }));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.Directors.Count.ShouldBe(2);
        features.Directors.ShouldContain("");
        features.Directors.ShouldContain("  Director A  ");
        features.Directors.ShouldNotContain("Director A");
    }

    [Fact]
    public void From_ActorsWithBlankAndSpacedText_KeepsTextWithoutTrimmingOrCaseRewrite()
    {
        // arrange
        var movie = CreateMovie(new MovieMetadata("Title", actors: new[] { "", "  Actor A  " }));

        // act
        var features = MovieFeaturesMapper.From(movie);

        // assert
        features.Cast.Count.ShouldBe(2);
        features.Cast.ShouldContain("");
        features.Cast.ShouldContain("  Actor A  ");
        features.Cast.ShouldNotContain("Actor A");
    }

    [Fact]
    public void From_EquivalentMappedValuesWithNullMembers_AreEqualWithEqualHash()
    {
        // arrange
        var first = CreateMovie(new MovieMetadata(
            "Title",
            genres: new[] { "Drama", NullElement() },
            directors: new[] { "Director A", NullElement() },
            writers: new[] { "Writer A", NullElement() },
            actors: new[] { "Actor A", NullElement() }));
        var second = CreateMovie(new MovieMetadata(
            "Title",
            genres: new[] { "drama", NullElement() },
            directors: new[] { "DIRECTOR A", NullElement() },
            writers: new[] { "writer a", NullElement() },
            actors: new[] { "ACTOR A", NullElement() }));

        // act
        var firstFeatures = MovieFeaturesMapper.From(first);
        var secondFeatures = MovieFeaturesMapper.From(second);

        // assert
        firstFeatures.ShouldBe(secondFeatures);
        secondFeatures.ShouldBe(firstFeatures);
        firstFeatures.GetHashCode().ShouldBe(secondFeatures.GetHashCode());
    }

    [Fact]
    public void Hash_GenresNullMemberAddedOrRemoved_LeavesWholeValueHashUnchangedAndEqualityDistinguishes()
    {
        // arrange
        var withoutNull = MovieFeaturesMapper.From(CreateMovie(new MovieMetadata("Title", genres: ["Drama"], synopsis: "Plot")));
        var withNull = MovieFeaturesMapper.From(CreateMovie(new MovieMetadata("Title", genres: ["Drama", NullElement()], synopsis: "Plot")));

        // act & assert
        withNull.GetHashCode().ShouldBe(withoutNull.GetHashCode());
        withNull.ShouldNotBe(withoutNull);
        withoutNull.ShouldNotBe(withNull);
    }

    [Fact]
    public void Hash_DirectorsNullMemberAddedOrRemoved_LeavesWholeValueHashUnchangedAndEqualityDistinguishes()
    {
        // arrange
        var withoutNull = MovieFeaturesMapper.From(CreateMovie(new MovieMetadata("Title", directors: ["Director A"], synopsis: "Plot")));
        var withNull = MovieFeaturesMapper.From(CreateMovie(new MovieMetadata("Title", directors: ["Director A", NullElement()], synopsis: "Plot")));

        // act & assert
        withNull.GetHashCode().ShouldBe(withoutNull.GetHashCode());
        withNull.ShouldNotBe(withoutNull);
        withoutNull.ShouldNotBe(withNull);
    }

    [Fact]
    public void Hash_WritersNullMemberAddedOrRemoved_LeavesWholeValueHashUnchangedAndEqualityDistinguishes()
    {
        // arrange
        var withoutNull = MovieFeaturesMapper.From(CreateMovie(new MovieMetadata("Title", writers: ["Writer A"], synopsis: "Plot")));
        var withNull = MovieFeaturesMapper.From(CreateMovie(new MovieMetadata("Title", writers: ["Writer A", NullElement()], synopsis: "Plot")));

        // act & assert
        withNull.GetHashCode().ShouldBe(withoutNull.GetHashCode());
        withNull.ShouldNotBe(withoutNull);
        withoutNull.ShouldNotBe(withNull);
    }

    [Fact]
    public void Hash_CastNullMemberAddedOrRemoved_LeavesWholeValueHashUnchangedAndEqualityDistinguishes()
    {
        // arrange
        var withoutNull = MovieFeaturesMapper.From(CreateMovie(new MovieMetadata("Title", actors: ["Actor A"], synopsis: "Plot")));
        var withNull = MovieFeaturesMapper.From(CreateMovie(new MovieMetadata("Title", actors: ["Actor A", NullElement()], synopsis: "Plot")));

        // act & assert
        withNull.GetHashCode().ShouldBe(withoutNull.GetHashCode());
        withNull.ShouldNotBe(withoutNull);
        withoutNull.ShouldNotBe(withNull);
    }

    [Fact]
    public void From_DirectorsSourceMutatedAfterMapping_DoesNotChangeSet()
    {
        // arrange
        var directors = new List<string> { "Director A" };
        var movie = CreateMovie(new MovieMetadata("Title", directors: directors));

        // act
        var features = MovieFeaturesMapper.From(movie);
        directors.Add("Director B");

        // assert
        features.Directors.Count.ShouldBe(1);
        features.Directors.ShouldContain("Director A");
        features.Directors.ShouldNotContain("Director B");
    }

    [Fact]
    public void From_ActorsSourceMutatedAfterMapping_DoesNotChangeCast()
    {
        // arrange
        var actors = new List<string> { "Actor A" };
        var movie = CreateMovie(new MovieMetadata("Title", actors: actors));

        // act
        var features = MovieFeaturesMapper.From(movie);
        actors.Add("Actor B");

        // assert
        features.Cast.Count.ShouldBe(1);
        features.Cast.ShouldContain("Actor A");
        features.Cast.ShouldNotContain("Actor B");
    }

    [Fact]
    public void From_FullPlotNullEmptyWhitespaceAndMixedCase_IsPreserved()
    {
        // arrange
        var nullPlot = CreateMovie(new MovieMetadata("Title", synopsis: null));
        var emptyPlot = CreateMovie(new MovieMetadata("Title", synopsis: ""));
        var whitespacePlot = CreateMovie(new MovieMetadata("Title", synopsis: "   "));
        var mixedCasePlot = CreateMovie(new MovieMetadata("Title", synopsis: "MiXeD Plot"));

        // act
        var nullFeatures = MovieFeaturesMapper.From(nullPlot);
        var emptyFeatures = MovieFeaturesMapper.From(emptyPlot);
        var whitespaceFeatures = MovieFeaturesMapper.From(whitespacePlot);
        var mixedCaseFeatures = MovieFeaturesMapper.From(mixedCasePlot);

        // assert
        nullFeatures.FullPlot.ShouldBeNull();
        emptyFeatures.FullPlot.ShouldBe("");
        whitespaceFeatures.FullPlot.ShouldBe("   ");
        mixedCaseFeatures.FullPlot.ShouldBe("MiXeD Plot");
    }

    [Fact]
    public void From_FullPlotCaseDifference_IsUnequal()
    {
        // arrange
        var lower = MovieFeaturesMapper.From(CreateMovie(new MovieMetadata("Title", synopsis: "plot")));
        var upper = MovieFeaturesMapper.From(CreateMovie(new MovieMetadata("Title", synopsis: "Plot")));

        // act & assert
        lower.ShouldNotBe(upper);
        upper.ShouldNotBe(lower);
    }

    private static string NullElement()
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed // the null member is the subject of this test.
        return null!;
    }

    private static bool IsNull(object? value) => value is null;

    private static Movie CreateMovie(MovieMetadata? metadata) =>
        Movie.Rehydrate(
            new MovieId(1),
            "Title",
            new LibraryPath("C:/library/title.mkv"),
            new MediaFormat("mkv"),
            false,
            metadata,
            EnrichmentStatus.Enriched,
            Now,
            1,
            null,
            Now);
}
