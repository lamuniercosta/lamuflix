using System.Collections.Generic;
using System.Linq;
using LamuFlix.Data;
using LamuFlix.Data.Models;
using LamuFlix.Data.Repositories;
using LamuFlix.Tests.Common;
using Shouldly;
using Xunit;

namespace LamuFlix.Test
{
    public class GenericRepositoryGetByIdTests
    {
        private const int MissingId = 2;

        [Fact]
        public void GetById_WhenEntityExists_ReturnsEntity()
        {
            using var context = LamuFlixContextFactory.CreateContext();
            var movie = SeedMovie(context);
            var repository = new GenericRepository<Movie>(context);

            var found = repository.GetById(movie.Id);

            found.ShouldBe(movie);
        }

        [Fact]
        public void GetById_WhenEntityMissing_ThrowsKeyNotFoundException()
        {
            using var context = LamuFlixContextFactory.CreateContext();
            SeedMovie(context);
            var repository = new GenericRepository<Movie>(context);

            var ex = Should.Throw<KeyNotFoundException>(() => repository.GetById(MissingId));

            ex.Message.ShouldBe($"Movie with id '{MissingId}' was not found.");
        }

        [Fact]
        public void GetById_WhenCalledThroughInterfaceAndEntityExists_ReturnsSingleElementSequence()
        {
            using var context = LamuFlixContextFactory.CreateContext();
            var movie = SeedMovie(context);
            IGenericRepository<Movie> repository = new GenericRepository<Movie>(context);

            var found = repository.GetById(movie.Id).ToList();

            found.Count.ShouldBe(1);
            found[0].ShouldBe(movie);
        }

        [Fact]
        public void GetById_WhenCalledThroughInterfaceAndEntityMissing_ThrowsKeyNotFoundException()
        {
            using var context = LamuFlixContextFactory.CreateContext();
            SeedMovie(context);
            IGenericRepository<Movie> repository = new GenericRepository<Movie>(context);

            var ex = Should.Throw<KeyNotFoundException>(() => repository.GetById(MissingId));

            ex.Message.ShouldBe($"Movie with id '{MissingId}' was not found.");
        }

        private static Movie SeedMovie(LamuFlixContext context)
        {
            var movie = new Movie
            {
                Id = 1,
                Title = "Test Movie",
                Location = @"C:\TestLibrary\Test[2020]\test.mkv",
                Format = ".mkv"
            };
            context.Movies.Add(movie);
            context.SaveChanges();
            return movie;
        }
    }
}
