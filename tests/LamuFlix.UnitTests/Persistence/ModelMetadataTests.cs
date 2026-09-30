using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
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
            .UseNpgsql()
            .Options;
        return new LamuFlixDbContext(options);
    }
}