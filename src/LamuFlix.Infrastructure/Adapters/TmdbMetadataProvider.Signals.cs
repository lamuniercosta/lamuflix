using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;

namespace LamuFlix.Infrastructure.Adapters;

public sealed partial class TmdbMetadataProvider
{
    private const string MalformedKeywords = "malformed keywords payload";
    private const string MalformedCredits = "malformed credits payload";

    private async Task<MovieSignalSnapshot> FetchSignalsAsync(int movieId, bool hasCollection, CancellationToken ct)
    {
        var keywords = await ReadKeywordsAsync(movieId, ct);
        var credits = await ReadCreditsAsync(movieId, ct);
        if (!keywords.Failed && !credits.Failed)
        {
            healthState.Record(category: null, wasUnauthorized: false);
        }

        return new MovieSignalSnapshot(
            keywords.Availability,
            keywords.Values,
            credits.Availability,
            credits.Values,
            hasCollection ? SignalAvailability.Available : SignalAvailability.KnownEmpty);
    }

    private async Task<KeywordSignal> ReadKeywordsAsync(int movieId, CancellationToken ct)
    {
        var fetch = await FetchAsync(BuildKeywordsUri(movieId), RequestKind.Keywords, ct);
        return fetch is Fetch.Succeeded succeeded
            ? MapKeywords(succeeded.Body, movieId)
            : UnavailableKeywords();
    }

    private async Task<CastSignal> ReadCreditsAsync(int movieId, CancellationToken ct)
    {
        var fetch = await FetchAsync(BuildCreditsUri(movieId), RequestKind.Credits, ct);
        return fetch is Fetch.Succeeded succeeded
            ? MapCredits(succeeded.Body, movieId)
            : UnavailableCredits();
    }

    private KeywordSignal MapKeywords(Response? body, int movieId)
    {
        if (SignalKeywords(body, movieId) is not { } keywords)
        {
            return MalformedKeywordsSignal();
        }

        var names = new Dictionary<int, string>();
        foreach (var keyword in keywords)
        {
            if (!TryAddKeyword(names, keyword))
            {
                return MalformedKeywordsSignal();
            }
        }

        return names.Count == 0
            ? new KeywordSignal(SignalAvailability.KnownEmpty, ImmutableArray<MovieKeyword>.Empty, Failed: false)
            : new KeywordSignal(
                SignalAvailability.Available,
                [.. names.OrderBy(pair => pair.Key).Select(pair => new MovieKeyword(pair.Key, pair.Value))],
                Failed: false);
    }

    private CastSignal MapCredits(Response? body, int movieId)
    {
        if (SignalCast(body, movieId) is not { } cast)
        {
            return MalformedCreditsSignal();
        }

        var names = ImmutableHashSet.CreateBuilder<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var credit in cast)
        {
            if (!TryAddCredit(names, credit))
            {
                return MalformedCreditsSignal();
            }
        }

        return names.Count == 0
            ? new CastSignal(SignalAvailability.KnownEmpty, EmptyCast(), Failed: false)
            : new CastSignal(SignalAvailability.Available, names.ToImmutable(), Failed: false);
    }

    private static List<KeywordPayload>? SignalKeywords(Response? body, int movieId) =>
        body is not null && body.Success != false && body.Id == movieId
            ? body.Keywords
            : null;

    private static List<CastPayload>? SignalCast(Response? body, int movieId) =>
        body is not null && body.Success != false && body.Id == movieId
            ? body.Cast
            : null;

    private static bool TryAddKeyword(Dictionary<int, string> names, KeywordPayload keyword)
    {
        if (keyword.Id is not { } id || id <= 0 || string.IsNullOrWhiteSpace(keyword.Name))
        {
            return false;
        }

        var name = keyword.Name.Trim();
        if (!names.TryGetValue(id, out var existing) || string.CompareOrdinal(name, existing) < 0)
        {
            names[id] = name;
        }

        return true;
    }

    private static bool TryAddCredit(ImmutableHashSet<string>.Builder names, CastPayload credit)
    {
        if (credit.Id is not { } id || id <= 0 || string.IsNullOrWhiteSpace(credit.Name))
        {
            return false;
        }

        names.Add(credit.Name.Trim());
        return true;
    }

    private KeywordSignal MalformedKeywordsSignal()
    {
        RecordSignalFailure(MalformedKeywords);
        return UnavailableKeywords();
    }

    private CastSignal MalformedCreditsSignal()
    {
        RecordSignalFailure(MalformedCredits);
        return UnavailableCredits();
    }

    private void RecordSignalFailure(string reason)
    {
        Log.SignalUnavailable(logger, reason);
        healthState.Record(EnrichmentFailureCategory.InvalidResponse, wasUnauthorized: false);
    }

    private static KeywordSignal UnavailableKeywords() =>
        new(SignalAvailability.Unavailable, ImmutableArray<MovieKeyword>.Empty, Failed: true);

    private static CastSignal UnavailableCredits() =>
        new(SignalAvailability.Unavailable, EmptyCast(), Failed: true);

    private static ImmutableHashSet<string> EmptyCast() =>
        ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);

    private readonly record struct KeywordSignal(
        SignalAvailability Availability,
        ImmutableArray<MovieKeyword> Values,
        bool Failed);

    private readonly record struct CastSignal(
        SignalAvailability Availability,
        ImmutableHashSet<string> Values,
        bool Failed);
}
