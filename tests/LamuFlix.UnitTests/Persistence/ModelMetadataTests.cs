using System;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Converters;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LamuFlix.UnitTests.Persistence;

public sealed class ModelMetadataTests
{
    [Fact]
    public void Actor_MetadataMatchesDatabaseMapping()
    {
        using var context = CreateContext();
        AssertNamedEntity<ActorRecord>(context, "actors");
    }

    [Fact]
    public void Director_MetadataMatchesDatabaseMapping()
    {
        using var context = CreateContext();
        AssertNamedEntity<DirectorRecord>(context, "directors");
    }

    [Fact]
    public void Genre_MetadataMatchesDatabaseMapping()
    {
        using var context = CreateContext();
        AssertNamedEntity<GenreRecord>(context, "genres");
    }

    [Fact]
    public void Writer_MetadataMatchesDatabaseMapping()
    {
        using var context = CreateContext();
        AssertNamedEntity<WriterRecord>(context, "writers");
    }

    [Fact]
    public void Writer_IdIsValueGeneratedOnAdd()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(WriterRecord));
        entity.ShouldNotBeNull();
        var id = entity.FindProperty("Id");
        id.ShouldNotBeNull();
        id.ValueGenerated.ShouldBe(ValueGenerated.OnAdd);
    }

    [Fact]
    public void PlaybackEvent_MetadataMatchesDatabaseMapping()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(PlaybackEventRecord));
        entity.ShouldNotBeNull();
        entity.GetTableName().ShouldBe("playback_events");
        var table = StoreObjectIdentifier.Table("playback_events");

        var id = entity.FindProperty("Id");
        id.ShouldNotBeNull();
        id.GetColumnName(table).ShouldBe("id");
        id.GetColumnType().ShouldBe("integer");
        id.ValueGenerated.ShouldBe(ValueGenerated.OnAdd);
        entity.FindPrimaryKey().ShouldNotBeNull().Properties.ShouldContain(id);

        var movieId = entity.FindProperty("MovieId");
        movieId.ShouldNotBeNull();
        movieId.GetColumnName(table).ShouldBe("movie_id");
        movieId.GetColumnType().ShouldBe("integer");
        movieId.IsNullable.ShouldBeFalse();
        movieId.GetValueConverter().ShouldBeOfType<MovieIdConverter>();

        var playedAt = entity.FindProperty("PlayedAt");
        playedAt.ShouldNotBeNull();
        playedAt.GetColumnName(table).ShouldBe("played_at");
        playedAt.GetColumnType().ShouldBe("timestamptz");
        playedAt.IsNullable.ShouldBeFalse();

        var index = entity.GetIndexes().ShouldHaveSingleItem();
        index.Properties.ShouldBe([movieId]);
        index.IsUnique.ShouldBeFalse();

        var foreignKey = entity.GetForeignKeys().ShouldHaveSingleItem();
        foreignKey.PrincipalEntityType.ClrType.ShouldBe(typeof(MovieRecord));
        foreignKey.Properties.ShouldBe([movieId]);
        foreignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
    }

    [Fact]
    public void MovieFeatures_MetadataMatchesDatabaseMapping()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(MovieFeaturesRecord));
        entity.ShouldNotBeNull();
        entity.GetTableName().ShouldBe("movie_features");
        var table = StoreObjectIdentifier.Table("movie_features");

        var movieId = entity.FindProperty("MovieId");
        movieId.ShouldNotBeNull();
        movieId.GetColumnName(table).ShouldBe("movie_id");
        movieId.GetColumnType().ShouldBe("integer");
        movieId.IsNullable.ShouldBeFalse();
        movieId.ValueGenerated.ShouldBe(ValueGenerated.Never);
        movieId.GetValueConverter().ShouldBeOfType<MovieIdConverter>();
        entity.FindPrimaryKey().ShouldNotBeNull().Properties.ShouldBe([movieId]);

        var vector = entity.FindProperty("Vector");
        vector.ShouldNotBeNull();
        vector.GetColumnName(table).ShouldBe("vector");
        vector.GetColumnType().ShouldBe("vector(384)");
        vector.IsNullable.ShouldBeFalse();

        var computedAt = entity.FindProperty("ComputedAt");
        computedAt.ShouldNotBeNull();
        computedAt.GetColumnName(table).ShouldBe("computed_at");
        computedAt.GetColumnType().ShouldBe("timestamptz");
        computedAt.IsNullable.ShouldBeFalse();

        entity.GetIndexes().ShouldBeEmpty();

        var foreignKey = entity.GetForeignKeys().ShouldHaveSingleItem();
        foreignKey.PrincipalEntityType.ClrType.ShouldBe(typeof(MovieRecord));
        foreignKey.Properties.ShouldBe([movieId]);
        foreignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
    }

    private static void AssertNamedEntity<TEntity>(LamuFlixDbContext context, string tableName)
        where TEntity : class
    {
        var entity = context.Model.FindEntityType(typeof(TEntity));
        entity.ShouldNotBeNull();
        entity.GetTableName().ShouldBe(tableName);

        var table = StoreObjectIdentifier.Table(tableName);
        var id = entity.FindProperty("Id");
        id.ShouldNotBeNull();
        id.GetColumnName(table).ShouldBe("id");
        id.GetColumnType().ShouldBe("integer");
        entity.FindPrimaryKey().ShouldNotBeNull().Properties.ShouldContain(id);

        var name = entity.FindProperty("Name");
        name.ShouldNotBeNull();
        name.GetColumnName(table).ShouldBe("name");
        name.GetColumnType().ShouldBe("text");
        name.IsNullable.ShouldBeFalse();

        entity.GetIndexes().ShouldBeEmpty();
    }

    private static LamuFlixDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(o => o.UseVector())
            .ReplaceService<IModelCacheKeyFactory, UncachedModelCacheKeyFactory>()
            .Options;
        return new LamuFlixDbContext(options);
    }

    private sealed class UncachedModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime) => Guid.NewGuid();
    }
}