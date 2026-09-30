using System;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LamuFlix.UnitTests.Persistence;

public sealed class MovieModelTests
{
    [Fact]
    public void MoviesKey_UsesMovieIdConverterAndValueGeneratedOnAdd()
    {
        using var context = CreateContext();
        var entity = RequireEntity(context, typeof(MovieRecord));
        var id = RequireProperty(entity, nameof(MovieRecord.Id));

        var converter = id.GetValueConverter().ShouldNotBeNull();
        converter.ConvertToProvider(new MovieId(5)).ShouldBe(5);
        id.ValueGenerated.ShouldBe(ValueGenerated.OnAdd);
    }

    [Fact]
    public void MoviesColumns_MatchFr005AndFr005aNamesTypesAndNullability()
    {
        using var context = CreateContext();
        var entity = RequireEntity(context, typeof(MovieRecord));
        entity.GetTableName().ShouldBe("movies");
        context.Model.FindEntityType(typeof(Movie)).ShouldBeNull();

        var table = StoreObjectIdentifier.Table("movies");
        AssertColumn(entity, table, nameof(MovieRecord.Id), "id", "integer", false);
        AssertColumn(entity, table, nameof(MovieRecord.Title), "title", "text", false);
        AssertColumn(entity, table, nameof(MovieRecord.LibraryPath), "library_path", "text", false);
        AssertColumn(entity, table, nameof(MovieRecord.Format), "format", "text", false);
        AssertColumn(entity, table, nameof(MovieRecord.IsInWatchlist), "is_in_watchlist", "boolean", false);
        AssertColumn(entity, table, nameof(MovieRecord.Status), "status", "integer", false);
        AssertColumn(entity, table, nameof(MovieRecord.EnrichedAt), "enriched_at", "timestamptz", true);
        AssertColumn(entity, table, nameof(MovieRecord.EnrichmentAttempts), "enrichment_attempts", "integer", false);
        AssertColumn(entity, table, nameof(MovieRecord.EnrichmentFailureCategory), "enrichment_failure_category", "varchar(32)", true);
        AssertColumn(entity, table, nameof(MovieRecord.LastAttemptAt), "last_attempt_at", "timestamptz", true);
        AssertColumn(entity, table, nameof(MovieRecord.MetadataTitle), "metadata_title", "text", true);
        AssertColumn(entity, table, nameof(MovieRecord.RuntimeMinutes), "runtime_minutes", "integer", true);
        AssertColumn(entity, table, nameof(MovieRecord.ReleaseYear), "release_year", "integer", true);
        AssertColumn(entity, table, nameof(MovieRecord.ImdbRating), "imdb_rating", "numeric(3,1)", true);
        AssertColumn(entity, table, nameof(MovieRecord.ImdbId), "imdb_id", "text", true);
        AssertColumn(entity, table, nameof(MovieRecord.RottenTomatoesRating), "rotten_tomatoes_rating", "smallint", true);
        AssertColumn(entity, table, nameof(MovieRecord.MetaScore), "meta_score", "smallint", true);
        AssertColumn(entity, table, nameof(MovieRecord.Plot), "plot", "text", true);
        AssertColumn(entity, table, nameof(MovieRecord.PosterUrl), "poster_url", "text", true);
    }

    [Fact]
    public void MoviesIndexes_IncludeUniqueLibraryPathAndFilteredImdbId()
    {
        using var context = CreateContext();
        var entity = RequireEntity(context, typeof(MovieRecord));

        AssertSinglePropertyIndex(entity, nameof(MovieRecord.Title), false);
        AssertSinglePropertyIndex(entity, nameof(MovieRecord.ReleaseYear), false);
        AssertSinglePropertyIndex(entity, nameof(MovieRecord.Status), false);
        AssertSinglePropertyIndex(entity, nameof(MovieRecord.LibraryPath), true);
        AssertFilteredUniqueImdbIdIndex(FindUniqueSinglePropertyIndex(entity, nameof(MovieRecord.ImdbId)));
    }

    [Fact]
    public void MoviesSkipNavigations_UseExpectedJoinTablesAndForeignKeys()
    {
        using var context = CreateContext();
        var movie = RequireEntity(context, typeof(MovieRecord));

        AssertSkipNavigation(movie, nameof(MovieRecord.Actors), "movie_actors", "actor_id");
        AssertSkipNavigation(movie, nameof(MovieRecord.Directors), "movie_directors", "director_id");
        AssertSkipNavigation(movie, nameof(MovieRecord.Genres), "movie_genres", "genre_id");
    }

    private static void AssertSkipNavigation(IEntityType movie, string name, string joinTable, string relatedForeignKey)
    {
        var navigation = movie.FindSkipNavigation(name);
        navigation.ShouldNotBeNull();
        navigation.JoinEntityType.GetTableName().ShouldBe(joinTable);
        navigation.ForeignKey.Properties.Select(property => property.Name).ShouldBe(new[] { "movie_id" });

        var inverse = navigation.Inverse;
        inverse.ShouldNotBeNull();
        inverse.ForeignKey.Properties.Select(property => property.Name).ShouldBe(new[] { relatedForeignKey });
        navigation.JoinEntityType.FindProperty("movie_id").ShouldNotBeNull();
        navigation.JoinEntityType.FindProperty(relatedForeignKey).ShouldNotBeNull();
    }

    private static void AssertSinglePropertyIndex(IEntityType entity, string propertyName, bool unique) =>
        entity.GetIndexes().ShouldContain(index => IsSinglePropertyIndex(index, propertyName, unique));

    private static bool IsSinglePropertyIndex(IIndex index, string propertyName, bool unique) =>
        index.Properties.Count == 1 &&
        index.Properties[0].Name == propertyName &&
        index.IsUnique == unique;

    private static IIndex? FindUniqueSinglePropertyIndex(IEntityType entity, string propertyName)
    {
        foreach (var index in entity.GetIndexes())
        {
            if (IsSinglePropertyIndex(index, propertyName, true))
            {
                return index;
            }
        }

        return null;
    }

    private static void AssertFilteredUniqueImdbIdIndex(IIndex? imdb)
    {
        imdb.ShouldNotBeNull();
        var filter = imdb.GetFilter();
        filter.ShouldNotBeNull();
        filter.Replace("\"", string.Empty, StringComparison.Ordinal)
            .ShouldContain("imdb_id IS NOT NULL");
    }

    private static LamuFlixDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql()
            .Options;
        return new LamuFlixDbContext(options);
    }

    private static IEntityType RequireEntity(LamuFlixDbContext context, Type clrType)
    {
        var entity = context.Model.FindEntityType(clrType);
        entity.ShouldNotBeNull();
        return entity;
    }

    private static IProperty RequireProperty(IEntityType entity, string name)
    {
        var property = entity.FindProperty(name);
        property.ShouldNotBeNull();
        return property;
    }

    private static void AssertColumn(
        IEntityType entity,
        StoreObjectIdentifier table,
        string propertyName,
        string columnName,
        string storeType,
        bool nullable)
    {
        var property = RequireProperty(entity, propertyName);
        var column = property.FindColumn(table);
        column.ShouldNotBeNull();
        column.Name.ShouldBe(columnName);
        column.StoreType.ShouldBe(storeType);
        column.IsNullable.ShouldBe(nullable);
    }
}
