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
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
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
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Stranded movie sweep failed.");
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
}