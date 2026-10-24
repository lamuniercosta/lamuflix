using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Enrichment;

public sealed class StrandedMovieSweeper(
    IServiceScopeFactory scopeFactory,
    IOptions<EnrichmentOptions> options,
    TimeProvider timeProvider,
    ILogger<StrandedMovieSweeper> logger) : BackgroundService
{
    private const string FeaturePassInactiveMessage =
        "The missing features recovery pass is inactive because the feature composition is not registered.";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunEnrichmentPassAsync(stoppingToken);
                await RunFeaturePassAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await Task.Delay(options.Value.SweepInterval, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunEnrichmentPassAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider
                .GetRequiredService<ICommandHandler<SweepStrandedMoviesCommand, int>>();
            await handler.HandleAsync(new SweepStrandedMoviesCommand(), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Stranded movie sweep failed.");
        }
    }

    private async Task RunFeaturePassAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider
                .GetService<ICommandHandler<SweepMissingMovieFeaturesCommand, int>>();
            if (handler is null)
            {
                logger.LogDebug("{Message}", FeaturePassInactiveMessage);
                return;
            }

            await handler.HandleAsync(new SweepMissingMovieFeaturesCommand(), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Missing movie features sweep failed.");
        }
    }
}
