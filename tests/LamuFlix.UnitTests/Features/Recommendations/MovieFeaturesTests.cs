using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class MovieFeaturesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_NoArguments_YieldsFourEmptySetsAndFiveNullScalars()
    {
        // act
        var features = new MovieFeatures();

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
    public void Constructor_ExplicitNullArguments_YieldsFourEmptySetsAndFiveNullScalars()
    {
        // act
        var features = new MovieFeatures(null, null, null, null, null, null, null, null, null);

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
    public void Constructor_DefaultSets_UseOrdinalIgnoreCaseMembership()
    {
        // act
        var features = new MovieFeatures();

        // assert
        features.Genres.KeyComparer.ShouldBe(StringComparer.OrdinalIgnoreCase);
        features.Directors.KeyComparer.ShouldBe(StringComparer.OrdinalIgnoreCase);
        features.Writers.KeyComparer.ShouldBe(StringComparer.OrdinalIgnoreCase);
        features.Cast.KeyComparer.ShouldBe(StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_CaseVariedMembers_UseOrdinalIgnoreCaseMembership()
    {
        // act
        var features = new MovieFeatures(genres: new[] { "Drama" });

        // assert
        features.Genres.Contains("DRAMA").ShouldBeTrue();
        features.Genres.Contains("drama").ShouldBeTrue();
    }

    [Fact]
    public void Constructor_CaseEquivalentDuplicates_CollapseToSingleMemberRegardlessOfOrder()
    {
        // act
        var features = new MovieFeatures(genres: new[] { "Drama", "drama", "DRAMA" });

        // assert
        features.Genres.Count.ShouldBe(1);
    }

    [Fact]
    public void Constructor_BlankAndSpacedText_IsKeptWithoutTrimmingOrCaseRewrite()
    {
        // act
        var features = new MovieFeatures(genres: new[] { "", "  Drama  " });

        // assert
        features.Genres.Count.ShouldBe(2);
        features.Genres.ShouldContain("");
        features.Genres.ShouldContain("  Drama  ");
        features.Genres.ShouldNotContain("Drama");
    }

    [Fact]
    public void Constructor_GenresWithNullElement_KeepsNullMember()
    {
        // act
        var features = new MovieFeatures(genres: new[] { "Drama", null! });

        // assert
        features.Genres.Count.ShouldBe(2);
        features.Genres.Any(value => value is null).ShouldBeTrue();
    }

    [Fact]
    public void Constructor_DirectorsWithNullElement_KeepsNullMember()
    {
        // act
        var features = new MovieFeatures(directors: new[] { "Director A", null! });

        // assert
        features.Directors.Count.ShouldBe(2);
        features.Directors.Any(value => value is null).ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WritersWithNullElement_KeepsNullMember()
    {
        // act
        var features = new MovieFeatures(writers: new[] { "Writer A", null! });

        // assert
        features.Writers.Count.ShouldBe(2);
        features.Writers.Any(value => value is null).ShouldBeTrue();
    }

    [Fact]
    public void Constructor_CastWithNullElement_KeepsNullMember()
    {
        // act
        var features = new MovieFeatures(cast: new[] { "Actor A", null! });

        // assert
        features.Cast.Count.ShouldBe(2);
        features.Cast.Any(value => value is null).ShouldBeTrue();
    }

    [Fact]
    public void Constructor_GenresWithRepeatedNullElements_CollapsesToSingleNullMember()
    {
        // act
        var features = new MovieFeatures(genres: new[] { null!, null!, "Drama" });

        // assert
        features.Genres.Count(value => value is null).ShouldBe(1);
        features.Genres.Count.ShouldBe(2);
    }

    [Fact]
    public void Constructor_DirectorsWithRepeatedNullElements_CollapsesToSingleNullMember()
    {
        // act
        var features = new MovieFeatures(directors: new[] { null!, null!, "Director A" });

        // assert
        features.Directors.Count(value => value is null).ShouldBe(1);
        features.Directors.Count.ShouldBe(2);
    }

    [Fact]
    public void Constructor_WritersWithRepeatedNullElements_CollapsesToSingleNullMember()
    {
        // act
        var features = new MovieFeatures(writers: new[] { null!, null!, "Writer A" });

        // assert
        features.Writers.Count(value => value is null).ShouldBe(1);
        features.Writers.Count.ShouldBe(2);
    }

    [Fact]
    public void Constructor_CastWithRepeatedNullElements_CollapsesToSingleNullMember()
    {
        // act
        var features = new MovieFeatures(cast: new[] { null!, null!, "Actor A" });

        // assert
        features.Cast.Count(value => value is null).ShouldBe(1);
        features.Cast.Count.ShouldBe(2);
    }

    [Fact]
    public void Constructor_ParametersAndProperties_KeepDeclaredPublicTypes()
    {
        // act
        var constructors = typeof(MovieFeatures).GetConstructors();
        var constructor = constructors.ShouldHaveSingleItem();

        // assert
        constructor.GetParameters().Select(parameter => parameter.ParameterType).ShouldBe(
        [
            typeof(IEnumerable<string>),
            typeof(IEnumerable<string>),
            typeof(IEnumerable<string>),
            typeof(IEnumerable<string>),
            typeof(ReleaseYear),
            typeof(Runtime),
            typeof(ImdbRating),
            typeof(int?),
            typeof(string),
        ]);
        typeof(MovieFeatures).GetProperty(nameof(MovieFeatures.Genres))!.PropertyType.ShouldBe(typeof(ImmutableHashSet<string>));
        typeof(MovieFeatures).GetProperty(nameof(MovieFeatures.Directors))!.PropertyType.ShouldBe(typeof(ImmutableHashSet<string>));
        typeof(MovieFeatures).GetProperty(nameof(MovieFeatures.Writers))!.PropertyType.ShouldBe(typeof(ImmutableHashSet<string>));
        typeof(MovieFeatures).GetProperty(nameof(MovieFeatures.Cast))!.PropertyType.ShouldBe(typeof(ImmutableHashSet<string>));
    }

    [Fact]
    public void Constructor_GenresSourceMutatedAfterConstruction_DoesNotChangeSet()
    {
        // arrange
        var genres = new List<string> { "Drama" };

        // act
        var features = new MovieFeatures(genres: genres);
        genres.Add("Comedy");

        // assert
        features.Genres.Count.ShouldBe(1);
        features.Genres.ShouldContain("Drama");
        features.Genres.ShouldNotContain("Comedy");
    }

    [Fact]
    public void Constructor_DirectorsSourceMutatedAfterConstruction_DoesNotChangeSet()
    {
        // arrange
        var directors = new List<string> { "Director A" };

        // act
        var features = new MovieFeatures(directors: directors);
        directors.Add("Director B");

        // assert
        features.Directors.Count.ShouldBe(1);
        features.Directors.ShouldContain("Director A");
        features.Directors.ShouldNotContain("Director B");
    }

    [Fact]
    public void Constructor_WritersSourceMutatedAfterConstruction_DoesNotChangeSet()
    {
        // arrange
        var writers = new List<string> { "Writer A" };

        // act
        var features = new MovieFeatures(writers: writers);
        writers.Add("Writer B");

        // assert
        features.Writers.Count.ShouldBe(1);
        features.Writers.ShouldContain("Writer A");
        features.Writers.ShouldNotContain("Writer B");
    }

    [Fact]
    public void Constructor_CastSourceMutatedAfterConstruction_DoesNotChangeSet()
    {
        // arrange
        var cast = new List<string> { "Actor A" };

        // act
        var features = new MovieFeatures(cast: cast);
        cast.Add("Actor B");

        // assert
        features.Cast.Count.ShouldBe(1);
        features.Cast.ShouldContain("Actor A");
        features.Cast.ShouldNotContain("Actor B");
    }

    [Fact]
    public void Equality_IndependentlyConstructedEquivalentValues_AreEqualWithEqualHash()
    {
        // arrange
        var left = CreateBaseline();
        var right = new MovieFeatures(
            genres: new[] { "sci-fi", "DRAMA" },
            directors: new[] { "DIRECTOR A" },
            writers: new[] { "writer a" },
            cast: new[] { "actor b", "ACTOR A" },
            releaseYear: new ReleaseYear(2016, Now),
            runtime: new Runtime(116),
            imdbRating: new ImdbRating(8.0m),
            metaScore: 94,
            fullPlot: "A linguist works with the military.");

        // act & assert
        left.ShouldBe(right);
        right.ShouldBe(left);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void Equality_ValuesWithMatchingNullMembers_AreEqualWithEqualHash()
    {
        // arrange
        var left = new MovieFeatures(
            genres: new[] { "Drama", null! },
            directors: new[] { "Director A", null! },
            writers: new[] { "Writer A", null! },
            cast: new[] { "Actor A", null! });
        var right = new MovieFeatures(
            genres: new[] { "drama", null! },
            directors: new[] { "DIRECTOR A", null! },
            writers: new[] { "writer a", null! },
            cast: new[] { "ACTOR A", null! });

        // act & assert
        left.ShouldBe(right);
        right.ShouldBe(left);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void Hash_GenresNullMemberAddedOrRemoved_LeavesWholeValueHashUnchangedAndEqualityDistinguishes()
    {
        // arrange
        var withoutNull = new MovieFeatures(genres: new[] { "Drama" }, fullPlot: "Plot");
        var withNull = new MovieFeatures(genres: new[] { "Drama", null! }, fullPlot: "Plot");

        // act & assert
        withNull.GetHashCode().ShouldBe(withoutNull.GetHashCode());
        withNull.ShouldNotBe(withoutNull);
        withoutNull.ShouldNotBe(withNull);
    }

    [Fact]
    public void Hash_DirectorsNullMemberAddedOrRemoved_LeavesWholeValueHashUnchangedAndEqualityDistinguishes()
    {
        // arrange
        var withoutNull = new MovieFeatures(directors: new[] { "Director A" }, fullPlot: "Plot");
        var withNull = new MovieFeatures(directors: new[] { "Director A", null! }, fullPlot: "Plot");

        // act & assert
        withNull.GetHashCode().ShouldBe(withoutNull.GetHashCode());
        withNull.ShouldNotBe(withoutNull);
        withoutNull.ShouldNotBe(withNull);
    }

    [Fact]
    public void Hash_WritersNullMemberAddedOrRemoved_LeavesWholeValueHashUnchangedAndEqualityDistinguishes()
    {
        // arrange
        var withoutNull = new MovieFeatures(writers: new[] { "Writer A" }, fullPlot: "Plot");
        var withNull = new MovieFeatures(writers: new[] { "Writer A", null! }, fullPlot: "Plot");

        // act & assert
        withNull.GetHashCode().ShouldBe(withoutNull.GetHashCode());
        withNull.ShouldNotBe(withoutNull);
        withoutNull.ShouldNotBe(withNull);
    }

    [Fact]
    public void Hash_CastNullMemberAddedOrRemoved_LeavesWholeValueHashUnchangedAndEqualityDistinguishes()
    {
        // arrange
        var withoutNull = new MovieFeatures(cast: new[] { "Actor A" }, fullPlot: "Plot");
        var withNull = new MovieFeatures(cast: new[] { "Actor A", null! }, fullPlot: "Plot");

        // act & assert
        withNull.GetHashCode().ShouldBe(withoutNull.GetHashCode());
        withNull.ShouldNotBe(withoutNull);
        withoutNull.ShouldNotBe(withNull);
    }

    [Theory]
    [InlineData(nameof(MovieFeatures.Genres))]
    [InlineData(nameof(MovieFeatures.Directors))]
    [InlineData(nameof(MovieFeatures.Writers))]
    [InlineData(nameof(MovieFeatures.Cast))]
    [InlineData(nameof(MovieFeatures.ReleaseYear))]
    [InlineData(nameof(MovieFeatures.Runtime))]
    [InlineData(nameof(MovieFeatures.ImdbRating))]
    [InlineData(nameof(MovieFeatures.MetaScore))]
    [InlineData(nameof(MovieFeatures.FullPlot))]
    public void Equality_EachOfNineFieldsChanged_AreNotEqual(string changedField)
    {
        // arrange
        var baseline = CreateBaseline();
        var changed = ChangeField(baseline, changedField);

        // act & assert
        changed.ShouldNotBe(baseline);
        baseline.ShouldNotBe(changed);
    }

    [Fact]
    public void Equals_NullOrDifferentType_IsFalse()
    {
        // arrange
        var features = new MovieFeatures();

        // act & assert
        features.Equals((MovieFeatures?)null).ShouldBeFalse();
        features.Equals("not movie features").ShouldBeFalse();
    }

    [Fact]
    public void Constructor_FullPlotNullEmptyWhitespaceAndMixedCase_IsPreserved()
    {
        // act
        var nullPlot = new MovieFeatures(fullPlot: null);
        var emptyPlot = new MovieFeatures(fullPlot: "");
        var whitespacePlot = new MovieFeatures(fullPlot: "   ");
        var mixedCasePlot = new MovieFeatures(fullPlot: "MiXeD Plot");

        // assert
        nullPlot.FullPlot.ShouldBeNull();
        emptyPlot.FullPlot.ShouldBe("");
        whitespacePlot.FullPlot.ShouldBe("   ");
        mixedCasePlot.FullPlot.ShouldBe("MiXeD Plot");
    }

    [Fact]
    public void Equality_FullPlotCaseDifference_IsUnequal()
    {
        // arrange
        var lower = new MovieFeatures(fullPlot: "plot");
        var upper = new MovieFeatures(fullPlot: "Plot");

        // act & assert
        lower.ShouldNotBe(upper);
        upper.ShouldNotBe(lower);
    }

    private static MovieFeatures CreateBaseline() =>
        new(
            genres: new[] { "Drama", "Sci-Fi" },
            directors: new[] { "Director A" },
            writers: new[] { "Writer A" },
            cast: new[] { "Actor A", "Actor B" },
            releaseYear: new ReleaseYear(2016, Now),
            runtime: new Runtime(116),
            imdbRating: new ImdbRating(8.0m),
            metaScore: 94,
            fullPlot: "A linguist works with the military.");

    private static MovieFeatures ChangeField(MovieFeatures baseline, string field) =>
        field switch
        {
            nameof(MovieFeatures.Genres) => Copy(baseline, genres: new[] { "Comedy" }),
            nameof(MovieFeatures.Directors) => Copy(baseline, directors: new[] { "Director B" }),
            nameof(MovieFeatures.Writers) => Copy(baseline, writers: new[] { "Writer B" }),
            nameof(MovieFeatures.Cast) => Copy(baseline, cast: new[] { "Actor B" }),
            nameof(MovieFeatures.ReleaseYear) => Copy(baseline, releaseYear: new ReleaseYear(2017, Now)),
            nameof(MovieFeatures.Runtime) => Copy(baseline, runtime: new Runtime(117)),
            nameof(MovieFeatures.ImdbRating) => Copy(baseline, imdbRating: new ImdbRating(8.1m)),
            nameof(MovieFeatures.MetaScore) => Copy(baseline, metaScore: 95),
            nameof(MovieFeatures.FullPlot) => Copy(baseline, fullPlot: "Another plot."),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown field."),
        };

    private static MovieFeatures Copy(
        MovieFeatures baseline,
        IEnumerable<string>? genres = null,
        IEnumerable<string>? directors = null,
        IEnumerable<string>? writers = null,
        IEnumerable<string>? cast = null,
        ReleaseYear? releaseYear = null,
        Runtime? runtime = null,
        ImdbRating? imdbRating = null,
        int? metaScore = null,
        string? fullPlot = null) =>
        new(
            genres ?? baseline.Genres,
            directors ?? baseline.Directors,
            writers ?? baseline.Writers,
            cast ?? baseline.Cast,
            releaseYear ?? baseline.ReleaseYear,
            runtime ?? baseline.Runtime,
            imdbRating ?? baseline.ImdbRating,
            metaScore ?? baseline.MetaScore,
            fullPlot ?? baseline.FullPlot);
}
