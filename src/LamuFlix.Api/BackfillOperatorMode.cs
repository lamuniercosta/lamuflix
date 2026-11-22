using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LamuFlix.Api;

/// <summary>
/// The one-off TMDb recommendation backfill operator mode. Startup hands argv here before the web host
/// starts; this type owns argument validation, the configuration and migration preflight, dispatch and
/// sanitized console output. It never prints configuration values, request URIs or provider payloads.
/// </summary>
public static class BackfillOperatorMode
{
    public const string Flag = "--backfill-tmdb-recommendations";

    private const int CompletedExitCode = 0;
    private const int RefusedExitCode = 1;

    /// <summary>
    /// True when argv carries the operator flag or a malformed spelling of it, so startup must decide
    /// between dispatching and refusing instead of running the web host.
    /// </summary>
    public static bool IsRequested(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return args.Any(argument => argument.StartsWith(Flag, StringComparison.Ordinal));
    }

    /// <summary>
    /// The operator invocation is exactly the flag. Any other argument alongside it is malformed and is
    /// rejected before configuration, the database or the provider are touched.
    /// </summary>
    public static bool TryParse(IReadOnlyList<string> args, out string? error)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Count == 1 && string.Equals(args[0], Flag, StringComparison.Ordinal))
        {
            error = null;
            return true;
        }

        error = $"operator mode {Flag} accepts no other arguments.";
        return false;
    }

    /// <summary>
    /// Preflights configured TMDb and the applied migration, dispatches the Core backfill command and writes
    /// the sanitized five-count result. Returns the process exit code: 0 completed, 1 refused or stopped.
    /// </summary>
    public static async Task<int> RunAsync(
        IServiceProvider services,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(output);

        if (!IsTmdbConfigured(services))
        {
            output.WriteLine("tmdb-backfill: refused reason=tmdb-not-configured");
            return RefusedExitCode;
        }

        switch (await PendingMigrationAsync(services, cancellationToken))
        {
            case true:
                output.WriteLine("tmdb-backfill: refused reason=migration-not-applied");
                return RefusedExitCode;
            case null:
                output.WriteLine("tmdb-backfill: refused reason=database-preflight-unavailable");
                return RefusedExitCode;
            default:
                break;
        }

        var handler = services.GetRequiredService<
            ICommandHandler<BackfillTmdbRecommendationsCommand, BackfillTmdbRecommendationsResult>>();
        var result = await handler.HandleAsync(
            new BackfillTmdbRecommendationsCommand(),
            cancellationToken);
        Write(output, result);
        return result.Stopped ? RefusedExitCode : CompletedExitCode;
    }

    private static bool IsTmdbConfigured(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<TmdbOptions>>().Value;
        return options.Enabled && !string.IsNullOrWhiteSpace(options.ApiKey);
    }

    private static async Task<bool?> PendingMigrationAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<LamuFlixDbContext>();
            var pending = await database.Database.GetPendingMigrationsAsync(cancellationToken);
            return pending.Any();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void Write(TextWriter output, BackfillTmdbRecommendationsResult result)
    {
        var marker = result.StopReason is { } reason
            ? string.Concat("STOPPED reason=", ReasonName(reason), " ")
            : string.Empty;
        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"tmdb-backfill: {marker}imdb-eligible={result.ImdbEligible} matched={result.Matched} unmatched={result.Unmatched} skipped={result.Skipped} failures={result.Failures}"));
    }

    private static string ReasonName(BackfillStopReason reason) =>
        reason == BackfillStopReason.Cancelled ? "cancelled" : "provider-unavailable";
}
