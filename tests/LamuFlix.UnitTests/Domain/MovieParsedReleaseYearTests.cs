using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieParsedReleaseYearTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithParsedReleaseYear_ExposesItWithoutMetadata()
    {
        // arrange
        var year = new ReleaseYear(2012, Now);

        // act
        var movie = Movie.Create(new MovieId(1), "2 Days in New York", new LibraryPath("C:/library/a"), new MediaFormat("mkv"), year);

        // assert
        movie.ParsedReleaseYear.ShouldBe(year);
        movie.Metadata.ShouldBeNull();
    }

    [Fact]
    public void Create_WithoutParsedReleaseYear_IsNull()
    {
        // act
        var movie = Movie.Create(new MovieId(2), "Blade Runner", new LibraryPath("C:/library/a"), new MediaFormat("mkv"));

        // assert
        movie.ParsedReleaseYear.ShouldBeNull();
    }

    [Fact]
    public void Rehydrate_RestoresParsedReleaseYear()
    {
        // arrange
        var year = new ReleaseYear(2026, Now);

        // act
        var movie = Movie.Rehydrate(
            new MovieId(3),
            "The Invite",
            new LibraryPath("C:/library/a"),
            new MediaFormat("mkv"),
            false,
            null,
            EnrichmentStatus.Pending,
            null,
            0,
            null,
            null,
            year);

        // assert
        movie.ParsedReleaseYear.ShouldBe(year);
    }

    [Fact]
    public void Rehydrate_WithoutParsedReleaseYear_IsNull()
    {
        // act
        var movie = Movie.Rehydrate(
            new MovieId(4),
            "Blade Runner",
            new LibraryPath("C:/library/a"),
            new MediaFormat("mkv"),
            false,
            null,
            EnrichmentStatus.Pending,
            null,
            0,
            null,
            null);

        // assert
        movie.ParsedReleaseYear.ShouldBeNull();
    }
}
