using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Adapters;

public sealed partial class OllamaEmbeddingStartupValidator(
    IServiceScopeFactory scopeFactory,
    OllamaEmbeddingModelState modelState,
    IOptions<OllamaOptions> options,
    ILogger<OllamaEmbeddingStartupValidator> logger) : IHostedService
{
    internal const string ProbeText = "LamuFlix embedding startup verification probe.";

    private readonly OllamaOptions ollamaOptions = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var endpoint = new Uri(ollamaOptions.Endpoint);

        using var scope = scopeFactory.CreateScope();
        var generator = scope.ServiceProvider
            .GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        try
        {
            var result = await generator.GenerateAsync(
                [ProbeText],
                cancellationToken: cancellationToken);

            RequireSingleVerifiedVector(result);
            modelState.PublishVerified(endpoint, ollamaOptions.ModelName, EmbeddingDimensions.Value);
            EmbeddingDimensions.Validate(generator);
        }
        catch (EmbeddingGenerationException exception)
        {
            modelState.RecordFailure(exception.Category);
            Log.Failed(
                logger,
                OllamaSharpEmbeddingGenerator.SafeEndpoint(endpoint),
                ollamaOptions.ModelName,
                exception.Category.Code);
            throw new InvalidOperationException(
                string.Concat(
                    "Ollama embedding startup verification failed at ",
                    OllamaSharpEmbeddingGenerator.SafeEndpoint(endpoint),
                    " for model '",
                    ollamaOptions.ModelName,
                    "': ",
                    exception.Category.Code,
                    ". ",
                    exception.Message),
                exception);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static void RequireSingleVerifiedVector(GeneratedEmbeddings<Embedding<float>> result)
    {
        if (result.Count != 1
            || result[0].Vector.Length != EmbeddingDimensions.Value
            || !AllFinite(result[0].Vector.Span))
        {
            throw new EmbeddingGenerationException(
                EnrichmentFailureCategory.InvalidResponse,
                "The embedding startup probe did not return exactly one finite 384-dimension vector.");
        }
    }

    private static bool AllFinite(ReadOnlySpan<float> values)
    {
        foreach (var value in values)
        {
            if (!float.IsFinite(value))
            {
                return false;
            }
        }

        return true;
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Critical,
            Message = "Embedding startup verification failed at {Endpoint} with model {Model}: category {Category}.")]
        public static partial void Failed(ILogger logger, string endpoint, string model, string category);
    }
}
