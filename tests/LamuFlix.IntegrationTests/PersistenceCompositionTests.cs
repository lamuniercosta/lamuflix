using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.ServiceDefaults;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class PersistenceCompositionTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task ComposedRegistration_ResolvesScopedRepositoryAndOneContextPerScope()
    {
        // arrange
        var services = await ComposeAsync();
        await using var provider = services.BuildServiceProvider(Validating());

        // act
        await using var firstScope = provider.CreateAsyncScope();
        await using var secondScope = provider.CreateAsyncScope();
        var first = firstScope.ServiceProvider.GetRequiredService<IMovieRepository>();
        var sameScope = firstScope.ServiceProvider.GetRequiredService<IMovieRepository>();
        var firstContext = firstScope.ServiceProvider.GetRequiredService<LamuFlixDbContext>();
        var sameContext = firstScope.ServiceProvider.GetRequiredService<LamuFlixDbContext>();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<LamuFlixDbContext>();

        // assert
        first.ShouldBeOfType<EfMovieRepository>();
        ReferenceEquals(first, sameScope).ShouldBeTrue();
        ReferenceEquals(firstContext, sameContext).ShouldBeTrue();
        ReferenceEquals(firstContext, secondContext).ShouldBeFalse();
    }

    [Fact]
    public async Task ComposedRegistration_ScopedOutboxSharesTheRepositoryContextAndDiffersAcrossScopes()
    {
        // arrange
        var services = await ComposeAsync();
        await using var provider = services.BuildServiceProvider(Validating());
        await using var firstScope = provider.CreateAsyncScope();
        await using var secondScope = provider.CreateAsyncScope();

        // act
        var firstOutbox = firstScope.ServiceProvider.GetRequiredService<IEnrichmentOutbox>();
        var sameOutbox = firstScope.ServiceProvider.GetRequiredService<IEnrichmentOutbox>();
        var repository = firstScope.ServiceProvider.GetRequiredService<IMovieRepository>();
        var sharedContext = firstScope.ServiceProvider.GetRequiredService<LamuFlixDbContext>();
        firstOutbox.Stage(new EnrichmentRequested(new MovieId(1), 1));
        var stagedOnSharedContext = sharedContext.ChangeTracker
            .Entries<OutboxMessageRecord>()
            .Count();
        var secondOutbox = secondScope.ServiceProvider.GetRequiredService<IEnrichmentOutbox>();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<LamuFlixDbContext>();

        // assert
        firstOutbox.ShouldBeOfType<EfEnrichmentOutbox>();
        repository.ShouldBeOfType<EfMovieRepository>();
        ReferenceEquals(firstOutbox, sameOutbox).ShouldBeTrue();
        ReferenceEquals(secondOutbox, firstOutbox).ShouldBeFalse();
        ReferenceEquals(sharedContext, secondContext).ShouldBeFalse();
        stagedOnSharedContext.ShouldBe(1);
        secondContext.ChangeTracker.Entries<OutboxMessageRecord>().ShouldBeEmpty();
    }

    [Fact]
    public async Task ComposedRegistration_ValidConfiguration_PassesStartupValidation()
    {
        // arrange
        var services = await ComposeAsync();

        // act
        await using var provider = services.BuildServiceProvider(Validating());
        var validators = provider.GetServices<IStartupValidator>().ToArray();

        // assert
        validators.ShouldNotBeEmpty();
        foreach (var validator in validators)
        {
            validator.Validate();
        }
    }

    [Fact]
    public async Task ComposedRegistration_RoundTripsAMovieThroughTheResolvedRepository()
    {
        // arrange
        var services = await ComposeAsync();
        await using var provider = services.BuildServiceProvider(Validating());
        await using var scope = provider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMovieRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var id = await repository.NextIdentityAsync(cancellationToken);
        var movie = Movie.Create(
            id, "Composed Title", new LibraryPath("C:/library/composed.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(movie, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await using var queryScope = provider.CreateAsyncScope();
        var queryRepository = queryScope.ServiceProvider.GetRequiredService<IMovieRepository>();
        var loaded = await queryRepository.GetAsync(id, cancellationToken);

        // assert
        loaded.ShouldNotBeNull();
        loaded.ShouldNotBeSameAs(movie);
        loaded.Id.ShouldBe(id);
        loaded.Title.ShouldBe(movie.Title);
        loaded.Path.ShouldBe(movie.Path);
    }

    [Fact]
    public async Task ComposedRegistration_RoundTripsAParameterizedVectorAndMovieFeaturesOwnsTheOnlyVectorColumn()
    {
        // arrange
        var services = await ComposeAsync();
        await using var provider = services.BuildServiceProvider(Validating());
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LamuFlixDbContext>();
        var sent = new Vector(new float[] { 1, 2, 3 });
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var received = await RoundTripVectorAsync(context, sent, cancellationToken);

        // assert
        received.ShouldBe(sent);
        (await ListVectorColumnsAsync(context, cancellationToken))
            .ShouldBe(["movie_features.vector"]);
    }

    [Fact]
    public void DesignTimeFactory_EnablesVectorDeclaresTheExtensionAndGeneratesTheExpectedMigration()
    {
        // arrange
        var previous = Environment.GetEnvironmentVariable(
            LamuFlixDesignTimeDbContextFactory.ConnectionStringVariable);
        try
        {
            Environment.SetEnvironmentVariable(
                LamuFlixDesignTimeDbContextFactory.ConnectionStringVariable,
                fixture.ConnectionString);
            using var context = new LamuFlixDesignTimeDbContextFactory().CreateDbContext([]);

            // act
            var options = context.GetService<IDbContextOptions>();
            var designTimeModel = context.GetService<IDesignTimeModel>().Model;
            var script = GenerateMigrationScript(context, "_AddMovieWriters", "_EnableVector");

            // assert
            options.Extensions.OfType<VectorDbContextOptionsExtension>().ShouldNotBeEmpty();
            PostgresExtension.FindPostgresExtension(designTimeModel, null, "vector").ShouldNotBeNull();
            script.ShouldContain("CREATE EXTENSION IF NOT EXISTS vector");
            script.ShouldNotContain("CREATE TABLE");
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                LamuFlixDesignTimeDbContextFactory.ConnectionStringVariable,
                previous);
        }
    }

    [Fact]
    public void DesignTimeFactory_GeneratesOmdbDailyUsageMigrationWithDateKeyAndNonNegativeCount()
    {
        // arrange
        var previous = Environment.GetEnvironmentVariable(
            LamuFlixDesignTimeDbContextFactory.ConnectionStringVariable);
        try
        {
            Environment.SetEnvironmentVariable(
                LamuFlixDesignTimeDbContextFactory.ConnectionStringVariable,
                fixture.ConnectionString);
            using var context = new LamuFlixDesignTimeDbContextFactory().CreateDbContext([]);

            // act
            var script = GenerateMigrationScript(context, "_AddMovieCollections", "_AddOmdbDailyUsage");

            // assert
            script.ShouldContain("CREATE TABLE omdb_daily_usage");
            script.ShouldContain("usage_date date NOT NULL");
            script.ShouldContain("request_count integer NOT NULL");
            script.ShouldContain("CONSTRAINT \"PK_omdb_daily_usage\" PRIMARY KEY (usage_date)");
            script.ShouldContain("CONSTRAINT ck_omdb_daily_usage_request_count_nonnegative CHECK (request_count >= 0)");
            script.ShouldNotContain("1000");
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                LamuFlixDesignTimeDbContextFactory.ConnectionStringVariable,
                previous);
        }
    }

    private static string GenerateMigrationScript(
        LamuFlixDbContext context,
        string fromSuffix,
        string toSuffix)
    {
        var migrations = context.Database.GetMigrations().ToArray();
        var from = migrations.Single(id => id.EndsWith(fromSuffix, StringComparison.Ordinal));
        var to = migrations.Single(id => id.EndsWith(toSuffix, StringComparison.Ordinal));
        return context.Database.GetService<IMigrator>().GenerateScript(from, to);
    }

    private static async Task<Vector?> RoundTripVectorAsync(
        LamuFlixDbContext context,
        Vector value,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT @value::vector";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "value";
        parameter.Value = value;
        command.Parameters.Add(parameter);
        return (Vector?)await command.ExecuteScalarAsync(cancellationToken);
    }

    private static async Task<List<string>> ListVectorColumnsAsync(
        LamuFlixDbContext context,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT table_name || '.' || column_name FROM information_schema.columns WHERE table_schema = 'public' AND udt_name = 'vector' ORDER BY 1";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var columns = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    private Task<ServiceCollection> ComposeAsync()
    {
        var connectionString = fixture.ConnectionString;
        connectionString.ShouldNotBeNullOrWhiteSpace();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                [$"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}"] = "C:/library",
                [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}"] = "not-a-real-key",
                [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = "https://example.invalid/",
                [$"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}"] = "localhost",
                [$"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.ClaimLease)}"] = "00:05:00",
                [$"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.SweepInterval)}"] = "00:01:00",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLamuFlixOptions();
        services.AddLogging();
        services.AddSingleton<IEnrichmentStatusNotifier, RecordingEnrichmentStatusNotifier>();
        services.AddLamuFlixPersistence(configuration);
        return Task.FromResult(services);
    }

    private static ServiceProviderOptions Validating() =>
        new() { ValidateOnBuild = true, ValidateScopes = true };
}
