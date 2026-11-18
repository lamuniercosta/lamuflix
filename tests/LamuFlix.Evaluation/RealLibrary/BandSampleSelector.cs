using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LamuFlix.Evaluation.RealLibrary;

/// <summary>
/// Deterministic band sampling for the quality report. A band is a directed, non-self pair of
/// enriched movies; pairs are ordered by the full bytes of the SHA-256 digest of the UTF-8 invariant
/// decimal text <c>selectedId:otherId</c> (no whitespace, no newline), then by ascending ids, so the
/// same unchanged library always yields the same sample regardless of process, culture or enumeration
/// order. Undersupplied bands are disclosed with their real available count rather than padded.
/// </summary>
public static class BandSampleSelector
{
    public const int MaxSamplesPerBand = 12;
    public const string StrongOnlyBandLabel = "strong-only";

    private static readonly DirectedPairComparer DigestOrder = new();

    /// <summary>
    /// The pinned directed encoding digest: SHA-256 over UTF-8 of <c>selectedId + ":" + otherId</c>
    /// using invariant decimal text with no whitespace and no trailing newline.
    /// </summary>
    public static byte[] Digest(int selectedId, int otherId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(selectedId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(otherId);

        return SHA256.HashData(Encoding.UTF8.GetBytes(EncodedText(selectedId, otherId)));
    }

    public static string EncodedText(int selectedId, int otherId) =>
        selectedId.ToString(CultureInfo.InvariantCulture)
        + ":"
        + otherId.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Selects up to <see cref="MaxSamplesPerBand"/> pairs from the already-qualified candidates of one
    /// band, in digest order, and reports the qualified count so undersupply is visible.
    /// </summary>
    public static BandSample Select(
        string label,
        double? lowerInclusive,
        double? upperExclusive,
        IEnumerable<DirectedPair> qualifiedCandidates)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(qualifiedCandidates);

        var qualified = qualifiedCandidates
            .Where(pair => pair.SelectedId != pair.OtherId)
            .Distinct()
            .ToImmutableArray();

        var pairs = qualified
            .OrderBy(pair => pair, DigestOrder)
            .Take(MaxSamplesPerBand)
            .ToImmutableArray();

        return new BandSample(
            label,
            lowerInclusive,
            upperExclusive,
            qualified.Length,
            pairs,
            Undersupplied: qualified.Length < MaxSamplesPerBand);
    }

    private sealed class DirectedPairComparer : IComparer<DirectedPair>
    {
        public int Compare(DirectedPair? x, DirectedPair? y)
        {
            if (x is null || y is null)
            {
                return x is null ? (y is null ? 0 : -1) : 1;
            }

            var left = Digest(x.SelectedId, x.OtherId);
            var right = Digest(y.SelectedId, y.OtherId);
            var byDigest = left.AsSpan().SequenceCompareTo(right.AsSpan());

            if (byDigest != 0)
            {
                return byDigest;
            }

            var bySelected = x.SelectedId.CompareTo(y.SelectedId);

            return bySelected != 0 ? bySelected : x.OtherId.CompareTo(y.OtherId);
        }
    }
}

public sealed record DirectedPair(int SelectedId, int OtherId);

// ReSharper disable NotAccessedPositionalProperty.Global // report contract consumed by tests and operators
public sealed record BandSample(
    string Label,
    double? LowerInclusive,
    double? UpperExclusive,
    int AvailableCount,
    ImmutableArray<DirectedPair> Pairs,
    bool Undersupplied);
// ReSharper restore NotAccessedPositionalProperty.Global
