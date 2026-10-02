using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class PostgresFixtureTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Startup_MigratedDatabase_AppliesTheKnownMigrationsAndOpensAConnection()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateMigratedContext();

        // act
        var applied = await context.Database.GetAppliedMigrationsAsync(cancellationToken);
        var version = await ServerVersionAsync(context, cancellationToken);
        var connected = await context.Database.CanConnectAsync(cancellationToken);
        TestContext.Current.TestOutputHelper?.WriteLine($"[postgres-server-version] {version}");
        TestContext.Current.TestOutputHelper?.WriteLine($"[postgres-container-id] {fixture.Container.Id}");

        // assert
        applied.ShouldBe(context.Database.GetMigrations());
        version.ShouldNotBeNullOrWhiteSpace();
        connected.ShouldBeTrue();
    }

    [Fact]
    public async Task ResetAsync_SeededMovie_EmptiesEveryApplicationTableAndKeepsMigrationHistory()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var movie = MovieCatalogSeed.Create("Seeded", "fixture-reset");
        movie.Actors.Add(new ActorRecord { Name = "Ada Actor" });
        movie.Directors.Add(new DirectorRecord { Name = "Dana Director" });
        movie.Genres.Add(new GenreRecord { Name = "Drama" });
        await using (var seeding = fixture.CreateMigratedContext())
        {
            await MovieCatalogSeed.AddAsync(seeding, movie, cancellationToken);
        }

        // act
        await fixture.ResetAsync(cancellationToken);
        await using var context = fixture.CreateMigratedContext();

        // assert
        (await context.Movies.CountAsync(cancellationToken)).ShouldBe(0);
        (await context.Actors.CountAsync(cancellationToken)).ShouldBe(0);
        (await context.Directors.CountAsync(cancellationToken)).ShouldBe(0);
        (await context.Genres.CountAsync(cancellationToken)).ShouldBe(0);
        (await context.Movies.SelectMany(item => item.Actors).CountAsync(cancellationToken)).ShouldBe(0);
        (await context.Movies.SelectMany(item => item.Directors).CountAsync(cancellationToken)).ShouldBe(0);
        (await context.Movies.SelectMany(item => item.Genres).CountAsync(cancellationToken)).ShouldBe(0);
        (await context.Database.GetAppliedMigrationsAsync(cancellationToken))
            .ShouldBe(context.Database.GetMigrations());
    }

    [Fact]
    public async Task ResetAsync_AfterTwoResets_TheFirstInsertGetsTheSameIdentity()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var first = await FirstIdentityAsync(cancellationToken);
        var second = await FirstIdentityAsync(cancellationToken);

        // assert
        second.ShouldBe(first);
    }

    [Fact]
    public async Task ResetAsync_Afterwards_InsertsAndQueriesAgain()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(cancellationToken);

        // act
        await using var context = fixture.CreateMigratedContext();
        await MovieCatalogSeed.AddAsync(context, MovieCatalogSeed.Create("After Reset", "reset-usable"), cancellationToken);
        var titles = await context.Movies.Select(movie => movie.Title).ToArrayAsync(cancellationToken);

        // assert
        titles.ShouldBe(["After Reset"]);
    }

    [Fact]
    public async Task DisposeAsync_WithoutInitialization_Completes()
    {
        // arrange
        var neverInitialized = new PostgresFixture();

        // act
        var act = () => neverInitialized.DisposeAsync().AsTask();

        // assert
        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_Completes()
    {
        // arrange
        var neverInitialized = new PostgresFixture();
        await neverInitialized.DisposeAsync();

        // act
        var act = () => neverInitialized.DisposeAsync().AsTask();

        // assert
        await Should.NotThrowAsync(act);
    }

    private async Task<int> FirstIdentityAsync(CancellationToken cancellationToken)
    {
        await fixture.ResetAsync(cancellationToken);
        await using var context = fixture.CreateMigratedContext();
        var movie = await MovieCatalogSeed.AddAsync(
            context,
            MovieCatalogSeed.Create("Identity", "fixture-identity"),
            cancellationToken);
        var id = movie.Id;
        id.ShouldNotBeNull();
        return id.Value;
    }

    private static async Task<string> ServerVersionAsync(LamuFlixDbContext context, CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SHOW server_version";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? reader.GetString(0)
            : throw new InvalidOperationException("PostgreSQL reported no server version.");
    }
}