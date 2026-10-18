using System.Linq;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;

namespace LamuFlix.UnitTests.Persistence;

public sealed class MovieConfigurationTests
{
    [Fact]
    public void Movies_HasSingleColumnPrimaryKeyOnId()
    {
        // arrange
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql("Host=unused;Username=unused;Password=unused;Database=unused")
            .Options;

        // act
        using var context = new LamuFlixDbContext(options);
        var entity = context.Model.FindEntityType(typeof(MovieRecord)).ShouldNotBeNull();

        // assert
        var key = entity.FindPrimaryKey().ShouldNotBeNull();
        key.Properties.Select(property => property.Name).ShouldBe(["Id"]);
    }
}
