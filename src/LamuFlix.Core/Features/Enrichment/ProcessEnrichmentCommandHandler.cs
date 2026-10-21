using System;
using System.Collections.Generic;
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
    IEnrichmentQueue queue,
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

        if (await IsHeldByAnotherMovieAsync(movie, movie.ManualImdbId, cancellationToken))
        {
            return await FailAsync(command, EnrichmentFailureCategory.DuplicateImdbId, movie, cancellationToken);
        }

        var result = await LookupAsync(movie, cancellationToken);

        if (result is MetadataLookupResult.Failed failed)
        {
            return await FailAsync(command, failed.Category, movie, cancellationToken);
        }

        if (await IsFoundImdbIdHeldByAnotherMovieAsync(movie, result, cancellationToken))
        {
            return await FailAsync(command, EnrichmentFailureCategory.DuplicateImdbId, movie, cancellationToken);
        }

        return await CompleteAsync(command, movie, result, cancellationToken);
    }

    private async Task<MetadataLookupResult> LookupAsync(Movie movie, CancellationToken cancellationToken)
    {
        // ReSharper disable once RedundantAssignment - the seed keeps the lookup result assigned when Stryker removes a try or catch body, so those mutants compile instead of tripping CS0165.
        MetadataLookupResult result = new MetadataLookupResult.Failed(EnrichmentFailureCategory.Unknown);
        try
        {
            var lookup = new MetadataLookup(
                movie.Title,
                movie.Metadata?.ReleaseYear ?? movie.ParsedReleaseYear,
                movie.ManualImdbId);
            result = await provider.FindAsync(lookup, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            result = new MetadataLookupResult.Failed(EnrichmentFailureClassifier.Classify(exception));
        }

        return result;
    }

    private async Task<ProcessEnrichmentOutcome> CompleteAsync(
        ProcessEnrichmentCommand command,
        Movie movie,
        MetadataLookupResult result,
        CancellationToken cancellationToken)
    {
        RecordLookupOutcome(result);
        Apply(movie, result, time.GetUtcNow());
        try
        {
            await movies.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateImdbIdException)
        {
            // Another row took the id between the lookup check and the save: reload the rejected row and end it as a duplicate.
            // The classified outcome was already recorded above, so settlement records no second measurement.
            var fresh = await movies.GetAsync(command.MovieId, cancellationToken)
                        ?? throw new NotFoundException();
            return await SettleFailureAsync(command, EnrichmentFailureCategory.DuplicateImdbId, fresh, cancellationToken);
        }

        if (result is MetadataLookupResult.Found && !string.IsNullOrWhiteSpace(movie.Metadata?.Synopsis))
        {
            await queue.EnqueueAsync(new FeaturesRequested(movie.Id, 1), cancellationToken);
        }

        return new ProcessEnrichmentOutcome.Completed(Claimed: true);
    }

    private async Task<bool> IsFoundImdbIdHeldByAnotherMovieAsync(
        Movie movie,
        MetadataLookupResult result,
        CancellationToken cancellationToken) =>
        result is MetadataLookupResult.Found found
        && await IsHeldByAnotherMovieAsync(movie, found.Metadata.ImdbId, cancellationToken);

    private async Task<bool> IsHeldByAnotherMovieAsync(Movie movie, ImdbId? imdbId, CancellationToken cancellationToken)
    {
        if (imdbId is null)
        {
            return false;
        }

        var holder = await movies.FindMovieIdByImdbIdAsync(imdbId, cancellationToken);
        return holder is not null && holder != movie.Id;
    }

    private async Task<ProcessEnrichmentOutcome> FailAsync(
        ProcessEnrichmentCommand command,
        EnrichmentFailureCategory category,
        Movie movie,
        CancellationToken cancellationToken)
    {
        EnrichmentMetrics.EnrichmentOutcome.Add(
            1,
            new KeyValuePair<string, object?>(TelemetryConstants.OutcomeTagName, TelemetryConstants.FailedOutcome),
            new KeyValuePair<string, object?>(TelemetryConstants.FailureCategoryTagName, category.Code));
        return await SettleFailureAsync(command, category, movie, cancellationToken);
    }

    private async Task<ProcessEnrichmentOutcome> SettleFailureAsync(
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

    private static void RecordLookupOutcome(MetadataLookupResult result) =>
        EnrichmentMetrics.EnrichmentOutcome.Add(
            1,
            new KeyValuePair<string, object?>(
                TelemetryConstants.OutcomeTagName,
                result is MetadataLookupResult.NotFound
                    ? TelemetryConstants.NotFoundOutcome
                    : TelemetryConstants.EnrichedOutcome));

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
