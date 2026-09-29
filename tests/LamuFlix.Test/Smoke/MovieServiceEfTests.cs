using System.Linq;
using LamuFlix.Data.Models;
using LamuFlix.Tests.Common;
using LamuFlix.Web.Library;
using LamuFlix.Web.Models.Movies;
using Shouldly;
using Xunit;

namespace LamuFlix.Test.Smoke;

public sealed class MovieServiceEfTests
{
    [Fact]
    public void ExplicitYearFilterAndTitleSort_TranslatesOnPostgreSql()
    {
        using var context = LamuFlixContextFactory.CreateContext();
        context.Movies.AddRange(
            new Movie { Title = "B", Year = 1999, Location = "a.mkv", Format = "mkv" },
            new Movie { Title = null, Year = 1999, Location = "b.mkv", Format = "mkv" },
            new Movie { Title = "A", Year = 2001, Location = "c.mkv", Format = "mkv" });
        context.SaveChanges();

        var titles = context.Movies
            .ApplyLegacyFilters(new MoviesFilterViewModel { Year = 1999 })
            .ApplyLegacySort("Title", "asc")
            .Select(movie => movie.Title)
            .ToList();

        titles.ShouldBe(["B", null], "PostgreSQL-only smoke; MySQL translation on Pomelo 8.0.31 is unproven");
    }
}
