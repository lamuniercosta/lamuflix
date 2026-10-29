using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class MigrationTests(PostgresFixture fixture)
{
    private static readonly string[] ApplicationTables =
    [
        "movies",
        "actors",
        "directors",
        "genres",
        "writers",
        "movie_actors",
        "movie_directors",
        "movie_genres",
        "movie_writers",
        "playback_events",
        "movie_features",
        "outbox_messages",
        "collections"
    ];

    private static readonly string[] NullableMoviesColumns =
    [
        "metadata_title",
        "runtime_minutes",
        "release_year",
        "imdb_rating",
        "imdb_id",
        "rotten_tomatoes_rating",
        "meta_score",
        "plot",
        "poster_url",
        "enriched_at",
        "enrichment_failure_category",
        "last_attempt_at",
        "parsed_release_year",
        "manual_imdb_id",
        "collection_id"
    ];

    private static readonly Regex SnakeCase = new(
        "^[a-z][a-z0-9]*(_[a-z0-9]+)*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    // Legacy rows are inserted with SQL that names only the columns the older schema has, because EF's
    // INSERT for the current model also writes parsed_release_year, which does not exist before that migration.
    private static Task InsertLegacyMovieAsync(
        DbContext context,
        string title,
        string libraryPath,
        CancellationToken cancellationToken)
    {
        return context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO movies (title, library_path, format, is_in_watchlist, status, enrichment_attempts) VALUES ({title}, {libraryPath}, 'mkv', false, {(int)EnrichmentStatus.Pending}, 0)",
            cancellationToken);
    }

    [Fact]
    public async Task MigrateAsync_EmptyDatabase_AppliesMigrationsEnablesVectorAndHasNoPendingModelChanges()
    {
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(TestContext.Current.CancellationToken);

        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        var cancellationToken = TestContext.Current.CancellationToken;
        var migrations = context.Database.GetMigrations().ToArray();
        migrations.Length.ShouldBe(10);
        migrations[0].ShouldEndWith("_Initial");
        migrations[1].ShouldEndWith("_AddMovieWriters");
        migrations[2].ShouldEndWith("_EnableVector");
        migrations[3].ShouldEndWith("_AddPlaybackEvents");
        migrations[4].ShouldEndWith("_AddMovieFeedbackMarks");
        migrations[5].ShouldEndWith("_AddParsedReleaseYear");
        migrations[6].ShouldEndWith("_AddManualImdbId");
        migrations[7].ShouldEndWith("_AddMovieFeatures");
        migrations[8].ShouldEndWith("_AddEnrichmentOutbox");
        migrations[9].ShouldEndWith("_AddMovieCollections");
        var applied = await context.Database.GetAppliedMigrationsAsync(cancellationToken);
        applied.ShouldBe(migrations);
        context.Database.HasPendingModelChanges().ShouldBeFalse();
        (await new PostgresCatalog(context).ListPostgresExtensionsAsync(cancellationToken)).ShouldContain("vector");
    }

    [Fact]
    public async Task MigrateAsync_FromAddMovieWritersWithExistingRows_UpgradesToLatestAndKeepsRowsAndCatalog()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var catalog = new PostgresCatalog(context);
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20261016195556_AddMovieWriters", cancellationToken);
        var seeded = new[]
        {
            new LegacyMovie("Upgrade Survivor", "C:/library/upgrade-survivor.mkv", "mkv", 0),
            new LegacyMovie("Upgrade Second", "C:/library/upgrade-second.mp4", "mp4", 1),
            new LegacyMovie("Upgrade Third", "C:/library/upgrade-third.avi", "avi", 2),
        };
        foreach (var movie in seeded)
        {
            await InsertLegacyMovieAsync(context, movie, cancellationToken);
        }

        var priorCount = await ReadLegacyMovieCountAsync(context, cancellationToken);
        var priorTitles = await ReadLegacyMovieTitlesAsync(context, cancellationToken);
        priorCount.ShouldBeGreaterThan(0);
        priorTitles.ShouldBe(seeded.Select(movie => movie.Title), ignoreOrder: true);

        await migrator.MigrateAsync(null, cancellationToken);

        var applied = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
        applied.ShouldBe(context.Database.GetMigrations());
        applied[^1].ShouldEndWith("_AddMovieCollections");
        var rows = await context.Movies.AsNoTracking()
            .OrderBy(movie => movie.Title)
            .ToListAsync(cancellationToken);
        rows.Count.ShouldBe(priorCount);
        foreach (var movie in seeded)
        {
            var row = rows.Single(item => item.Title == movie.Title);
            row.LibraryPath.Value.ShouldBe(movie.LibraryPath);
            row.Format.Extension.ShouldBe(movie.Format);
            row.Status.Value.ShouldBe(movie.Status);
            row.IsWatched.ShouldBeFalse();
            row.IsNotInterested.ShouldBeFalse();
        }

        await AssertFeedbackColumnsAsync(catalog, cancellationToken);
        await AssertPublicTablesAsync(catalog, cancellationToken);
        (await catalog.ListPostgresExtensionsAsync(cancellationToken)).ShouldContain("vector");
        context.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    [Fact]
    public async Task MigrateAsync_AddMovieFeedbackMarksRolledBack_DropsOnlyTheTwoColumnsAndKeepsRows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var catalog = new PostgresCatalog(context);
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(null, cancellationToken);
        await AssertFeedbackColumnsAsync(catalog, cancellationToken);
        var latestColumns = await catalog.ListColumnsAsync("movies", cancellationToken);
        var seeded = new[]
        {
            new MovieRecord
            {
                Title = "Down Survivor",
                LibraryPath = new LibraryPath("C:/library/down-survivor.mkv"),
                Format = new MediaFormat("mkv"),
                Status = EnrichmentStatus.Pending,
                IsWatched = true,
                IsNotInterested = true,
                IsInWatchlist = true,
            },
            new MovieRecord
            {
                Title = "Down Second",
                LibraryPath = new LibraryPath("C:/library/down-second.mp4"),
                Format = new MediaFormat("mp4"),
                Status = EnrichmentStatus.Enriched,
            },
        };
        context.Movies.AddRange(seeded);
        await context.SaveChangesAsync(cancellationToken);
        var expected = seeded
            .Select(movie => new LegacyMovie(movie.Title, movie.LibraryPath.Value, movie.Format.Extension, movie.Status.Value))
            .OrderBy(movie => movie.Title)
            .ToArray();

        await migrator.MigrateAsync("20261016195556_AddMovieWriters", cancellationToken);

        var applied = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
        applied.Length.ShouldBe(2);
        applied[0].ShouldEndWith("_Initial");
        applied[1].ShouldEndWith("_AddMovieWriters");
        var priorColumns = await catalog.ListColumnsAsync("movies", cancellationToken);
        priorColumns.ShouldBe(
            latestColumns.Where(column => column is not "is_watched" and not "is_not_interested" and not "parsed_release_year" and not "manual_imdb_id" and not "collection_id"),
            ignoreOrder: true);
        priorColumns.ShouldNotContain("is_watched");
        priorColumns.ShouldNotContain("is_not_interested");
        priorColumns.ShouldNotContain("parsed_release_year");
        priorColumns.ShouldNotContain("manual_imdb_id");
        var rows = await catalog.ListLegacyMoviesAsync(cancellationToken);
        rows.ShouldBe(expected, ignoreOrder: true);
        context.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    [Fact]
    public async Task MigrateAsync_EnableVectorRolledBack_ReturnsToAddMovieWritersAndReapplies()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(null, cancellationToken);

        await migrator.MigrateAsync("20261016195556_AddMovieWriters", cancellationToken);

        var rolledBack = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
        rolledBack.Length.ShouldBe(2);
        rolledBack[0].ShouldEndWith("_Initial");
        rolledBack[1].ShouldEndWith("_AddMovieWriters");

        await migrator.MigrateAsync(null, cancellationToken);

        (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).Last().ShouldEndWith("_AddMovieCollections");
    }

    [Fact]
    public async Task MigrateAsync_FromInitialWithExistingMovie_AddsWriterTablesWithoutFabricatingWriters()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20260930153520_Initial", cancellationToken);
        await InsertLegacyMovieAsync(context, "Legacy", "C:/library/legacy.mkv", cancellationToken);

        await migrator.MigrateAsync(null, cancellationToken);

        (await context.Movies.CountAsync(cancellationToken)).ShouldBe(1);
        (await context.Writers.CountAsync(cancellationToken)).ShouldBe(0);
        context.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    [Fact]
    public async Task MigrateAsync_SecondMigrationRolledBack_DropsWriterTablesAndKeepsExistingData()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(null, cancellationToken);

        var movie = new MovieRecord
        {
            Title = "Survivor",
            LibraryPath = new LibraryPath("C:/library/survivor.mkv"),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Pending
        };
        movie.Writers.Add(new WriterRecord { Name = "Rollback Writer" });
        context.Movies.Add(movie);
        await context.SaveChangesAsync(cancellationToken);

        await migrator.MigrateAsync("20260930153520_Initial", cancellationToken);

        var publicTables = await new PostgresCatalog(context).ListPublicTablesAsync(cancellationToken);
        publicTables.ShouldContain("movies");
        publicTables.ShouldNotContain("writers");
        publicTables.ShouldNotContain("movie_writers");
        (await context.Movies.CountAsync(cancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task MigrateAsync_AddPlaybackEventsUpgradeDowngradeReapply_PreservesMoviesAndDropsEvents()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20261016195556_AddMovieWriters", cancellationToken);
        await InsertLegacyMovieAsync(
            context,
            new LegacyMovie("Upgrade Survivor", "C:/library/upgrade-survivor.mkv", "mkv", 0),
            cancellationToken);

        await migrator.MigrateAsync(null, cancellationToken);
        context.ChangeTracker.Clear();

        var movieId = (await context.Movies.AsNoTracking().SingleAsync(cancellationToken)).Id;
        movieId.ShouldNotBeNull();

        var catalog = new PostgresCatalog(context);
        (await catalog.ListPublicTablesAsync(cancellationToken)).ShouldContain("playback_events");
        (await context.Movies.CountAsync(cancellationToken)).ShouldBe(1);
        await AssertPlaybackEventsColumnsAsync(catalog, cancellationToken);
        await AssertPlaybackEventsForeignKeyAsync(catalog, cancellationToken);
        await AssertPlaybackEventsIndexAsync(catalog, cancellationToken);
        context.PlaybackEvents.Add(new PlaybackEventRecord
        {
            MovieId = movieId,
            PlayedAt = new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.Zero)
        });
        await context.SaveChangesAsync(cancellationToken);

        await migrator.MigrateAsync("20261016195556_AddMovieWriters", cancellationToken);
        context.ChangeTracker.Clear();

        (await catalog.ListPublicTablesAsync(cancellationToken)).ShouldNotContain("playback_events");
        (await ReadLegacyMovieCountAsync(context, cancellationToken)).ShouldBe(1);

        await migrator.MigrateAsync(null, cancellationToken);
        context.ChangeTracker.Clear();

        (await catalog.ListPublicTablesAsync(cancellationToken)).ShouldContain("playback_events");
        (await context.Movies.CountAsync(cancellationToken)).ShouldBe(1);
        (await context.PlaybackEvents.CountAsync(cancellationToken)).ShouldBe(0);
        context.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    [Fact]
    public async Task MigrateAsync_AddMovieFeaturesUpgradeDowngradeReapply_KeepsMoviesAndVectorExtension()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var catalog = new PostgresCatalog(context);
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20261019002819_AddManualImdbId", cancellationToken);
        await InsertLegacyMovieAsync(
            context,
            new LegacyMovie("Features Survivor", "C:/library/features-survivor.mkv", "mkv", 0),
            cancellationToken);

        await migrator.MigrateAsync(null, cancellationToken);
        context.ChangeTracker.Clear();

        (await catalog.ListPublicTablesAsync(cancellationToken)).ShouldContain("movie_features");
        (await catalog.ListPostgresExtensionsAsync(cancellationToken)).ShouldContain("vector");
        await AssertMovieFeaturesColumnsAsync(catalog, cancellationToken);
        await AssertMovieFeaturesConstraintsAsync(catalog, cancellationToken);
        await AssertMovieFeaturesIndexAsync(catalog, cancellationToken);
        (await ReadLegacyMovieCountAsync(context, cancellationToken)).ShouldBe(1);
        context.Database.HasPendingModelChanges().ShouldBeFalse();

        await migrator.MigrateAsync("20261019002819_AddManualImdbId", cancellationToken);
        context.ChangeTracker.Clear();

        (await catalog.ListPublicTablesAsync(cancellationToken)).ShouldNotContain("movie_features");
        (await catalog.ListPostgresExtensionsAsync(cancellationToken)).ShouldContain("vector");
        (await ReadLegacyMovieCountAsync(context, cancellationToken)).ShouldBe(1);
        (await catalog.ListTableIndexesAsync("movie_features", cancellationToken)).ShouldBeEmpty();

        await migrator.MigrateAsync(null, cancellationToken);
        context.ChangeTracker.Clear();

        (await catalog.ListPublicTablesAsync(cancellationToken)).ShouldContain("movie_features");
        (await catalog.ListPostgresExtensionsAsync(cancellationToken)).ShouldContain("vector");
        await AssertMovieFeaturesColumnsAsync(catalog, cancellationToken);
        await AssertMovieFeaturesConstraintsAsync(catalog, cancellationToken);
        await AssertMovieFeaturesIndexAsync(catalog, cancellationToken);
        (await ReadLegacyMovieCountAsync(context, cancellationToken)).ShouldBe(1);
        context.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    [Fact]
    public async Task MigrateAsync_AddEnrichmentOutboxUpgradeDowngradeReapply_AddsOnlyTheFourColumnOutboxAndPendingIndex()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        var catalog = new PostgresCatalog(context);
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20261022101541_AddMovieFeatures", cancellationToken);
        await InsertLegacyMovieAsync(
            context,
            new LegacyMovie("Outbox Survivor", "C:/library/outbox-survivor.mkv", "mkv", 0),
            cancellationToken);
        (await catalog.ListPublicTablesAsync(cancellationToken)).ShouldNotContain("outbox_messages");

        // act
        await migrator.MigrateAsync(null, cancellationToken);
        context.ChangeTracker.Clear();

        // assert
        (await catalog.ListPublicTablesAsync(cancellationToken)).ShouldContain("outbox_messages");
        await AssertOutboxColumnsAsync(catalog, cancellationToken);
        await AssertOutboxPendingIndexAsync(catalog, cancellationToken);
        (await catalog.ListPublicTablesAsync(cancellationToken))
            .Except(["__EFMigrationsHistory"])
            .ShouldBe(ApplicationTables, ignoreOrder: true);
        (await ReadLegacyMovieCountAsync(context, cancellationToken)).ShouldBe(1);
        context.Database.HasPendingModelChanges().ShouldBeFalse();

        await migrator.MigrateAsync("20261022101541_AddMovieFeatures", cancellationToken);
        context.ChangeTracker.Clear();

        (await catalog.ListPublicTablesAsync(cancellationToken)).ShouldNotContain("outbox_messages");
        (await ReadLegacyMovieCountAsync(context, cancellationToken)).ShouldBe(1);
        (await catalog.ListTableIndexesAsync("outbox_messages", cancellationToken)).ShouldBeEmpty();

        await migrator.MigrateAsync(null, cancellationToken);
        context.ChangeTracker.Clear();

        (await catalog.ListPublicTablesAsync(cancellationToken)).ShouldContain("outbox_messages");
        (await context.OutboxMessages.CountAsync(cancellationToken)).ShouldBe(0);
        (await ReadLegacyMovieCountAsync(context, cancellationToken)).ShouldBe(1);
        context.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    [Fact]
    public async Task MigrateAsync_EmptyDatabase_MatchesPostgresCatalogContract()
    {
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(TestContext.Current.CancellationToken);
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        var catalog = new PostgresCatalog(context);
        var cancellationToken = TestContext.Current.CancellationToken;

        await AssertPublicTablesAsync(catalog, cancellationToken);
        await AssertColumnsAreSnakeCaseAsync(catalog, cancellationToken);
        await AssertMoviesNullabilityAsync(catalog, cancellationToken);
        await AssertJoinPrimaryKeysAsync(catalog, cancellationToken);
        await AssertMovieIndexDefinitionsAsync(catalog, cancellationToken);
    }

    private static Task InsertLegacyMovieAsync(
        LamuFlixDbContext context,
        LegacyMovie movie,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlRawAsync(
            "INSERT INTO movies (title, library_path, format, status, enrichment_attempts, is_in_watchlist) VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
            [movie.Title, movie.LibraryPath, movie.Format, movie.Status, 0, false],
            cancellationToken);

    private static Task<int> ReadLegacyMovieCountAsync(
        LamuFlixDbContext context,
        CancellationToken cancellationToken) =>
        context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM movies")
            .SingleAsync(cancellationToken);

    private static Task<List<string>> ReadLegacyMovieTitlesAsync(
        LamuFlixDbContext context,
        CancellationToken cancellationToken) =>
        context.Database
            .SqlQueryRaw<string>("SELECT title AS \"Value\" FROM movies ORDER BY title")
            .ToListAsync(cancellationToken);

    private static async Task AssertFeedbackColumnsAsync(
        PostgresCatalog catalog,
        CancellationToken cancellationToken)
    {
        var columns = await catalog.ListMovieFeedbackColumnsAsync(cancellationToken);
        columns.Count.ShouldBe(2);
        columns.Select(column => column.Name).ShouldBe(["is_not_interested", "is_watched"], ignoreOrder: true);
        foreach (var column in columns)
        {
            column.IsNullable.ShouldBeFalse($"Column movies.{column.Name} must be NOT NULL.");
            column.DefaultValue.ShouldBe("false", $"Column movies.{column.Name} must default to false.");
        }
    }

    private sealed record LegacyMovie(string Title, string LibraryPath, string Format, int Status);

    private static async Task AssertPublicTablesAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        var publicTables = await catalog.ListPublicTablesAsync(cancellationToken);
        publicTables.ShouldContain("__EFMigrationsHistory");
        publicTables.Except(["__EFMigrationsHistory"]).ShouldBe(ApplicationTables, ignoreOrder: true);
    }

    private static async Task AssertColumnsAreSnakeCaseAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        foreach (var table in ApplicationTables)
        {
            await AssertTableColumnsAreSnakeCaseAsync(catalog, table, cancellationToken);
        }
    }

    private static async Task AssertTableColumnsAreSnakeCaseAsync(PostgresCatalog catalog, string table, CancellationToken cancellationToken)
    {
        var columns = await catalog.ListColumnsAsync(table, cancellationToken);
        columns.ShouldNotBeEmpty();
        foreach (var column in columns)
        {
            SnakeCase.IsMatch(column).ShouldBeTrue($"Column {table}.{column} must be snake_case.");
        }
    }

    private static async Task AssertMoviesNullabilityAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        var nullability = await catalog.ListMoviesNullabilityAsync(cancellationToken);
        foreach (var column in NullableMoviesColumns)
        {
            nullability[column].ShouldBeTrue($"Column movies.{column} must be nullable.");
        }

        foreach (var (column, isNullable) in nullability)
        {
            AssertRequiredMoviesColumn(column, isNullable);
        }
    }

    private static void AssertRequiredMoviesColumn(string column, bool isNullable)
    {
        if (NullableMoviesColumns.Contains(column, StringComparer.Ordinal))
        {
            return;
        }

        isNullable.ShouldBeFalse($"Column movies.{column} must be NOT NULL.");
    }

    private static async Task AssertJoinPrimaryKeysAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        (await catalog.ListPrimaryKeyColumnsAsync("movie_actors", cancellationToken))
            .ShouldBe(["actor_id", "movie_id"], ignoreOrder: true);
        (await catalog.ListPrimaryKeyColumnsAsync("movie_directors", cancellationToken))
            .ShouldBe(["director_id", "movie_id"], ignoreOrder: true);
        (await catalog.ListPrimaryKeyColumnsAsync("movie_genres", cancellationToken))
            .ShouldBe(["genre_id", "movie_id"], ignoreOrder: true);
        (await catalog.ListPrimaryKeyColumnsAsync("movie_writers", cancellationToken))
            .ShouldBe(["writer_id", "movie_id"], ignoreOrder: true);
    }

    private static async Task AssertMovieIndexDefinitionsAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        var indexes = await catalog.ListMovieIndexesAsync(cancellationToken);
        AssertTitleIndex(indexes);
        AssertReleaseYearIndex(indexes);
        AssertStatusIndex(indexes);
        AssertLibraryPathIndex(indexes);
        AssertImdbIdIndex(indexes);
    }

    private static void AssertTitleIndex(List<string> indexes) =>
        indexes.ShouldContain(def => IsTitleIndex(def));

    private static bool IsTitleIndex(string def) =>
        def.Contains("title", StringComparison.Ordinal)
        && !def.Contains("UNIQUE", StringComparison.Ordinal);

    private static void AssertReleaseYearIndex(List<string> indexes) =>
        indexes.ShouldContain(def => def.Contains("release_year", StringComparison.Ordinal));

    private static void AssertStatusIndex(List<string> indexes) =>
        indexes.ShouldContain(def => IsStatusIndex(def));

    private static bool IsStatusIndex(string def) =>
        def.Contains("status", StringComparison.Ordinal)
        && !def.Contains("imdb", StringComparison.Ordinal);

    private static void AssertLibraryPathIndex(List<string> indexes) =>
        indexes.ShouldContain(def => IsLibraryPathIndex(def));

    private static bool IsLibraryPathIndex(string def) =>
        def.Contains("library_path", StringComparison.Ordinal)
        && def.Contains("UNIQUE", StringComparison.Ordinal);

    private static void AssertImdbIdIndex(List<string> indexes) =>
        indexes.ShouldContain(def => IsImdbIdIndex(def));

    private static async Task AssertPlaybackEventsColumnsAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        var columns = await catalog.ListColumnDetailsAsync("playback_events", cancellationToken);
        columns.ShouldContain(("id", "integer", false, true));
        columns.ShouldContain(("movie_id", "integer", false, false));
        columns.ShouldContain(("played_at", "timestamp with time zone", false, false));
        columns.Count.ShouldBe(3);
    }

    private static async Task AssertPlaybackEventsForeignKeyAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        var foreignKeys = await catalog.ListForeignKeysAsync("playback_events", cancellationToken);
        foreignKeys.ShouldContain(("movie_id", "movies", "id", "CASCADE"));
    }

    private static async Task AssertPlaybackEventsIndexAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        var indexes = await catalog.ListTableIndexesAsync("playback_events", cancellationToken);
        indexes.ShouldContain(def =>
            def.Contains("movie_id", StringComparison.Ordinal)
            && !def.Contains("UNIQUE", StringComparison.Ordinal));
    }

    private static bool IsImdbIdIndex(string def) =>
        def.Contains("imdb_id", StringComparison.Ordinal)
        && def.Contains("UNIQUE", StringComparison.Ordinal)
        && def.Contains("IS NOT NULL", StringComparison.Ordinal);

    private static async Task AssertMovieFeaturesColumnsAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        var columns = await catalog.ListColumnFormatTypesAsync("movie_features", cancellationToken);
        columns.ShouldBe(
        [
            ("movie_id", "integer"),
            ("vector", "vector(384)"),
            ("computed_at", "timestamp with time zone")
        ]);
        var details = await catalog.ListColumnDetailsAsync("movie_features", cancellationToken);
        details.Count.ShouldBe(3);
        foreach (var column in details)
        {
            column.IsNullable.ShouldBeFalse($"Column movie_features.{column.Column} must be NOT NULL.");
        }
    }

    private static async Task AssertMovieFeaturesConstraintsAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        (await catalog.ListPrimaryKeyColumnsAsync("movie_features", cancellationToken))
            .ShouldBe(["movie_id"], ignoreOrder: true);
        (await catalog.ListForeignKeysAsync("movie_features", cancellationToken))
            .ShouldContain(("movie_id", "movies", "id", "CASCADE"));
    }

    private static async Task AssertOutboxColumnsAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        var columns = await catalog.ListColumnFormatTypesAsync("outbox_messages", cancellationToken);
        columns.ShouldBe(
        [
            ("id", "bigint"),
            ("payload", "bytea"),
            ("created_at", "timestamp with time zone"),
            ("processed_at", "timestamp with time zone")
        ]);
        var details = await catalog.ListColumnDetailsAsync("outbox_messages", cancellationToken);
        details.Count.ShouldBe(4);
        details.ShouldContain(("id", "bigint", false, true));
        details.ShouldContain(("payload", "bytea", false, false));
        details.ShouldContain(("created_at", "timestamp with time zone", false, false));
        details.ShouldContain(("processed_at", "timestamp with time zone", true, false));
        (await catalog.ListPrimaryKeyColumnsAsync("outbox_messages", cancellationToken))
            .ShouldBe(["id"]);
    }

    private static async Task AssertOutboxPendingIndexAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        var indexes = await catalog.ListTableIndexesAsync("outbox_messages", cancellationToken);
        indexes.ShouldContain(def =>
            def.Contains("created_at", StringComparison.Ordinal)
            && def.Contains("processed_at IS NULL", StringComparison.Ordinal)
            && def.Contains("WHERE", StringComparison.Ordinal)
            && !def.Contains("UNIQUE", StringComparison.Ordinal));
    }

    private static async Task AssertMovieFeaturesIndexAsync(PostgresCatalog catalog, CancellationToken cancellationToken)
    {
        var indexes = await catalog.ListTableIndexesAsync("movie_features", cancellationToken);
        indexes.ShouldContain(def =>
            def.Contains("IX_movie_features_vector", StringComparison.Ordinal)
            && def.Contains("USING hnsw", StringComparison.Ordinal)
            && def.Contains("vector_cosine_ops", StringComparison.Ordinal));
        indexes.Count(def => def.Contains("IX_movie_features_vector", StringComparison.Ordinal)).ShouldBe(1);
    }

    private sealed class PostgresCatalog(LamuFlixDbContext context)
    {
        public Task<List<string>> ListPublicTablesAsync(CancellationToken cancellationToken) =>
            QueryStringsAsync(
                """
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
                """,
                cancellationToken);

        public Task<List<string>> ListPostgresExtensionsAsync(CancellationToken cancellationToken) =>
            QueryStringsAsync("SELECT extname FROM pg_extension", cancellationToken);

        public Task<List<string>> ListColumnsAsync(string table, CancellationToken cancellationToken) =>
            QueryStringsAsync(
                $"""
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = '{table}'
                """,
                cancellationToken);

        public async Task<Dictionary<string, bool>> ListMoviesNullabilityAsync(CancellationToken cancellationToken)
        {
            var rows = await QueryPairsAsync(
                """
                SELECT column_name, is_nullable
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'movies'
                """,
                cancellationToken);
            return rows.ToDictionary(
                row => row.Left,
                row => row.Right == "YES",
                StringComparer.Ordinal);
        }

        public async Task<List<(string Name, bool IsNullable, string? DefaultValue)>> ListMovieFeedbackColumnsAsync(
            CancellationToken cancellationToken)
        {
            var values = new List<(string Name, bool IsNullable, string? DefaultValue)>();
            await using var command = await CreateCommandAsync(
                """
                SELECT column_name, is_nullable, column_default
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'movies'
                  AND column_name IN ('is_watched', 'is_not_interested')
                """,
                cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                values.Add((
                    reader.GetString(0),
                    reader.GetString(1) == "YES",
                    reader.IsDBNull(2) ? null : reader.GetString(2)));
            }

            return values;
        }

        public async Task<List<LegacyMovie>> ListLegacyMoviesAsync(CancellationToken cancellationToken)
        {
            var values = new List<LegacyMovie>();
            await using var command = await CreateCommandAsync(
                """
                SELECT title, library_path, format, status
                FROM movies
                ORDER BY title
                """,
                cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                values.Add(new LegacyMovie(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetInt32(3)));
            }

            return values;
        }

        public Task<List<string>> ListPrimaryKeyColumnsAsync(string table, CancellationToken cancellationToken) =>
            QueryStringsAsync(
                $"""
                SELECT kcu.column_name
                FROM information_schema.table_constraints AS tc
                JOIN information_schema.key_column_usage AS kcu
                  ON tc.constraint_name = kcu.constraint_name
                 AND tc.table_schema = kcu.table_schema
                WHERE tc.constraint_type = 'PRIMARY KEY'
                  AND tc.table_schema = 'public'
                  AND tc.table_name = '{table}'
                ORDER BY kcu.ordinal_position
                """,
                cancellationToken);

        public Task<List<string>> ListMovieIndexesAsync(CancellationToken cancellationToken) =>
            ListTableIndexesAsync("movies", cancellationToken);

        public Task<List<string>> ListTableIndexesAsync(string table, CancellationToken cancellationToken) =>
            QueryStringsAsync(
                $"""
                SELECT indexdef
                FROM pg_indexes
                WHERE schemaname = 'public' AND tablename = '{table}'
                """,
                cancellationToken);

        public async Task<List<(string Column, string DataType, bool IsNullable, bool IsIdentity)>>
            ListColumnDetailsAsync(string table, CancellationToken cancellationToken)
        {
            var values = new List<(string Column, string DataType, bool IsNullable, bool IsIdentity)>();
            await using var command = await CreateCommandAsync(
                $"""
                SELECT column_name, data_type, is_nullable, is_identity
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = '{table}'
                """,
                cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                values.Add((
                    reader.GetString(0),
                    reader.GetString(1),
                    string.Equals(reader.GetString(2), "YES", StringComparison.Ordinal),
                    string.Equals(reader.GetString(3), "YES", StringComparison.Ordinal)));
            }

            return values;
        }

        public async Task<List<(string Column, string FormatType)>> ListColumnFormatTypesAsync(
            string table,
            CancellationToken cancellationToken)
        {
            var values = new List<(string Column, string FormatType)>();
            await using var command = await CreateCommandAsync(
                $"""
                SELECT attribute.attname, format_type(attribute.atttypid, attribute.atttypmod)
                FROM pg_attribute AS attribute
                JOIN pg_class AS relation ON relation.oid = attribute.attrelid
                JOIN pg_namespace AS space ON space.oid = relation.relnamespace
                WHERE space.nspname = 'public'
                  AND relation.relname = '{table}'
                  AND attribute.attnum > 0
                  AND NOT attribute.attisdropped
                ORDER BY attribute.attnum
                """,
                cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                values.Add((reader.GetString(0), reader.GetString(1)));
            }

            return values;
        }

        public async Task<List<(string Column, string ReferencedTable, string ReferencedColumn, string DeleteRule)>>
            ListForeignKeysAsync(string table, CancellationToken cancellationToken)
        {
            var values = new List<(string Column, string ReferencedTable, string ReferencedColumn, string DeleteRule)>();
            await using var command = await CreateCommandAsync(
                $"""
                SELECT kcu.column_name, ccu.table_name, ccu.column_name, rc.delete_rule
                FROM information_schema.referential_constraints AS rc
                JOIN information_schema.key_column_usage AS kcu
                  ON rc.constraint_name = kcu.constraint_name
                 AND rc.constraint_schema = kcu.constraint_schema
                JOIN information_schema.constraint_column_usage AS ccu
                  ON rc.unique_constraint_name = ccu.constraint_name
                 AND rc.unique_constraint_schema = ccu.constraint_schema
                WHERE kcu.table_schema = 'public' AND kcu.table_name = '{table}'
                ORDER BY kcu.ordinal_position
                """,
                cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                values.Add((
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3)));
            }

            return values;
        }

        private async Task<List<string>> QueryStringsAsync(string sql, CancellationToken cancellationToken)
        {
            var values = new List<string>();
            await using var command = await CreateCommandAsync(sql, cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                values.Add(reader.GetString(0));
            }

            return values;
        }

        private async Task<List<(string Left, string Right)>> QueryPairsAsync(
            string sql,
            CancellationToken cancellationToken)
        {
            var values = new List<(string Left, string Right)>();
            await using var command = await CreateCommandAsync(sql, cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                values.Add((reader.GetString(0), reader.GetString(1)));
            }

            return values;
        }

        private async Task<DbCommand> CreateCommandAsync(string sql, CancellationToken cancellationToken)
        {
            var connection = context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            var command = connection.CreateCommand();
            command.CommandText = sql;
            return command;
        }
    }
}
