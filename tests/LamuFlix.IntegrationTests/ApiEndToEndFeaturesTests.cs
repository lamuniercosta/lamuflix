using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Core.Pipeline;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.UnitTests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace LamuFlix.IntegrationTests;

/// <summary>
/// DEV-414 T022: the started Api composition carries a metadata enrichment through FeaturesRequested to
/// one movie_features row on the same trace, with a fake embedding generator, real PostgreSQL, and a real
/// RabbitMQ broker, and leaves the metadata enrichment state unchanged.
/// </summary>
[Collection(nameof(ApiEndToEndCollection))]
public sealed class ApiEndToEndFeaturesTests(ApiEndToEndFixture fixture) : ApiEndToEndTestBase(fixture)
{
    private const string RetryRoutePrefix = "/api/movies/";
    private const string RetryRouteSuffix = "/enrichment";
    private const string SeedTitle = "Features End To End";
    private const string SeedPathSuffix = "features-end-to-end";
    private const string ExpectedSynopsis = "A stored plot that flows into a persisted embedding row.";
    private const string ExpectedImdbId = "tt4140001";
    private const int ExpectedYear = 1988;
    private const int ExpectedRuntime = 101;
    private static readonly TimeSpan ArrivalTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan TraceTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);

    private static readonly string OmdbBody = $$"""
        {
          "Response": "True",
          "Title": "{{SeedTitle}}",
          "Year": "{{ExpectedYear}}",
          "Runtime": "{{ExpectedRuntime}} min",
          "Plot": "{{ExpectedSynopsis}}",
          "Genre": "Drama",
          "imdbID": "{{ExpectedImdbId}}"
        }
        """;

    [Fact]
    public async Task EnrichmentThroughFeaturesRequested_PersistsOneFeaturesRowOnOneTraceAndKeepsMetadataState()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        while (Activity.Current is not null)
        {
            Activity.Current.Stop();
        }

        var gate = new GatedEmbeddingGenerator();
        using var capture = new ApiImportTraceCapture();
        var client = await StartHostAsync(InstallOmdbStub, gate.InstallInto);
        var seed = MovieCatalogSeed.Create(SeedTitle, SeedPathSuffix);
        seed.Status = EnrichmentStatus.Failed;
        seed.EnrichmentAttempts = 1;
        seed.EnrichmentFailureCategory = EnrichmentFailureCategory.ProviderUnavailable;
        seed.LastAttemptAt = null;
        var movie = await SeedAsync(seed);
        var movieId = movie.Id.ShouldNotBeNull();
        var computedAt = Clock.GetUtcNow();
        var requestTraceId = ActivityTraceId.CreateRandom();
        var traceparent =
            $"00-{requestTraceId.ToHexString()}-{ActivitySpanId.CreateRandom().ToHexString()}-01";
        using var request = new HttpRequestMessage(HttpMethod.Post, RetryRoute(movieId.Value));
        request.Headers.Add(TraceContextCarrier.TraceParentHeader, traceparent);
        Activity.Current.ShouldBeNull("The retry POST must carry only its unique traceparent with no ambient test Activity.");

        try
        {
            // act
            using var response = await client.SendAsync(request, cancellationToken);

            // assert
            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            await gate.WaitForArrivalAsync(cancellationToken);
            var metadataBefore = await ReadMetadataStateAsync(movieId, cancellationToken);
            metadataBefore.Status.ShouldBe(EnrichmentStatus.Enriched);

            // act
            gate.Release();
            var snapshot = await WaitForSnapshotAsync(movieId, cancellationToken);

            // assert
            await using (var context = Fixture.Postgres.CreateMigratedContext())
            {
                (await context.MovieFeatures.AsNoTracking().CountAsync(cancellationToken)).ShouldBe(1);
            }

            snapshot.MovieId.ShouldBe(movieId);
            snapshot.Vector.Length.ShouldBe(EmbeddingDimensions.Value);
            var expectedVector = (await new FakeEmbeddingGenerator()
                .GenerateAsync([ExpectedSynopsis], cancellationToken: cancellationToken))[0].Vector.ToArray();
            snapshot.Vector.ToArray().ShouldBe(expectedVector);
            snapshot.ComputedAt.ShouldBe(ToDatabasePrecision(computedAt));

            var metadataAfter = await ReadMetadataStateAsync(movieId, cancellationToken);
            metadataAfter.ShouldBe(metadataBefore);

            await using (var context = Fixture.Postgres.CreateMigratedContext())
            {
                var storedPlot = await context.Movies.AsNoTracking()
                    .Where(candidate => candidate.Id == movieId)
                    .Select(candidate => candidate.Plot)
                    .SingleAsync(cancellationToken);
                storedPlot.ShouldBe(ExpectedSynopsis);
            }

            // assert
            var trace = await WaitForFeatureTraceAsync(capture, requestTraceId, cancellationToken);
            ShouldProveFeatureTrace(trace);
        }
        finally
        {
            gate.Release();
        }
    }

    private static DateTimeOffset ToDatabasePrecision(DateTimeOffset value) =>
        new(value.Ticks - (value.Ticks % TimeSpan.TicksPerMicrosecond), value.Offset);

    private static void InstallOmdbStub(WireMockServer server) =>
        server
            .Given(Request.Create().UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(OmdbBody));

    private static string RetryRoute(int id) =>
        string.Create(CultureInfo.InvariantCulture, $"{RetryRoutePrefix}{id}{RetryRouteSuffix}");

    private async Task<MetadataState> ReadMetadataStateAsync(MovieId movieId, CancellationToken cancellationToken)
    {
        await using var context = Fixture.Postgres.CreateMigratedContext();
        return await context.Movies.AsNoTracking()
            .Where(candidate => candidate.Id == movieId)
            .Select(candidate => new MetadataState(candidate.Status, candidate.EnrichmentAttempts))
            .SingleAsync(cancellationToken);
    }

    private async Task<MovieFeaturesSnapshot> WaitForSnapshotAsync(
        MovieId movieId,
        CancellationToken cancellationToken)
    {
        var deadline = TimeProvider.System.GetUtcNow() + ArrivalTimeout;
        while (TimeProvider.System.GetUtcNow() < deadline)
        {
            await using var context = Fixture.Postgres.CreateMigratedContext();
            var stored = await new EfMovieFeaturesRepository(context)
                .GetByMovieIdAsync(movieId, cancellationToken);
            if (stored is not null)
            {
                return stored;
            }

            await Task.Delay(PollInterval, cancellationToken);
        }

        throw new InvalidOperationException(
            $"The features consumer did not persist movie {movieId.Value} within {ArrivalTimeout.TotalSeconds} seconds.");
    }

    private static async Task<FeatureTrace> WaitForFeatureTraceAsync(
        ApiImportTraceCapture capture,
        ActivityTraceId requestTraceId,
        CancellationToken cancellationToken)
    {
        var deadline = TimeProvider.System.GetUtcNow() + TraceTimeout;
        while (TimeProvider.System.GetUtcNow() < deadline)
        {
            var trace = FindFeatureTrace(capture.Stopped, requestTraceId);
            if (trace is not null)
            {
                return trace;
            }

            await Task.Delay(PollInterval, cancellationToken);
        }

        var final = FindFeatureTrace(capture.Stopped, requestTraceId);
        return final ?? throw new InvalidOperationException(
            $"The request trace {requestTraceId} did not produce a features consumer span within {TraceTimeout.TotalSeconds} seconds.");
    }

    private static FeatureTrace? FindFeatureTrace(
        IReadOnlyList<Activity> stopped,
        ActivityTraceId requestTraceId)
    {
        var trace = stopped.Where(activity => activity.TraceId == requestTraceId).ToArray();
        return FeatureTrace.From(trace);
    }

    private static void ShouldProveFeatureTrace(FeatureTrace trace)
    {
        trace.Consumer.Kind.ShouldBe(ActivityKind.Consumer);
        trace.Consumer.OperationName.ShouldBe(TelemetryConstants.FeaturesProcess);
        trace.Producer.SpanId.ToHexString().ShouldBe(trace.Consumer.ParentSpanId.ToHexString());
        trace.Producer.Source.Name.ShouldBe(TelemetryConstants.RabbitMqPublisherActivitySourceName);
        trace.Enqueue.Source.Name.ShouldBe(TelemetryConstants.ActivitySourceName);
        trace.Enqueue.OperationName.ShouldBe(TelemetryConstants.EnrichmentEnqueue);
        trace.Producer.ParentSpanId.ToHexString().ShouldBe(trace.Enqueue.SpanId.ToHexString());
        trace.EnrichmentHandler.OperationName.ShouldBe(nameof(ProcessEnrichmentCommand));
        ShouldDescendFrom(trace.Enqueue, trace.EnrichmentHandler, trace.BySpanId);
        ShouldDescendFrom(trace.FeatureHandler, trace.Consumer, trace.BySpanId);
        trace.FeatureHandler.OperationName.ShouldBe(nameof(ProcessFeaturesCommand));
    }

    private static void ShouldDescendFrom(
        Activity activity,
        Activity ancestor,
        IReadOnlyDictionary<string, Activity> bySpanId)
    {
        var current = activity;
        for (var hops = 0; hops <= bySpanId.Count; hops++)
        {
            if (!bySpanId.TryGetValue(current.ParentSpanId.ToHexString(), out var parent))
            {
                break;
            }

            if (parent.SpanId == ancestor.SpanId)
            {
                return;
            }

            current = parent;
        }

        throw new InvalidOperationException(
            $"'{activity.OperationName}' ({activity.SpanId}) does not descend from '{ancestor.OperationName}' ({ancestor.SpanId}).");
    }

    private sealed record MetadataState(EnrichmentStatus Status, int EnrichmentAttempts);

    private sealed record FeatureTrace(
        Activity Producer,
        Activity Enqueue,
        Activity EnrichmentHandler,
        Activity Consumer,
        Activity FeatureHandler,
        IReadOnlyDictionary<string, Activity> BySpanId)
    {
        public static FeatureTrace? From(IReadOnlyList<Activity> trace)
        {
            var consumers = trace
                .Where(activity =>
                    activity.Source.Name == TelemetryConstants.ActivitySourceName
                    && activity.OperationName == TelemetryConstants.FeaturesProcess)
                .ToArray();
            if (consumers.Length != 1)
            {
                return null;
            }

            var consumer = consumers[0];
            var producer = trace.SingleOrDefault(activity =>
                activity.SpanId.ToHexString() == consumer.ParentSpanId.ToHexString());
            var enqueue = producer is null
                ? null
                : trace.SingleOrDefault(activity =>
                    activity.Source.Name == TelemetryConstants.ActivitySourceName
                    && activity.OperationName == TelemetryConstants.EnrichmentEnqueue
                    && activity.SpanId == producer.ParentSpanId);
            var enrichmentHandlers = trace
                .Where(activity => activity.OperationName == nameof(ProcessEnrichmentCommand))
                .ToArray();
            var featureHandlers = trace
                .Where(activity => activity.OperationName == nameof(ProcessFeaturesCommand))
                .ToArray();
            if (producer is null || enqueue is null || enrichmentHandlers.Length != 1 || featureHandlers.Length != 1)
            {
                return null;
            }

            return new FeatureTrace(
                producer,
                enqueue,
                enrichmentHandlers[0],
                consumer,
                featureHandlers[0],
                trace.ToDictionary(activity => activity.SpanId.ToHexString()));
        }
    }

    private sealed class GatedEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
    {
        private readonly FakeEmbeddingGenerator inner = new();
        private readonly TaskCompletionSource arrival = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void InstallInto(IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            services.RemoveAll<IEmbeddingGenerator<string, Embedding<float>>>();
            services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(this);
        }

        public Task WaitForArrivalAsync(CancellationToken cancellationToken) =>
            arrival.Task.WaitAsync(cancellationToken);

        public void Release() => release.TrySetResult();

        public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            arrival.TrySetResult();
            await release.Task.WaitAsync(cancellationToken);
            return await inner.GenerateAsync(values, options, cancellationToken);
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            inner.GetService(serviceType, serviceKey);

        public void Dispose() => inner.Dispose();
    }
}
