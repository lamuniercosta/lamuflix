using System;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LamuFlix.UnitTests.Persistence;

public sealed class EfMovieRepositoryConstructorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonPositiveClaimLease(int seconds)
    {
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused", o => o.UseVector())
            .Options;
        using var db = new LamuFlixDbContext(options);

        Should.Throw<ArgumentOutOfRangeException>(() => new EfMovieRepository(
            db,
            TimeProvider.System,
            Microsoft.Extensions.Options.Options.Create(
                new EnrichmentOptions { ClaimLease = TimeSpan.FromSeconds(seconds) }),
            Substitute.For<IEnrichmentStatusNotifier>(),
            NullLogger<EfMovieRepository>.Instance));
    }
}