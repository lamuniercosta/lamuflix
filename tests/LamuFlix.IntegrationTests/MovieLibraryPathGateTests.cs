using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class MovieLibraryPathGateTests(PostgresFixture fixture)
{
    private const string ActiveRoot = "C:/library";
    private const string LongSTwinPath = "C:/library/stest/movie.mkv";
    private const string LongSPath = "C:/library/\u017Ftest/movie.mkv";
    private const string GarayUpperPath = "C:/library/\U00010D50movie/movie.mkv";
    private const string GarayLowerPath = "C:/library/\U00010D70movie/movie.mkv";

    [Fact]
    public async Task Migrate_AdditiveOnly_LeavesSchemaNotReady()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);

        // act
        await context.Database.MigrateAsync(cancellationToken);

        // assert
        var column = await ReadColumnStateAsync(context, cancellationToken);
        column.IsNullable.ShouldBeTrue();
        column.Collation.ShouldBe("default");
        (await ReadKeyIndexDefinitionAsync(context, cancellationToken)).ShouldBeNull();
        (await ReadMovieIndexDefinitionsAsync(context, cancellationToken))
            .ShouldContain(definition => definition.Contains("library_path", StringComparison.Ordinal)
                && definition.Contains("UNIQUE", StringComparison.Ordinal));
        context.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    [Fact]
    public async Task Migrate_AppliedMigrationList_ContainsOnlyTheAdditiveLibraryPathKeyMigration()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);

        // act
        await context.Database.MigrateAsync(cancellationToken);

        // assert
        var migrations = context.Database.GetMigrations().ToArray();
        migrations[0].ShouldEndWith("_Initial");
        migrations[^1].ShouldEndWith("_AddLibraryPathKey");
        migrations.ShouldNotContain(name => name.Contains("LibraryPathKeyActivation", StringComparison.Ordinal));
        migrations.Length.ShouldBe(16);
    }

    [Fact]
    public async Task Run_LegacyAbsoluteRows_BackfillsCanonicalRelativePathsKeysAndActivatedSchema()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var windowsId = await InsertLegacyRowAsync(context, "Windows", "C:/library/Heat (1995)/heat.mkv", cancellationToken);
        var dockerId = await InsertLegacyRowAsync(context, "Docker", "/media/library/Alien/alien.mkv", cancellationToken);
        var nestedId = await InsertLegacyRowAsync(context, "Nested", "C:\\library\\Nested\\Deep\\deep.mkv", cancellationToken);
        var rootId = await InsertLegacyRowAsync(context, "Root", "C:/library", cancellationToken);

        // act
        await CreateGate(context).RunAsync(cancellationToken);

        // assert
        var rows = await ReadRowsAsync(context, cancellationToken);
        rows.Single(row => row.Id == windowsId.Value).ShouldBe(
            new Row(windowsId.Value, "Heat (1995)/heat.mkv", "HEAT (1995)/HEAT.MKV"));
        rows.Single(row => row.Id == dockerId.Value).ShouldBe(
            new Row(dockerId.Value, "Alien/alien.mkv", "ALIEN/ALIEN.MKV"));
        rows.Single(row => row.Id == nestedId.Value).ShouldBe(
            new Row(nestedId.Value, "Nested/Deep/deep.mkv", "NESTED/DEEP/DEEP.MKV"));
        rows.Single(row => row.Id == rootId.Value).ShouldBe(new Row(rootId.Value, ".", "."));
        var column = await ReadColumnStateAsync(context, cancellationToken);
        column.IsNullable.ShouldBeFalse();
        column.Collation.ShouldBe("C");
        var index = await ReadKeyIndexDefinitionAsync(context, cancellationToken);
        index.ShouldNotBeNull();
        index.ShouldContain("UNIQUE");
        index.ShouldContain("library_path_key");
    }

    [Fact]
    public async Task Run_OfflineMedia_MigratesWithoutFilesOnDisk()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var id = await InsertLegacyRowAsync(context, "Offline", "D:/elsewhere/Absent/absent.mkv", cancellationToken);
        var options = new LibraryOptions { RootPath = ActiveRoot, LegacyRootPaths = ["D:/elsewhere"] };

        // act
        await CreateGate(context, options).RunAsync(cancellationToken);

        // assert
        var row = (await ReadRowsAsync(context, cancellationToken)).Single(item => item.Id == id.Value);
        row.Path.ShouldBe("Absent/absent.mkv");
        row.Key.ShouldBe("ABSENT/ABSENT.MKV");
    }

    [Fact]
    public async Task Run_CaseOnlyCollision_FailsClosedWithBothMovieIdsAndNoIndexOrWrites()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var first = await InsertLegacyRowAsync(context, "Upper", "C:/library/Heat/heat.mkv", cancellationToken);
        var second = await InsertLegacyRowAsync(context, "Lower", "C:/library/heat/heat.mkv", cancellationToken);

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(
            () => CreateGate(context).RunAsync(cancellationToken));

        // assert
        failure.Message.ShouldContain(first.Value.ToString(CultureInfo.InvariantCulture));
        failure.Message.ShouldContain(second.Value.ToString(CultureInfo.InvariantCulture));
        failure.Message.ShouldContain("preflight failed closed");
        var rows = await ReadRowsAsync(context, cancellationToken);
        rows.ShouldAllBe(row => row.Key == null);
        rows.Single(row => row.Id == first.Value).Path.ShouldBe("C:/library/Heat/heat.mkv");
        (await ReadKeyIndexDefinitionAsync(context, cancellationToken)).ShouldBeNull();
        (await ReadColumnStateAsync(context, cancellationToken)).IsNullable.ShouldBeTrue();
    }

    [Fact]
    public async Task Run_LongSCollision_BlocksOnWholeStringUpperInvariantKey()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var longS = await InsertLegacyRowAsync(context, "Long S", LongSPath, cancellationToken);
        var plainS = await InsertLegacyRowAsync(context, "Plain S", LongSTwinPath, cancellationToken);
        "\u017F".ToUpperInvariant().ShouldBe("S");

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(
            () => CreateGate(context).RunAsync(cancellationToken));

        // assert
        failure.Message.ShouldContain(longS.Value.ToString(CultureInfo.InvariantCulture));
        failure.Message.ShouldContain(plainS.Value.ToString(CultureInfo.InvariantCulture));
        (await ReadRowsAsync(context, cancellationToken)).ShouldAllBe(row => row.Key == null);
    }

    [Fact]
    public async Task Run_SingleLongSPath_KeysToTheSameUpperInvariantValueAsPlainS()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var id = await InsertLegacyRowAsync(context, "Long S only", LongSPath, cancellationToken);

        // act
        await CreateGate(context).RunAsync(cancellationToken);

        // assert
        var row = (await ReadRowsAsync(context, cancellationToken)).Single(item => item.Id == id.Value);
        row.Path.ShouldBe("\u017Ftest/movie.mkv");
        row.Key.ShouldBe("STEST/MOVIE.MKV");
    }

    [Fact]
    public async Task Run_GarayCasePairs_RemainDistinctUnderOrdinalUpperKeys()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var upper = await InsertLegacyRowAsync(context, "Garay upper", GarayUpperPath, cancellationToken);
        var lower = await InsertLegacyRowAsync(context, "Garay lower", GarayLowerPath, cancellationToken);

        // act
        await CreateGate(context).RunAsync(cancellationToken);

        // assert
        var rows = await ReadRowsAsync(context, cancellationToken);
        var upperRow = rows.Single(row => row.Id == upper.Value);
        var lowerRow = rows.Single(row => row.Id == lower.Value);
        upperRow.Key.ShouldNotBe(lowerRow.Key);
        string.Equals("\U00010D50", "\U00010D70", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
        string.Equals(
            "\U00010D50".ToUpperInvariant(),
            "\U00010D70".ToUpperInvariant(),
            StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    public async Task Run_UnmappedRoot_FailsClosedWithActionableMovieId()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var id = await InsertLegacyRowAsync(context, "Unmapped", "E:/nowhere/Movie/movie.mkv", cancellationToken);

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(
            () => CreateGate(context).RunAsync(cancellationToken));

        // assert
        failure.Message.ShouldContain(id.Value.ToString(CultureInfo.InvariantCulture));
        failure.Message.ShouldContain("matches no active, legacy, or default Docker library root");
        (await ReadRowsAsync(context, cancellationToken)).Single().Key.ShouldBeNull();
    }

    [Fact]
    public async Task Run_OverlappingRoots_MapsAmbiguouslyAndFailsClosed()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var id = await InsertLegacyRowAsync(context, "Ambiguous", "C:/library/sub/Movie/movie.mkv", cancellationToken);
        var options = new LibraryOptions { RootPath = ActiveRoot, LegacyRootPaths = ["C:/library/sub"] };

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(
            () => CreateGate(context, options).RunAsync(cancellationToken));

        // assert
        failure.Message.ShouldContain(id.Value.ToString(CultureInfo.InvariantCulture));
        failure.Message.ShouldContain("maps ambiguously");
    }

    [Fact]
    public async Task Run_MalformedRelativeRow_FailsClosed()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var id = await InsertLegacyRowAsync(context, "Malformed", "  ", cancellationToken);

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(
            () => CreateGate(context).RunAsync(cancellationToken));

        // assert
        failure.Message.ShouldContain(id.Value.ToString(CultureInfo.InvariantCulture));
        failure.Message.ShouldContain("malformed library path");
    }

    [Fact]
    public async Task Run_AlreadyCanonicalRowsWithMissingKeys_PopulatesKeysAndActivates()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var id = await InsertLegacyRowAsync(context, "Canonical", "Heat (1995)/heat.mkv", cancellationToken);

        // act
        await CreateGate(context).RunAsync(cancellationToken);

        // assert
        var row = (await ReadRowsAsync(context, cancellationToken)).Single(item => item.Id == id.Value);
        row.Path.ShouldBe("Heat (1995)/heat.mkv");
        row.Key.ShouldBe("HEAT (1995)/HEAT.MKV");
        (await ReadColumnStateAsync(context, cancellationToken)).IsNullable.ShouldBeFalse();
    }

    [Fact]
    public async Task Run_PreservesIdsAndPreExistingBusinessColumns()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var id = await InsertLegacyRowAsync(context, "Survivor", "C:/library/Survivor/survivor.mkv", cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE movies SET is_watched = true, review = 1, enrichment_attempts = 5 WHERE id = {id.Value}",
            cancellationToken);

        // act
        await CreateGate(context).RunAsync(cancellationToken);

        // assert
        var survivor = await context.Database
            .SqlQuery<string>(
                $"SELECT (id::text || '|' || title || '|' || review::text || '|' || is_watched::text || '|' || enrichment_attempts::text) AS \"Value\" FROM movies WHERE id = {id.Value}")
            .SingleAsync(cancellationToken);
        survivor.ShouldBe($"{id.Value.ToString(CultureInfo.InvariantCulture)}|Survivor|1|true|5");
    }

    [Fact]
    public async Task Run_SecondRunOnActivatedConsistentTable_WritesNothing()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var id = await InsertLegacyRowAsync(context, "Idempotent", "C:/library/Idem/idem.mkv", cancellationToken);
        await CreateGate(context).RunAsync(cancellationToken);
        var before = await ReadRowsAsync(context, cancellationToken);
        var xminBefore = await ReadXminAsync(context, id, cancellationToken);

        // act
        await CreateGate(context).RunAsync(cancellationToken);

        // assert
        (await ReadRowsAsync(context, cancellationToken)).ShouldBe(before);
        (await ReadXminAsync(context, id, cancellationToken)).ShouldBe(xminBefore);
    }

    [Fact]
    public async Task Run_ActivationFailureAfterRealWrites_RollsTheWholeRewriteBack()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var id = await InsertLegacyRowAsync(context, "Rollback", "C:/library/Roll/rb.mkv", cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "CREATE INDEX \"IX_movies_library_path_key\" ON movies (title)",
            cancellationToken);

        // act
        await Should.ThrowAsync<Exception>(() => CreateGate(context).RunAsync(cancellationToken));

        // assert
        var row = (await ReadRowsAsync(context, cancellationToken)).Single(item => item.Id == id.Value);
        row.Path.ShouldBe("C:/library/Roll/rb.mkv");
        row.Key.ShouldBeNull();
        (await ReadColumnStateAsync(context, cancellationToken)).IsNullable.ShouldBeTrue();
    }

    [Fact]
    public async Task Run_InterruptedThenRerunAfterRepair_CommitsCleanly()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var first = await InsertLegacyRowAsync(context, "First", "C:/library/Dup/one.mkv", cancellationToken);
        var second = await InsertLegacyRowAsync(context, "Second", "C:/library/dup/one.mkv", cancellationToken);
        await Should.ThrowAsync<InvalidOperationException>(() => CreateGate(context).RunAsync(cancellationToken));

        // act
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE movies SET library_path = 'C:/library/other/two.mkv' WHERE id = {second.Value}",
            cancellationToken);
        await CreateGate(context).RunAsync(cancellationToken);

        // assert
        var rows = await ReadRowsAsync(context, cancellationToken);
        rows.Single(row => row.Id == first.Value).Path.ShouldBe("Dup/one.mkv");
        rows.Single(row => row.Id == second.Value).Path.ShouldBe("other/two.mkv");
        (await ReadColumnStateAsync(context, cancellationToken)).IsNullable.ShouldBeFalse();
    }

    [Fact]
    public async Task Run_WhileAnotherSessionHoldsTheTable_FailsClosedOnTheAccessExclusiveLock()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        await InsertLegacyRowAsync(context, "Locked", "C:/library/Locked/locked.mkv", cancellationToken);
        var connectionString = fixture.FullConnectionString(context);
        await using var holder = LamuFlixDbContextFactory.OpenContext(connectionString);
        await using var holderTransaction = await holder.Database.BeginTransactionAsync(cancellationToken);
        await holder.Database.ExecuteSqlRawAsync("LOCK TABLE movies IN SHARE MODE", cancellationToken);

        // act
        Exception? failure;
        await using (var gateContext = LamuFlixDbContextFactory.OpenContext(connectionString))
        {
            await gateContext.Database.OpenConnectionAsync(cancellationToken);
            await gateContext.Database.ExecuteSqlRawAsync("SET lock_timeout = '1500ms'", cancellationToken);
            failure = await Record.ExceptionAsync(() => CreateGate(gateContext).RunAsync(cancellationToken));
            await gateContext.Database.CloseConnectionAsync();
        }

        await holderTransaction.RollbackAsync(cancellationToken);

        // assert
        var failed = failure.ShouldNotBeNull();
        Describe(failed).ShouldContain("55P03");
        (await ReadRowsAsync(context, cancellationToken)).Single().Key.ShouldBeNull();
    }

    private static string Describe(Exception exception)
    {
        var builder = new System.Text.StringBuilder();
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            builder.Append(' ').Append(current.GetType().Name).Append(": ").Append(current.Message);
        }

        return builder.ToString();
    }

    [Fact]
    public async Task Run_TwoConcurrentGates_SerializeAndBothSucceed()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var id = await InsertLegacyRowAsync(context, "Concurrent", "C:/library/Conc/conc.mkv", cancellationToken);
        var connectionString = fixture.FullConnectionString(context);

        // act
        await Task.WhenAll(
            RunGateOnFreshContextAsync(connectionString, cancellationToken),
            RunGateOnFreshContextAsync(connectionString, cancellationToken));

        // assert
        var row = (await ReadRowsAsync(context, cancellationToken)).Single(item => item.Id == id.Value);
        row.Path.ShouldBe("Conc/conc.mkv");
        row.Key.ShouldBe("CONC/CONC.MKV");
        (await ReadColumnStateAsync(context, cancellationToken)).IsNullable.ShouldBeFalse();
    }

    [Fact]
    public async Task Run_WithMigrationsDisabled_VerifiesTheActivatedSchemaWithoutWriting()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var id = await InsertLegacyRowAsync(context, "Verify", "C:/library/Verify/verify.mkv", cancellationToken);
        await CreateGate(context).RunAsync(cancellationToken);
        var before = await ReadRowsAsync(context, cancellationToken);

        // act
        await CreateGate(context, applyMigrations: false).RunAsync(cancellationToken);

        // assert
        (await ReadRowsAsync(context, cancellationToken)).ShouldBe(before);
        before.Single().Id.ShouldBe(id.Value);
    }

    [Fact]
    public async Task Run_WithMigrationsDisabledOnSchemaNotReady_FailsClosedWithoutMigrating()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        await InsertLegacyRowAsync(context, "Verify", "C:/library/Verify/verify.mkv", cancellationToken);

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(
            () => CreateGate(context, applyMigrations: false).RunAsync(cancellationToken));

        // assert
        failure.Message.ShouldContain("Schema not ready");
        (await ReadColumnStateAsync(context, cancellationToken)).IsNullable.ShouldBeTrue();
        (await ReadRowsAsync(context, cancellationToken)).Single().Key.ShouldBeNull();
    }

    [Fact]
    public async Task Run_WithMigrationsDisabledAndKeyDrift_FailsClosed()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var id = await InsertLegacyRowAsync(context, "Drift", "C:/library/Drift/drift.mkv", cancellationToken);
        await CreateGate(context).RunAsync(cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE movies SET library_path_key = 'TAMPERED' WHERE id = {id.Value}",
            cancellationToken);

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(
            () => CreateGate(context, applyMigrations: false).RunAsync(cancellationToken));

        // assert
        failure.Message.ShouldContain("Library path key drift");
    }

    [Fact]
    public async Task Run_DefaultDockerAlias_MapsLegacyRowsWhenTheActiveRootIsTheContainerPath()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var id = await InsertLegacyRowAsync(context, "Docker root", "C:/library/Heat/heat.mkv", cancellationToken);
        var options = new LibraryOptions { RootPath = "/media/library", LegacyRootPaths = ["C:/library"] };

        // act
        await CreateGate(context, options).RunAsync(cancellationToken);

        // assert
        var row = (await ReadRowsAsync(context, cancellationToken)).Single(item => item.Id == id.Value);
        row.Path.ShouldBe("Heat/heat.mkv");
        row.Key.ShouldBe("HEAT/HEAT.MKV");
    }

    [Fact]
    public async Task Run_AutomaticDockerAliasConflictingWithTheActiveRoot_FailsClosed()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var id = await InsertLegacyRowAsync(context, "Conflict", "/media/library/library/Movie/movie.mkv", cancellationToken);
        var options = new LibraryOptions { RootPath = "/media/library/library" };

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(
            () => CreateGate(context, options).RunAsync(cancellationToken));

        // assert
        failure.Message.ShouldContain(id.Value.ToString(CultureInfo.InvariantCulture));
        failure.Message.ShouldContain("maps ambiguously");
    }

    [Fact]
    public async Task Run_NonAbsoluteActiveRoot_FailsClosedBeforeAnyDatabaseWork()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var options = new LibraryOptions { RootPath = "relative/library" };

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(
            () => CreateGate(context, options).RunAsync(cancellationToken));

        // assert
        failure.Message.ShouldContain("Library:RootPath");
    }

    [Fact]
    public async Task Run_MissingAdditiveColumn_FailsClosedAsSchemaNotReady()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateEmptyDatabaseContextAsync(cancellationToken);
        await context.Database.MigrateAsync("20261106115042_AddExplicitMovieReviews", cancellationToken);

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(
            () => CreateGate(context).RunAsync(cancellationToken));

        // assert
        failure.Message.ShouldContain("Apply the additive AddLibraryPathKey migration");
    }

    private static Task RunGateOnFreshContextAsync(string connectionString, CancellationToken cancellationToken) =>
        RunGateCoreAsync(LamuFlixDbContextFactory.OpenContext(connectionString), cancellationToken);

    private static async Task RunGateCoreAsync(LamuFlixDbContext context, CancellationToken cancellationToken)
    {
        await using (context)
        {
            await CreateGate(context).RunAsync(cancellationToken);
        }
    }

    private static MovieLibraryPathMigrationGate CreateGate(
        LamuFlixDbContext context,
        LibraryOptions? options = null,
        bool applyMigrations = true) =>
        new(
            context,
            Options.Create(options ?? new LibraryOptions { RootPath = ActiveRoot }),
            Options.Create(new DatabaseOptions { ApplyMigrationsOnStartup = applyMigrations }),
            NullLogger<MovieLibraryPathMigrationGate>.Instance);

    private static async Task<MovieId> InsertLegacyRowAsync(
        LamuFlixDbContext context,
        string title,
        string libraryPath,
        CancellationToken cancellationToken)
    {
        var ids = await context.Database
            .SqlQuery<int>(
                $"INSERT INTO movies (title, library_path, format, status, enrichment_attempts, is_in_watchlist) VALUES ({title}, {libraryPath}, 'mkv', 0, 0, false) RETURNING id AS \"Value\"")
            .ToListAsync(cancellationToken);
        return new MovieId(ids.Single());
    }

    private static async Task<List<Row>> ReadRowsAsync(
        LamuFlixDbContext context,
        CancellationToken cancellationToken)
    {
        var values = await context.Database
            .SqlQueryRaw<string>(
                "SELECT (id::text || '|' || library_path || '|' || COALESCE(library_path_key, '<null>')) AS \"Value\" FROM movies ORDER BY id")
            .ToListAsync(cancellationToken);
        return [.. values.Select(static value =>
        {
            var parts = value.Split('|');
            return new Row(
                int.Parse(parts[0], CultureInfo.InvariantCulture),
                parts[1],
                parts[2] == "<null>" ? null : parts[2]);
        })];
    }

    private static async Task<ColumnState> ReadColumnStateAsync(
        LamuFlixDbContext context,
        CancellationToken cancellationToken)
    {
        var values = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT (column_state.is_nullable || '|' || COALESCE((SELECT coll.collname FROM pg_collation AS coll
                    WHERE coll.oid = attribute.attcollation), '')) AS "Value"
                FROM information_schema.columns AS column_state
                JOIN pg_attribute AS attribute
                  ON attribute.attname = column_state.column_name
                 AND attribute.attnum > 0
                 AND NOT attribute.attisdropped
                JOIN pg_class AS relation
                  ON relation.oid = attribute.attrelid AND relation.relname = 'movies'
                WHERE column_state.table_schema = 'public'
                  AND column_state.table_name = 'movies'
                  AND column_state.column_name = 'library_path_key'
                """)
            .ToListAsync(cancellationToken);
        var parts = values.Single().Split('|');
        return new ColumnState(string.Equals(parts[0], "YES", StringComparison.Ordinal), parts[1]);
    }

    private static async Task<string?> ReadKeyIndexDefinitionAsync(
        LamuFlixDbContext context,
        CancellationToken cancellationToken)
    {
        var definitions = await context.Database
            .SqlQueryRaw<string>(
                "SELECT indexdef AS \"Value\" FROM pg_indexes WHERE schemaname = 'public' AND tablename = 'movies' AND indexname = 'IX_movies_library_path_key'")
            .ToListAsync(cancellationToken);
        return definitions.SingleOrDefault();
    }

    private static async Task<List<string>> ReadMovieIndexDefinitionsAsync(
        LamuFlixDbContext context,
        CancellationToken cancellationToken) =>
        await context.Database
            .SqlQueryRaw<string>(
                "SELECT indexdef AS \"Value\" FROM pg_indexes WHERE schemaname = 'public' AND tablename = 'movies'")
            .ToListAsync(cancellationToken);

    private static async Task<string> ReadXminAsync(
        LamuFlixDbContext context,
        MovieId id,
        CancellationToken cancellationToken) =>
        await context.Database
            .SqlQuery<string>($"SELECT xmin::text AS \"Value\" FROM movies WHERE id = {id.Value}")
            .SingleAsync(cancellationToken);

    private sealed record Row(int Id, string Path, string? Key);

    private sealed record ColumnState(bool IsNullable, string Collation);
}




