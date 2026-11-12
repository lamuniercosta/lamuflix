using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Subtitles;

public sealed record SubtitleCandidateBinding(
    string CandidateId,
    MovieId MovieId,
    string Language,
    SubtitleCandidate Candidate,
    string ReleaseName,
    DateTimeOffset ExpiresAtUtc,
    long IssuedSequence);

public sealed class SubtitleCandidateBinder
{
    private const int TokenByteCount = 16;

    private readonly Dictionary<string, SubtitleCandidateBinding> bindings = new(StringComparer.Ordinal);
    private readonly object gate = new();
    private readonly Random random;
    private readonly TimeProvider timeProvider;
    private readonly Func<TimeSpan> tokenTtl;
    private readonly Func<int> maxEntries;
    private long sequence;

    public SubtitleCandidateBinder(
        TimeProvider timeProvider,
        Random random,
        Func<TimeSpan> tokenTtl,
        Func<int> maxEntries)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(tokenTtl);
        ArgumentNullException.ThrowIfNull(maxEntries);

        this.timeProvider = timeProvider;
        this.random = random;
        this.tokenTtl = tokenTtl;
        this.maxEntries = maxEntries;
    }

    public int Count
    {
        get
        {
            lock (gate)
            {
                return bindings.Count;
            }
        }
    }

    public string Issue(MovieId movieId, string language, SubtitleCandidate candidate, string releaseName)
    {
        ArgumentNullException.ThrowIfNull(movieId);
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        ArgumentNullException.ThrowIfNull(candidate);

        lock (gate)
        {
            var now = timeProvider.GetUtcNow();
            PurgeExpired(now);
            EnsureCapacity();

            var candidateId = MintToken();
            bindings[candidateId] = new SubtitleCandidateBinding(
                candidateId,
                movieId,
                language,
                candidate,
                releaseName,
                now.Add(TokenTtl()),
                ++sequence);
            return candidateId;
        }
    }

    public bool TryResolve(string? candidateId, MovieId movieId, [NotNullWhen(true)] out SubtitleCandidateBinding? binding)
    {
        ArgumentNullException.ThrowIfNull(movieId);

        binding = null;
        if (string.IsNullOrWhiteSpace(candidateId))
        {
            return false;
        }
        lock (gate)
        {
            if (!bindings.TryGetValue(candidateId.Trim(), out var found))
            {
                return false;
            }

            if (found.ExpiresAtUtc <= timeProvider.GetUtcNow())
            {
                bindings.Remove(found.CandidateId);
                return false;
            }

            if (found.MovieId != movieId)
            {
                return false;
            }

            binding = found;
            return true;
        }
    }

    private TimeSpan TokenTtl()
    {
        var ttl = tokenTtl();
        return ttl > TimeSpan.Zero ? ttl : TimeSpan.Zero;
    }

    private void PurgeExpired(DateTimeOffset now)
    {
        List<string>? expired = null;
        foreach (var entry in bindings.Values)
        {
            if (entry.ExpiresAtUtc <= now)
            {
                (expired ??= []).Add(entry.CandidateId);
            }
        }

        if (expired is null)
        {
            return;
        }

        foreach (var candidateId in expired)
        {
            bindings.Remove(candidateId);
        }
    }

    private void EnsureCapacity()
    {
        var limit = Math.Max(1, maxEntries());
        while (bindings.Count >= limit)
        {
            var victim = FindEvictionVictim();
            if (victim is null)
            {
                return;
            }

            bindings.Remove(victim);
        }
    }

    private string? FindEvictionVictim()
    {
        SubtitleCandidateBinding? victim = null;
        foreach (var entry in bindings.Values)
        {
            if (victim is null
                || entry.ExpiresAtUtc < victim.ExpiresAtUtc
                || (entry.ExpiresAtUtc == victim.ExpiresAtUtc && entry.IssuedSequence < victim.IssuedSequence))
            {
                victim = entry;
            }
        }

        return victim?.CandidateId;
    }

    private string MintToken()
    {
        var bytes = new byte[TokenByteCount];
        string token;
        do
        {
            random.NextBytes(bytes);
            token = Convert.ToHexStringLower(bytes);
        }
        while (bindings.ContainsKey(token));

        return token;
    }
}
