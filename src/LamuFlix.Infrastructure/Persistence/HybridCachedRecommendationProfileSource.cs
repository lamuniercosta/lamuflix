using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Persistence;

public sealed partial class HybridCachedRecommendationProfileSource : IRecommendationProfileSource
{
    private const string CacheKey = "LamuFlix.Recommendations.TasteProfile.v2";

    private readonly HybridCache cache;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly TimeProvider timeProvider;
    private readonly CachedProfileSerializer serializer;
    private readonly ILogger<HybridCachedRecommendationProfileSource> logger;
    private readonly IOptions<HybridCacheOptions> hybridOptions;
    private readonly TimeSpan expiration;
    private readonly double warningFraction;

    private long generation;

    internal HybridCachedRecommendationProfileSource(
        HybridCache cache,
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        IOptions<TasteProfileCacheOptions> options,
        CachedProfileSerializer serializer,
        ILogger<HybridCachedRecommendationProfileSource> logger,
        IOptions<HybridCacheOptions> hybridOptions)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.hybridOptions = hybridOptions ?? throw new ArgumentNullException(nameof(hybridOptions));
        ArgumentNullException.ThrowIfNull(options);

        expiration = options.Value.Expiration;
        warningFraction = options.Value.WarningFraction;
    }

    internal CachedProfileSerializer Serializer => serializer;

    internal double WarningFraction => warningFraction;

    public async Task<TasteProfile> GetTasteProfileAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        while (true)
        {
            var entry = await cache.GetOrCreateAsync(
                CacheKey,
                CreateEntryAsync,
                tags: [CacheKey],
                cancellationToken: ct);

            ct.ThrowIfCancellationRequested();

            if (!IsStale(entry))
            {
                return entry.Profile;
            }

            await cache.RemoveAsync(CacheKey, cancellationToken: ct);
        }
    }

    public async Task<RecommendationProfileInputs> GetProfileInputsAsync(
        IReadOnlyCollection<MovieId> candidateMovieIds,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(candidateMovieIds);
        ct.ThrowIfCancellationRequested();

        var uncachedSource = ResolveUncachedSource(out var scope);
        await using (scope)
        {
            return await uncachedSource.GetProfileInputsAsync(candidateMovieIds, ct);
        }
    }

    internal async Task InvalidateAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        Interlocked.Increment(ref generation);

        await cache.RemoveByTagAsync(CacheKey, ct);
    }

    private async ValueTask<CachedProfile> CreateEntryAsync(CancellationToken factoryToken)
    {
        var generationSnapshot = Interlocked.Read(ref generation);
        var createdAt = timeProvider.GetUtcNow();

        var uncachedSource = ResolveUncachedSource(out var scope);
        await using (scope)
        {
            var profile = await uncachedSource.GetTasteProfileAsync(factoryToken);

            factoryToken.ThrowIfCancellationRequested();

            var entry = new CachedProfile(profile, generationSnapshot, createdAt);

            ObservePayload(entry);

            return entry;
        }
    }

    internal void ObservePayload(CachedProfile entry)
    {
        var writer = new ArrayBufferWriter<byte>();
        serializer.Serialize(entry, writer);

        var payloadBytes = writer.WrittenCount;
        var budgetBytes = hybridOptions.Value.MaximumPayloadBytes;
        var fraction = budgetBytes > 0 ? (double)payloadBytes / budgetBytes : 0.0;

        Log.PayloadObserved(logger, payloadBytes, budgetBytes, fraction);

        if (fraction >= warningFraction)
        {
            Log.PayloadNearLimit(logger, payloadBytes, budgetBytes, fraction);
        }
    }

    private EfRecommendationProfileSource ResolveUncachedSource(out AsyncServiceScope scope)
    {
        scope = scopeFactory.CreateAsyncScope();
        return scope.ServiceProvider.GetRequiredService<EfRecommendationProfileSource>();
    }

    private bool IsStale(CachedProfile entry) =>
        entry.Generation != Interlocked.Read(ref generation)
        || timeProvider.GetUtcNow() - entry.CreatedAt >= expiration;
}

internal sealed record CachedProfile(TasteProfile Profile, long Generation, DateTimeOffset CreatedAt);

internal sealed class CachedProfileSerializer : IHybridCacheSerializer<CachedProfile>
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new MovieIdDictionaryKeyConverter());
        return options;
    }

    private int serializeCount;
    private int deserializeCount;
    private int lastSerializedBytes;

    public int SerializeCount => Volatile.Read(ref serializeCount);

    public int DeserializeCount => Volatile.Read(ref deserializeCount);

    public int LastSerializedBytes => Volatile.Read(ref lastSerializedBytes);

    public CachedProfile Deserialize(ReadOnlySequence<byte> source)
    {
        Interlocked.Increment(ref deserializeCount);

        var payload = source.IsSingleSegment ? source.FirstSpan.ToArray() : source.ToArray();

        return JsonSerializer.Deserialize<CachedProfile>(payload, SerializerOptions)
            ?? throw new InvalidOperationException("The cached taste profile payload deserialized to null.");
    }

    public void Serialize(CachedProfile value, IBufferWriter<byte> target)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(target);

        Interlocked.Increment(ref serializeCount);

        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
        Volatile.Write(ref lastSerializedBytes, bytes.Length);

        target.Write(bytes);
    }

    private sealed class MovieIdDictionaryKeyConverter : JsonConverter<MovieId>
    {
        public override MovieId Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var document = JsonDocument.ParseValue(ref reader);
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    return new MovieId(property.Value.GetInt32());
                }

                throw new JsonException("The cached taste profile contains an empty movie id object.");
            }

            return reader.TokenType == JsonTokenType.String
                ? Parse(reader.GetString())
                : new MovieId(reader.GetInt32());
        }

        public override void Write(Utf8JsonWriter writer, MovieId value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value.Value);

        public override MovieId ReadAsPropertyName(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
            Parse(reader.GetString());

        public override void WriteAsPropertyName(
            Utf8JsonWriter writer,
            MovieId value,
            JsonSerializerOptions options) =>
            writer.WritePropertyName(value.Value.ToString(CultureInfo.InvariantCulture));

        private static MovieId Parse(string? value) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? new MovieId(parsed)
                : throw new JsonException($"The cached taste profile contains an invalid movie id '{value}'.");
    }
}
