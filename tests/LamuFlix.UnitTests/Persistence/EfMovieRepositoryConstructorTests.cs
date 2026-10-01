using System;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LamuFlix.UnitTests.Persistence;

public sealed class EfMovieRepositoryConstructorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonPositiveClaimLease(int seconds)
    {
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
            .Options;
        using var db = new LamuFlixDbContext(options);

        Should.Throw<ArgumentOutOfRangeException>(() => new EfMovieRepository(
            db,
            TimeProvider.System,
            Microsoft.Extensions.Options.Options.Create(
                new EnrichmentOptions { ClaimLease = TimeSpan.FromSeconds(seconds) })));
    }
}