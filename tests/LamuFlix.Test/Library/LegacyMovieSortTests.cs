using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Data.Models;
using LamuFlix.Web.Extensions;
using LamuFlix.Web.Models.Movies;
using Shouldly;
using Xunit;

namespace LamuFlix.Test.Library;

public sealed class LegacyMovieSortTests
{
    [Fact]
    public void EmptySort_OrdersByIdDescending()
    {
        Ids(Sort(Catalog(), string.Empty, "asc")).ShouldBe([4, 3, 2, 1]);
    }

    [Theory]
    [InlineData("Title", "asc", new[] { 4, 2, 1, 3 })]
    [InlineData("Title", "desc", new[] { 3, 1, 2, 4 })]
    [InlineData("Year", "asc", new[] { 4, 2, 1, 3 })]
    [InlineData("Year", "desc", new[] { 3, 1, 2, 4 })]
    [InlineData("Duration", "asc", new[] { 4, 1, 3, 2 })]
    [InlineData("Duration", "desc", new[] { 2, 3, 1, 4 })]
    [InlineData("ImdbRating", "asc", new[] { 3, 1, 2, 4 })]
    [InlineData("ImdbRating", "desc", new[] { 4, 2, 1, 3 })]
    [InlineData("MetaScore", "asc", new[] { 4, 1, 3, 2 })]
    [InlineData("MetaScore", "desc", new[] { 2, 3, 1, 4 })]
    [InlineData("RottenTomatoes", "asc", new[] { 4, 1, 3, 2 })]
    [InlineData("RottenTomatoes", "desc", new[] { 2, 3, 1, 4 })]
    [InlineData("Id", "asc", new[] { 1, 2, 3, 4 })]
    [InlineData("Id", "desc", new[] { 4, 3, 2, 1 })]
    public void SortKey_CurrentOrder(string sortBy, string sortOrder, int[] expected)
    {
        Ids(Sort(Catalog(), sortBy, sortOrder)).ShouldBe(expected);
    }

    [Fact]
    public void SortOrder_OnlyExactDesc_SortsDescending()
    {
        Ids(Sort(Catalog(), "Year", "DESC")).ShouldBe([4, 2, 1, 3]);
    }

    [Fact]
    public void UnknownSort_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => Sort(Catalog(), "XYZ", "desc").ToList());
    }

    [Fact]
    public void Title_Nulls_CurrentPlacement()
    {
        Ids(Sort(Catalog(), "Title", "asc")).ShouldBe([4, 2, 1, 3]);
        Ids(Sort(Catalog(), "Title", "desc")).ShouldBe([3, 1, 2, 4]);
    }

    [Fact]
    public void SearchField_MatchesTitleSubstringIgnoringCase()
    {
        var filter = new MoviesFilterViewModel { SearchField = "MaT" };
        Ids(FilterCatalog().AsQueryable().DynamicQuery(filter)).ShouldBe([1]);
    }

    [Fact]
    public void Year_MatchesEquality()
    {
        var filter = new MoviesFilterViewModel { Year = 1999 };
        Ids(FilterCatalog().AsQueryable().DynamicQuery(filter)).ShouldBe([2]);
    }

    [Fact]
    public void CollectionId_MatchesEquality()
    {
        var filter = new MoviesFilterViewModel { CollectionId = 8 };
        Ids(FilterCatalog().AsQueryable().DynamicQuery(filter)).ShouldBe([3]);
    }

    [Fact]
    public void DirectorId_MatchesAnyDirector()
    {
        var filter = new MoviesFilterViewModel { DirectorId = 7 };
        Ids(FilterCatalog().AsQueryable().DynamicQuery(filter)).ShouldBe([2]);
    }

    [Fact]
    public void GenreIds_MatchesAnySelectedGenre()
    {
        var filter = new MoviesFilterViewModel { GenreIds = [3] };
        Ids(FilterCatalog().AsQueryable().DynamicQuery(filter)).ShouldBe([2]);
    }

    [Fact]
    public void ActorIds_MatchesAnySelectedActor()
    {
        var filter = new MoviesFilterViewModel { ActorIds = [11] };
        Ids(FilterCatalog().AsQueryable().DynamicQuery(filter)).ShouldBe([3]);
    }

    private static IQueryable<Movie> Sort(IReadOnlyList<Movie> movies, string sortBy, string sortOrder) =>
        movies.AsQueryable().DynamicSort(null, sortBy, sortOrder);

    private static int[] Ids(IQueryable<Movie> movies) => [.. movies.Select(movie => movie.Id)];

    private static List<Movie> Catalog() =>
    [
        Movie(1, "B", 2000, 90, 7.0m, 50, 40, 9, 5, [1], [10]),
        Movie(2, "A", 1999, 120, 8.5m, 80, 90, null, 7, [3], [12]),
        Movie(3, "C", 2001, 100, 6.0m, 60, 70, 8, 6, [4], [11]),
        Movie(4, null, 1998, 80, 9.0m, 10, 20, null, 4, [2], [13]),
    ];

    private static List<Movie> FilterCatalog() =>
    [
        Movie(1, "Matrix", 2000, 90, 7.0m, 50, 40, 9, 5, [1], [10]),
        Movie(2, "Alien", 1999, 120, 8.5m, 80, 90, null, 7, [3], [12]),
        Movie(3, "Heat", 2001, 100, 6.0m, 60, 70, 8, 6, [4], [11]),
    ];

    private static Movie Movie(
        int id,
        string? title,
        int year,
        int duration,
        decimal imdb,
        int meta,
        int rotten,
        int? collectionId,
        int directorId,
        int[] genreIds,
        int[] actorIds) => new()
    {
        Id = id,
        Title = title,
        Year = year,
        Duration = duration,
        ImdbRating = imdb,
        MetaScore = meta,
        RottenTomatoes = rotten,
        CollectionId = collectionId,
        Directors = [new MovieDirectors { DirectorId = directorId }],
        Genres = [.. genreIds.Select(genre => new MovieGenre { GenreId = genre })],
        Actors = [.. actorIds.Select(actor => new MovieActors { ActorId = actor })],
    };
}
