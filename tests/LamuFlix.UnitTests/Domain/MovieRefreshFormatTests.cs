using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieRefreshFormatTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 5, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RefreshFormat_NewFormat_ChangesOnlyFormat()
    {
        // arrange
        var movie = Populated();

        // act
        movie.RefreshFormat(new MediaFormat("mkv"));

        // assert
        movie.Format.ShouldBe(new MediaFormat("mkv"));
        movie.Id.ShouldBe(new MovieId(37));
        movie.Title.ShouldBe("Heat");
        movie.Path.ShouldBe(new LibraryPath("C:/library/Heat (1995)"));
        movie.ParsedReleaseYear.ShouldBe(new ReleaseYear(1995, Now));
        movie.Metadata.ShouldBe(Metadata());
        movie.Status.ShouldBe(EnrichmentStatus.Enriched);
        movie.EnrichedAt.ShouldBe(Now);
        movie.EnrichmentAttempts.ShouldBe(2);
        movie.LastAttemptAt.ShouldBe(Now);
        movie.ManualImdbId.ShouldBe(new ImdbId("tt0113277"));
        movie.IsInWatchlist.ShouldBeTrue();
        movie.IsWatched.ShouldBeTrue();
        movie.IsNotInterested.ShouldBeFalse();
    }

    [Fact]
    public void RefreshFormat_SameFormat_LeavesFormatUnchanged()
    {
        // arrange
        var movie = Populated();
        var original = movie.Format;

        // act
        movie.RefreshFormat(new MediaFormat("avi"));

        // assert
        movie.Format.ShouldBe(original);
        movie.Status.ShouldBe(EnrichmentStatus.Enriched);
        movie.Metadata.ShouldBe(Metadata());
    }

    [Fact]
    public void RefreshFormat_FormatWithLeadingDot_NormalisesTheStoredExtension()
    {
        // arrange
        var movie = Populated();

        // act
        movie.RefreshFormat(new MediaFormat(".MKV"));

        // assert
        movie.Format.Extension.ShouldBe("mkv");
    }

    [Fact]
    public void RefreshFormat_NullFormat_ThrowsArgumentNullException()
    {
        // arrange
        var movie = Populated();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var thrown = Should.Throw<ArgumentNullException>(() => movie.RefreshFormat(null!));

        // assert
        thrown.ParamName.ShouldBe("format");
        movie.Format.ShouldBe(new MediaFormat("avi"));
    }

    private static Movie Populated() =>
        Movie.Rehydrate(
            new MovieId(37),
            "Heat",
            new LibraryPath("C:/library/Heat (1995)"),
            new MediaFormat("avi"),
            isInWatchlist: true,
            Metadata(),
            EnrichmentStatus.Enriched,
            Now,
            enrichmentAttempts: 2,
            lastFailureCategory: null,
            lastAttemptAt: Now,
            parsedReleaseYear: new ReleaseYear(1995, Now),
            manualImdbId: new ImdbId("tt0113277"),
            isWatched: true);

    private static MovieMetadata Metadata() =>
        new(
            "Heat",
            "A crew of thieves plans a bank heist in Los Angeles.",
            new ReleaseYear(1995, Now),
            new Runtime(170),
            new ImdbRating(8.3m),
            new ImdbId("tt0113277"));
}
