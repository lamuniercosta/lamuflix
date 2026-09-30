using System;
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

        var table = StoreObjectIdentifier.Table("movies", null);
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

        entity.GetIndexes().ShouldContain(index =>
            index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(MovieRecord.Title) &&
            !index.IsUnique);
        entity.GetIndexes().ShouldContain(index =>
            index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(MovieRecord.ReleaseYear) &&
            !index.IsUnique);
        entity.GetIndexes().ShouldContain(index =>
            index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(MovieRecord.Status) &&
            !index.IsUnique);
        entity.GetIndexes().ShouldContain(index =>
            index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(MovieRecord.LibraryPath) &&
            index.IsUnique);
        IIndex? imdb = null;
        foreach (var index in entity.GetIndexes())
        {
            if (index.Properties.Count == 1 &&
                index.Properties[0].Name == nameof(MovieRecord.ImdbId) &&
                index.IsUnique)
            {
                imdb = index;
                break;
            }
        }

        imdb.ShouldNotBeNull();
        imdb.GetFilter().ShouldNotBeNull();
        imdb.GetFilter()!.Replace("\"", string.Empty, StringComparison.Ordinal)
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
