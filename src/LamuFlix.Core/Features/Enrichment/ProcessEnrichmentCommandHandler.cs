using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Core.Pipeline;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Features.Enrichment;

public sealed class ProcessEnrichmentCommandHandler(
    IMovieRepository movies,
    IMetadataProvider provider,
    IOptions<EnrichmentOptions> options,
    TimeProvider time,
    ILogger<ProcessEnrichmentCommandHandler> logger)
    : ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>
{
    public async Task<ProcessEnrichmentOutcome> HandleAsync(
        ProcessEnrichmentCommand command,
        CancellationToken cancellationToken)
    {
        if (!await movies.TryClaimForEnrichmentAsync(command.MovieId, cancellationToken))
        {
            return new ProcessEnrichmentOutcome.Completed(Claimed: false);
        }

        var movie = await movies.GetAsync(command.MovieId, cancellationToken)
                    ?? throw new NotFoundException();
        MetadataLookupResult result;
        try
        {
            var lookup = new MetadataLookup(movie.Title, movie.Metadata?.ReleaseYear);
            result = await provider.FindAsync(lookup, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return await FailAsync(
                command,
                EnrichmentFailureClassifier.Classify(exception),
                movie,
                cancellationToken);
        }

        if (result is MetadataLookupResult.Failed failed)
        {
            return await FailAsync(command, failed.Category, movie, cancellationToken);
        }

        Apply(movie, result, time.GetUtcNow());
        await movies.SaveChangesAsync(cancellationToken);
        return new ProcessEnrichmentOutcome.Completed(Claimed: true);
    }

    private async Task<ProcessEnrichmentOutcome> FailAsync(
        ProcessEnrichmentCommand command,
        EnrichmentFailureCategory category,
        Movie movie,
        CancellationToken cancellationToken)
    {
        if (EnrichmentRetryPolicy.Decide(category, command.Attempt, options.Value.MaxAttempts) is { } retry)
        {
            LogOutcome(command, category);
            return new ProcessEnrichmentOutcome.Failed(retry, category);
        }

        movie.MarkFailed(category, time.GetUtcNow());
        await movies.SaveChangesAsync(cancellationToken);
        var deadLetter = new EnrichmentFailureDecision(EnrichmentFailureAction.DeadLetter, null);
        LogOutcome(command, category);
        return new ProcessEnrichmentOutcome.Failed(deadLetter, category);
    }

    private static void Apply(Movie movie, MetadataLookupResult result, DateTimeOffset now)
    {
        if (result is MetadataLookupResult.Found found)
        {
            movie.MarkEnriched(found.Metadata, now);
            return;
        }

        movie.MarkNotFound(now);
    }

    private void LogOutcome(
        ProcessEnrichmentCommand command,
        EnrichmentFailureCategory category) =>
        logger.LogInformation(
            "Enrichment failed for {MovieId} attempt {Attempt} category {Category}",
            command.MovieId.Value,
            command.Attempt,
            category.Code);
}

