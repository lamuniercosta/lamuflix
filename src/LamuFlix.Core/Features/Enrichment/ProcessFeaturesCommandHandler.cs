using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Features.Enrichment;

public sealed class ProcessFeaturesCommandHandler(
    IMovieRepository movies,
    IMovieFeaturesRepository features,
    IEmbeddingGenerator<string, Embedding<float>> generator,
    IOptions<EnrichmentOptions> options,
    TimeProvider time,
    ILogger<ProcessFeaturesCommandHandler> logger)
    : ICommandHandler<ProcessFeaturesCommand, ProcessFeaturesOutcome>
{
    private static readonly EnrichmentFailureDecision DeadLetterDecision =
        new(EnrichmentFailureAction.DeadLetter, null);

    public async Task<ProcessFeaturesOutcome> HandleAsync(
        ProcessFeaturesCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (IsInvalidPayload(command))
        {
            return Fail(command, EnrichmentFailureCategory.InvalidResponse);
        }

        try
        {
            return await ProcessAsync(command, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Fail(command, Classify(exception));
        }
    }

    private async Task<ProcessFeaturesOutcome> ProcessAsync(
        ProcessFeaturesCommand command,
        CancellationToken cancellationToken)
    {
        var plot = await movies.GetPlotAsync(command.MovieId, cancellationToken);
        if (string.IsNullOrWhiteSpace(plot))
        {
            return NoInput(command);
        }

        var generated = await generator.GenerateAsync([plot], cancellationToken: cancellationToken);
        var snapshot = CreateSnapshot(command.MovieId, generated);
        if (snapshot is null)
        {
            return Fail(command, EnrichmentFailureCategory.InvalidResponse);
        }

        await features.UpsertAsync(snapshot, cancellationToken);
        return Complete(command);
    }

    private MovieFeaturesSnapshot? CreateSnapshot(
        MovieId movieId,
        GeneratedEmbeddings<Embedding<float>> generated)
    {
        if (generated.Count != 1)
        {
            return null;
        }

        var vector = generated[0].Vector;
        if (vector.IsEmpty)
        {
            return null;
        }

        try
        {
            return new MovieFeaturesSnapshot(
                movieId,
                ImmutableArray.CreateRange(vector.ToArray()),
                time.GetUtcNow());
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static bool IsInvalidPayload(ProcessFeaturesCommand command) =>
        command.MovieId is null || command.Attempt < 1;

    private static EnrichmentFailureCategory Classify(Exception exception)
    {
        var category = exception is EmbeddingGenerationException embedding
            ? embedding.Category
            : EnrichmentFailureClassifier.Classify(exception);

        return IsFeatureCategory(category) ? category : EnrichmentFailureCategory.Unknown;
    }

    private static bool IsFeatureCategory(EnrichmentFailureCategory category) =>
        ReferenceEquals(category, EnrichmentFailureCategory.ProviderUnavailable)
        || ReferenceEquals(category, EnrichmentFailureCategory.RateLimited)
        || ReferenceEquals(category, EnrichmentFailureCategory.InvalidResponse)
        || ReferenceEquals(category, EnrichmentFailureCategory.Unknown);

    private ProcessFeaturesOutcome Complete(ProcessFeaturesCommand command)
    {
        logger.LogInformation(
            "Feature processing for {MovieId} attempt {Attempt} completed generated",
            command.MovieId.Value,
            command.Attempt);
        return new ProcessFeaturesOutcome.Completed(Generated: true);
    }

    private ProcessFeaturesOutcome NoInput(ProcessFeaturesCommand command)
    {
        logger.LogInformation(
            "Feature processing for {MovieId} attempt {Attempt} completed with no input",
            command.MovieId.Value,
            command.Attempt);
        return new ProcessFeaturesOutcome.Completed(Generated: false);
    }

    private ProcessFeaturesOutcome Fail(ProcessFeaturesCommand command, EnrichmentFailureCategory category)
    {
        var decision = EnrichmentRetryPolicy.Decide(category, command.Attempt, options.Value.MaxAttempts)
                       ?? DeadLetterDecision;
        logger.LogInformation(
            "Feature processing for {MovieId} attempt {Attempt} failed category {Category}",
            command.MovieId?.Value,
            command.Attempt,
            category.Code);
        return new ProcessFeaturesOutcome.Failed(decision, category);
    }
}
