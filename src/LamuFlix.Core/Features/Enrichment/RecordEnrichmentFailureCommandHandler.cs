using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Core.Pipeline;
using Microsoft.Extensions.Logging;

namespace LamuFlix.Core.Features.Enrichment;

public sealed class RecordEnrichmentFailureCommandHandler(
    IMovieRepository movies,
    TimeProvider time,
    EnrichmentOptions options,
    ILogger<RecordEnrichmentFailureCommandHandler> logger)
    : ICommandHandler<RecordEnrichmentFailureCommand, EnrichmentFailureDecision>
{
    public async Task<EnrichmentFailureDecision> HandleAsync(
        RecordEnrichmentFailureCommand command,
        CancellationToken cancellationToken)
    {
        if (EnrichmentRetryPolicy.Decide(command.Category, command.Attempt, options.MaxAttempts) is { } retry)
        {
            Log(command, retry);
            return retry;
        }

        var movie = await movies.GetAsync(command.Id, cancellationToken)
                    ?? throw new NotFoundException();
        movie.MarkFailed(command.Category, time.GetUtcNow());
        await movies.SaveChangesAsync(cancellationToken);
        var deadLetter = new EnrichmentFailureDecision(EnrichmentFailureAction.DeadLetter, null);
        Log(command, deadLetter);
        return deadLetter;
    }

    private void Log(RecordEnrichmentFailureCommand command, EnrichmentFailureDecision decision) =>
        logger.LogInformation(
            "Enrichment failure {Action} for {MovieId} attempt {Attempt} category {Category}",
            ActionName(decision.Action),
            command.Id.Value,
            command.Attempt,
            command.Category.Code);

    private static string ActionName(EnrichmentFailureAction action)
    {
        if (ReferenceEquals(action, EnrichmentFailureAction.Retry))
        {
            return "Retry";
        }

        if (ReferenceEquals(action, EnrichmentFailureAction.RetryDelayed))
        {
            return "RetryDelayed";
        }

        return "DeadLetter";
    }
}
