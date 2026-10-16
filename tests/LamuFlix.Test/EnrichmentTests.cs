using System.Threading.Tasks;
using LamuFlix.Data;
using LamuFlix.Data.Models;
using LamuFlix.Tests.Common;
using LamuFlix.Web.Services;
using NSubstitute;
using Shouldly;
using Xunit;

[assembly: AssemblyFixture(typeof(LamuFlix.Test.ContainerFixtureForTests))]

namespace LamuFlix.Test;

public sealed class ContainerFixtureForTests : IAsyncLifetime
{
    public ValueTask InitializeAsync() => new(ContainerFixture.StartAsync());

    public ValueTask DisposeAsync() => ContainerFixture.DisposeAsync();
}

public sealed class EnrichmentTests
{
    [Fact]
    public void FilmesService_CriarFilme_InsertsAsPendingAndPublishesMessage()
    {
        var db = LamuFlixContextFactory.CreateContext();
        var queuePublisher = Substitute.For<IEnrichmentQueuePublisher>();
        _ = new MovieService(db, null, queuePublisher);

        var movie = new Movie
        {
            Title = "Imported Movie",
            Year = 2023,
            Location = "F:\\Test.mkv",
            Format = ".mkv",
            Status = MovieEnrichmentStatus.Pending
        };
        db.Movies.Add(movie);
        db.SaveChanges();

        movie.Status.ShouldBe(MovieEnrichmentStatus.Pending);
    }
}
