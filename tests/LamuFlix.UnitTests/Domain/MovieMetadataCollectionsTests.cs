using System;
using System.Collections.Generic;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieMetadataCollectionsTests
{
    [Fact]
    public void Constructor_GenresAndWritersOmitted_LeavesExistingCallersValidAndEmpty()
    {
        // act
        var metadata = new MovieMetadata("Arrival");

        // assert
        metadata.Genres.ShouldBeEmpty();
        metadata.Writers.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_WithGenresAndWriters_KeepsValuesInSuppliedOrder()
    {
        // act
        var metadata = new MovieMetadata(
            "Arrival",
            genres: new[] { "Drama", "Sci-Fi" },
            writers: new[] { "Writer A", "Writer B" });

        // assert
        metadata.Genres.ShouldBe(new[] { "Drama", "Sci-Fi" });
        metadata.Writers.ShouldBe(new[] { "Writer A", "Writer B" });
    }

    [Fact]
    public void Constructor_WithEmptyGenresAndWriters_YieldsEmpty()
    {
        // act
        var metadata = new MovieMetadata("Arrival", genres: [], writers: []);

        // assert
        metadata.Genres.ShouldBeEmpty();
        metadata.Writers.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_Genres_CopiesInputSoLaterSourceMutationDoesNotLeak()
    {
        // arrange
        var genres = new List<string> { "Drama" };

        // act
        var metadata = new MovieMetadata("Arrival", genres: genres);
        genres.Add("Comedy");

        // assert
        metadata.Genres.ShouldBe(new[] { "Drama" });
    }

    [Fact]
    public void Constructor_Writers_CopiesInputSoLaterSourceMutationDoesNotLeak()
    {
        // arrange
        var writers = new List<string> { "Writer A" };

        // act
        var metadata = new MovieMetadata("Arrival", writers: writers);
        writers.Add("Writer B");

        // assert
        metadata.Writers.ShouldBe(new[] { "Writer A" });
    }

    [Fact]
    public void Genres_CannotBeMutatedThroughIListCast()
    {
        // arrange
        var metadata = new MovieMetadata("Arrival", genres: new[] { "Drama" });

        // act
        var exposed = (IList<string>)metadata.Genres;

        // assert
        Should.Throw<NotSupportedException>(() => exposed.Add("Comedy"));
        metadata.Genres.ShouldBe(new[] { "Drama" });
    }

    [Fact]
    public void Writers_CannotBeMutatedThroughIListCast()
    {
        // arrange
        var metadata = new MovieMetadata("Arrival", writers: new[] { "Writer A" });

        // act
        var exposed = (IList<string>)metadata.Writers;

        // assert
        Should.Throw<NotSupportedException>(() => exposed.Add("Writer B"));
        metadata.Writers.ShouldBe(new[] { "Writer A" });
    }

    [Fact]
    public void Genres_AreNotExposedAsMutableArray()
    {
        // arrange
        var metadata = new MovieMetadata("Arrival", genres: new[] { "Drama" });

        // act & assert
        ((object)metadata.Genres).ShouldNotBeOfType<string[]>();
        ((object)metadata.Writers).ShouldNotBeOfType<string[]>();
    }

    [Fact]
    public void EqualsAndHash_SameGenresAndWriters_AreEqualWithEqualHash()
    {
        // arrange
        var left = new MovieMetadata("Arrival", genres: new[] { "Drama", "Sci-Fi" }, writers: new[] { "Writer A" });
        var right = new MovieMetadata("Arrival", genres: new[] { "Drama", "Sci-Fi" }, writers: new[] { "Writer A" });

        // act & assert
        left.ShouldBe(right);
        right.ShouldBe(left);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void EqualsAndHash_DifferingGenres_AreNotEqualAndHashDiffers()
    {
        // arrange
        var left = new MovieMetadata("Arrival", genres: new[] { "Drama" }, writers: new[] { "Writer A" });
        var right = new MovieMetadata("Arrival", genres: new[] { "Comedy" }, writers: new[] { "Writer A" });

        // act & assert
        left.ShouldNotBe(right);
        right.ShouldNotBe(left);
        left.GetHashCode().ShouldNotBe(right.GetHashCode());
    }

    [Fact]
    public void EqualsAndHash_DifferingWriters_AreNotEqualAndHashDiffers()
    {
        // arrange
        var left = new MovieMetadata("Arrival", genres: new[] { "Drama" }, writers: new[] { "Writer A" });
        var right = new MovieMetadata("Arrival", genres: new[] { "Drama" }, writers: new[] { "Writer B" });

        // act & assert
        left.ShouldNotBe(right);
        right.ShouldNotBe(left);
        left.GetHashCode().ShouldNotBe(right.GetHashCode());
    }

    [Fact]
    public void EqualsAndHash_SameGenresInDifferentOrder_AreNotEqual()
    {
        // arrange
        var left = new MovieMetadata("Arrival", genres: new[] { "Drama", "Sci-Fi" }, writers: new[] { "Writer A" });
        var right = new MovieMetadata("Arrival", genres: new[] { "Sci-Fi", "Drama" }, writers: new[] { "Writer A" });

        // act & assert
        left.ShouldNotBe(right);
    }

    [Fact]
    public void EqualsAndHash_SameWritersInDifferentOrder_AreNotEqual()
    {
        // arrange
        var left = new MovieMetadata("Arrival", genres: new[] { "Drama" }, writers: new[] { "Writer A", "Writer B" });
        var right = new MovieMetadata("Arrival", genres: new[] { "Drama" }, writers: new[] { "Writer B", "Writer A" });

        // act & assert
        left.ShouldNotBe(right);
    }

    [Fact]
    public void EqualsAndHash_EmptyVersusNonEmptyCollections_AreNotEqual()
    {
        // arrange
        var empty = new MovieMetadata("Arrival");
        var filled = new MovieMetadata("Arrival", genres: new[] { "Drama" }, writers: new[] { "Writer A" });

        // act & assert
        empty.ShouldNotBe(filled);
        filled.ShouldNotBe(empty);
    }
}
