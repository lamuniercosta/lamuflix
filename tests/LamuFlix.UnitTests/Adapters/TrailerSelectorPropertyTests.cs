using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using TrailerCandidate = LamuFlix.Infrastructure.Adapters.TmdbMetadataProvider.TrailerCandidate;
using TrailerSelector = LamuFlix.Infrastructure.Adapters.TmdbMetadataProvider.TrailerSelector;

namespace LamuFlix.UnitTests.Adapters;

public sealed class TrailerSelectorPropertyTests
{
    private const string English = "en";
    private const string Portuguese = "pt";
    private const string Brazil = "BR";
    private const string UnitedStates = "US";
    private const string YouTube = "YouTube";
    private const string Trailer = "Trailer";
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-";
    private const int KeyLength = 11;

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Select_CandidatePermutation_AlwaysReturnsTheSameKey(int[] order, int seed)
    {
        var candidates = Candidates(seed);
        if (candidates.Length == 0)
        {
            return true;
        }

        var permuted = Permute(candidates, order);
        var expected = TrailerSelector.Select(candidates, English);
        var actual = TrailerSelector.Select(permuted, English);

        return string.Equals(expected, actual, StringComparison.Ordinal);
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Select_DuplicatingEveryCandidate_DoesNotChangeTheSelectedKey(int seed)
    {
        var candidates = Candidates(seed);
        var duplicated = candidates.Concat(candidates).ToArray();

        var expected = TrailerSelector.Select(candidates, English);
        var actual = TrailerSelector.Select(duplicated, English);

        return string.Equals(expected, actual, StringComparison.Ordinal);
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Select_RepeatingTheSelection_IsIdempotent(int seed)
    {
        var candidates = Candidates(seed);

        var first = TrailerSelector.Select(candidates, English);
        var second = TrailerSelector.Select(candidates, English);

        return string.Equals(first, second, StringComparison.Ordinal);
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Select_EveryReturnedKey_IsAValidTrailerKeyOrNull(int seed)
    {
        var selected = TrailerSelector.Select(Candidates(seed), English);

        return selected is null || MovieMetadata.IsValidTrailerKey(selected);
    }

    private static string RandomKey(Random random)
    {
        var characters = new char[KeyLength];
        for (var index = 0; index < characters.Length; index++)
        {
            characters[index] = Alphabet[random.Next(Alphabet.Length)];
        }

        return new string(characters);
    }

    private static TrailerCandidate[] Permute(TrailerCandidate[] candidates, int[] order)
    {
        if (order.Length == 0)
        {
            return [.. candidates.Reverse()];
        }

        return [.. candidates
            .Select((candidate, index) => (Candidate: candidate, Rank: order[index % order.Length]))
            .OrderBy(pair => pair.Rank)
            .ThenBy(pair => pair.Candidate.Key, StringComparer.Ordinal)
            .Select(pair => pair.Candidate)];
    }

    private static TrailerCandidate[] Candidates(int seed)
    {
        var random = new Random(seed);
        var count = random.Next(0, 10);
        var candidates = new List<TrailerCandidate>();
        for (var index = 0; index < count; index++)
        {
            candidates.Add(new TrailerCandidate(
                RandomKey(random),
                YouTube,
                Trailer,
                random.Next(2) == 0,
                random.Next(2) == 0 ? Portuguese : English,
                random.Next(2) == 0 ? Brazil : UnitedStates));
        }

        return [.. candidates];
    }
}
