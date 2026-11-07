using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Persistence;

/// <summary>
/// The single configuration-aware startup barrier for library-relative movie identities: it rewrites
/// stored absolute paths to canonical library-relative values, populates the derived portable
/// uniqueness key, and activates the column collation, NOT NULL constraint and unique key index in
/// one locked transaction, or verifies that activated state without writing when startup migrations
/// are disabled.
/// </summary>
public sealed partial class MovieLibraryPathMigrationGate(
    LamuFlixDbContext db,
    IOptions<LibraryOptions> libraryOptions,
    IOptions<DatabaseOptions> databaseOptions,
    ILogger<MovieLibraryPathMigrationGate> logger)
{
    private const string LockStatement = "LOCK TABLE movies IN ACCESS EXCLUSIVE MODE";
    private const string KeyColumn = "library_path_key";
    private const string KeyIndexName = "IX_movies_library_path_key";
    private const string BinaryCollation = "C";
    private const int ReportedFailureLimit = 20;

    private const string ColumnStateStatement = """
        SELECT is_nullable FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'movies' AND column_name = 'library_path_key'
        """;

    private const string CollationStatement = """
        SELECT COALESCE((SELECT coll.collname FROM pg_collation AS coll
                         WHERE coll.oid = attribute.attcollation), '')
        FROM pg_attribute AS attribute
        JOIN pg_class AS relation ON relation.oid = attribute.attrelid
        JOIN pg_namespace AS space ON space.oid = relation.relnamespace
        WHERE space.nspname = 'public' AND relation.relname = 'movies'
          AND attribute.attname = 'library_path_key' AND attribute.attnum > 0
          AND NOT attribute.attisdropped
        """;

    private const string IndexStatement = """
        SELECT indexdef FROM pg_indexes
        WHERE schemaname = 'public' AND tablename = 'movies'
          AND indexname = 'IX_movies_library_path_key'
          AND indexdef LIKE '%UNIQUE%'
          AND indexdef LIKE '%(library_path_key)%'
        """;

    private const string ApplyCollationStatement =
        "ALTER TABLE movies ALTER COLUMN library_path_key TYPE text COLLATE \"C\"";
    private const string NotNullStatement = "ALTER TABLE movies ALTER COLUMN library_path_key SET NOT NULL";
    private const string UniqueIndexStatement =
        "CREATE UNIQUE INDEX \"IX_movies_library_path_key\" ON movies (library_path_key)";
    private const string RowSelectStatement = "SELECT id, library_path, library_path_key FROM movies ORDER BY id";

    /// <summary>
    /// Runs preflight, backfill, key population, activation and consistency verification inside one
    /// transaction, or verifies the already-activated state when startup migrations are disabled.
    /// Any failure throws before the transaction commits, so startup fails closed.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var rootMap = RootMap.Create(libraryOptions.Value);

        if (!databaseOptions.Value.ApplyMigrationsOnStartup)
        {
            await VerifyOnlyAsync(cancellationToken);
            return;
        }

        await ActivateAsync(rootMap, cancellationToken);
    }

    private async Task VerifyOnlyAsync(CancellationToken cancellationToken)
    {
        var schema = await ReadSchemaAsync(cancellationToken);
        if (!schema.IsActivated)
        {
            throw new InvalidOperationException(
                $"Schema not ready: movies.{KeyColumn} must exist, be NOT NULL, collated {BinaryCollation}, "
                + $"and carry the unique {KeyIndexName} index before startup with startup migrations disabled.");
        }

        var inconsistent = (await ReadRowsAsync(cancellationToken))
            .Count(row => !string.Equals(row.LibraryPathKey, row.LibraryPath.ToUpperInvariant(), StringComparison.Ordinal));
        if (inconsistent > 0)
        {
            throw new InvalidOperationException(
                $"Library path key drift: {inconsistent.ToString(CultureInfo.InvariantCulture)} row(s) carry a "
                + $"movies.{KeyColumn} that is not the whole-string uppercase form of movies.library_path under the "
                + "shipping runtime while startup migrations are disabled.");
        }

        GateLog.VerifiedOnly(logger);
    }

    private async Task ActivateAsync(RootMap rootMap, CancellationToken cancellationToken)
    {
        RequireColumn(await ReadSchemaAsync(cancellationToken));

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync(LockStatement, cancellationToken);

        var schema = RequireColumn(await ReadSchemaAsync(cancellationToken));
        var plan = BuildPlan(await ReadRowsAsync(cancellationToken), rootMap);
        var rewritten = plan.Writes.Count(static write => write is not null);
        if (rewritten == 0 && schema.IsActivated)
        {
            await transaction.CommitAsync(cancellationToken);
            GateLog.AlreadyActivated(logger);
            return;
        }

        await WriteRowsAsync(plan, cancellationToken);
        await EnsureActivatedAsync(schema, cancellationToken);
        var verified = RequireColumn(await ReadSchemaAsync(cancellationToken));
        if (!verified.IsActivated)
        {
            throw new InvalidOperationException(
                $"Activation did not produce the final schema: movies.{KeyColumn} must be NOT NULL, "
                + $"collated {BinaryCollation}, and uniquely indexed by {KeyIndexName}.");
        }

        var mismatched = BuildPlan(await ReadRowsAsync(cancellationToken), rootMap)
            .Writes.Count(static write => write is not null);
        if (mismatched > 0)
        {
            throw new InvalidOperationException(
                $"Library path key drift: {mismatched.ToString(CultureInfo.InvariantCulture)} row(s) did not "
                + "persist their derived path and key.");
        }

        await transaction.CommitAsync(cancellationToken);
        GateLog.Activated(logger, rewritten);
    }

    private static SchemaState RequireColumn(SchemaState schema)
    {
        if (!schema.ColumnExists)
        {
            throw new InvalidOperationException(
                $"Schema not ready: movies.{KeyColumn} is missing. Apply the additive AddLibraryPathKey migration "
                + "before startup so the library path gate can activate it.");
        }

        return schema;
    }

    private async Task WriteRowsAsync(Plan plan, CancellationToken cancellationToken)
    {
        foreach (var write in plan.Writes)
        {
            if (write is null)
            {
                continue;
            }

            await using var command = await CreateCommandAsync(
                "UPDATE movies SET library_path = @path, library_path_key = @key WHERE id = @id",
                cancellationToken);
            AddParameter(command, "@path", write.Path);
            AddParameter(command, "@key", write.Key);
            AddParameter(command, "@id", write.Id);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task EnsureActivatedAsync(SchemaState schema, CancellationToken cancellationToken)
    {
        if (!schema.HasBinaryCollation)
        {
            await db.Database.ExecuteSqlRawAsync(ApplyCollationStatement, cancellationToken);
        }

        if (schema.ColumnIsNullable)
        {
            await db.Database.ExecuteSqlRawAsync(NotNullStatement, cancellationToken);
        }

        if (!schema.HasUniqueIndex)
        {
            await db.Database.ExecuteSqlRawAsync(UniqueIndexStatement, cancellationToken);
        }
    }

    private async Task<SchemaState> ReadSchemaAsync(CancellationToken cancellationToken)
    {
        var nullable = await ReadScalarAsync(ColumnStateStatement, cancellationToken);
        if (nullable is null)
        {
            return new SchemaState(false, false, false, false);
        }

        var collation = await ReadScalarAsync(CollationStatement, cancellationToken);
        var index = await ReadScalarAsync(IndexStatement, cancellationToken);
        return new SchemaState(
            true,
            string.Equals(nullable, "YES", StringComparison.Ordinal),
            string.Equals(collation, BinaryCollation, StringComparison.Ordinal),
            index is not null);
    }

    private async Task<string?> ReadScalarAsync(string sql, CancellationToken cancellationToken)
    {
        await using var command = await CreateCommandAsync(sql, cancellationToken);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private async Task<List<StoredRow>> ReadRowsAsync(CancellationToken cancellationToken)
    {
        var rows = new List<StoredRow>();
        await using var command = await CreateCommandAsync(RowSelectStatement, cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new StoredRow(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2)));
        }

        return rows;
    }

    private static Plan BuildPlan(List<StoredRow> rows, RootMap rootMap)
    {
        var failures = new List<string>();
        var writes = new List<RowWrite?>(rows.Count);
        var keys = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            PlanRow(row, rootMap, failures, keys, writes);
        }

        AddCollisionFailures(keys, failures);
        if (failures.Count > 0)
        {
            throw new InvalidOperationException(DescribeFailures(failures));
        }

        return new Plan(writes);
    }

    private static void PlanRow(
        StoredRow row,
        RootMap rootMap,
        List<string> failures,
        Dictionary<string, List<int>> keys,
        List<RowWrite?> writes)
    {
        var canonical = rootMap.Canonicalize(row.LibraryPath, row.Id, failures);
        if (canonical is null)
        {
            writes.Add(null);
            return;
        }

        var key = canonical.ToUpperInvariant();
        TrackKey(keys, key, row.Id);
        writes.Add(NeedsWrite(row, canonical, key) ? new RowWrite(row.Id, canonical, key) : null);
    }

    private static void TrackKey(Dictionary<string, List<int>> keys, string key, int id)
    {
        if (!keys.TryAdd(key, [id]))
        {
            keys[key].Add(id);
        }
    }

    private static bool NeedsWrite(StoredRow row, string canonical, string key) =>
        !string.Equals(row.LibraryPath, canonical, StringComparison.Ordinal)
        || !string.Equals(row.LibraryPathKey, key, StringComparison.Ordinal);

    private static void AddCollisionFailures(Dictionary<string, List<int>> keys, List<string> failures)
    {
        foreach (var (key, ids) in keys)
        {
            if (ids.Count > 1)
            {
                failures.Add(
                    $"Movie IDs {string.Join(", ", ids.Select(id => id.ToString(CultureInfo.InvariantCulture)))} "
                    + $"map to the same library path key '{key}'.");
            }
        }
    }

    private static string DescribeFailures(List<string> failures)
    {
        var builder = new StringBuilder(
            "Library path gate preflight failed closed; no path or key writes were committed.");
        foreach (var failure in failures.Take(ReportedFailureLimit))
        {
            builder.Append(' ').Append(failure);
        }

        if (failures.Count > ReportedFailureLimit)
        {
            builder.Append(' ')
                .Append((failures.Count - ReportedFailureLimit).ToString(CultureInfo.InvariantCulture))
                .Append(" more failure(s).");
        }

        return builder.ToString();
    }

    private async Task<DbCommand> CreateCommandAsync(string sql, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        return command;
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private sealed record StoredRow(int Id, string LibraryPath, string? LibraryPathKey);

    private sealed record RowWrite(int Id, string Path, string Key);

    private sealed record Plan(List<RowWrite?> Writes);

    private sealed record SchemaState(
        bool ColumnExists,
        bool ColumnIsNullable,
        bool HasBinaryCollation,
        bool HasUniqueIndex)
    {
        public bool IsActivated =>
            ColumnExists && !ColumnIsNullable && HasBinaryCollation && HasUniqueIndex;
    }

    /// <summary>
    /// Maps stored values against the active library root, the configured legacy roots and the
    /// automatic Docker default alias, failing closed whenever a value maps ambiguously or not at all.
    /// </summary>
    private sealed class RootMap
    {
        private const string DefaultDockerRoot = "/media/library";

        private readonly string[] roots;

        private RootMap(string[] roots) => this.roots = roots;

        public static RootMap Create(LibraryOptions options)
        {
            if (!LibraryPath.IsAbsolute(options.RootPath))
            {
                throw new InvalidOperationException(
                    $"Library:RootPath '{options.RootPath}' must be an absolute library root before the library path gate can run.");
            }

            var roots = new List<string> { options.RootPath };
            roots.AddRange(options.LegacyRootPaths);
            if (!roots.Contains(DefaultDockerRoot, StringComparer.Ordinal))
            {
                roots.Add(DefaultDockerRoot);
            }

            return new RootMap([.. roots]);
        }

        /// <summary>
        /// Returns the canonical relative identity for <paramref name="value"/>, or null after adding a
        /// failure describing why the value cannot be mapped.
        /// </summary>
        public string? Canonicalize(string value, int id, List<string> failures)
        {
            if (!LibraryPath.IsAbsolute(value))
            {
                if (LibraryPath.TryCanonicalizeRelative(value, out var relative))
                {
                    return relative;
                }

                failures.Add($"Movie ID {Describe(id)} has a malformed library path '{value}'.");
                return null;
            }

            var suffixes = MapAbsolute(value);
            if (suffixes.Count == 1)
            {
                return suffixes.First();
            }

            failures.Add(suffixes.Count == 0
                ? $"Movie ID {Describe(id)} path '{value}' matches no active, legacy, or default Docker library root."
                : $"Movie ID {Describe(id)} path '{value}' maps ambiguously through {Describe(suffixes)}.");
            return null;
        }

        private HashSet<string> MapAbsolute(string value)
        {
            var suffixes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var root in roots)
            {
                if (LibraryPath.TryCanonicalizeUnderRoot(value, root, out var canonical))
                {
                    suffixes.Add(canonical);
                }
            }

            return suffixes;
        }

        private static string Describe(int id) => id.ToString(CultureInfo.InvariantCulture);

        private static string Describe(HashSet<string> suffixes) =>
            string.Join(", ", suffixes.OrderBy(suffix => suffix, StringComparer.Ordinal).Select(suffix => $"'{suffix}'"));
    }

    private static partial class GateLog
    {
        [LoggerMessage(EventId = 4590, Level = LogLevel.Information,
            Message = "Library path gate found the activated library path key schema; no writes were needed.")]
        public static partial void AlreadyActivated(ILogger logger);

        [LoggerMessage(EventId = 4591, Level = LogLevel.Information,
            Message = "Library path gate activated the library path key schema after rewriting {RewrittenRows} row(s).")]
        public static partial void Activated(ILogger logger, int rewrittenRows);

        [LoggerMessage(EventId = 4592, Level = LogLevel.Information,
            Message = "Library path gate verified the activated library path key schema without writing.")]
        public static partial void VerifiedOnly(ILogger logger);
    }
}
