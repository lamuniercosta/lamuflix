using System;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Features.Subtitles;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleMatchScorerPropertyTests
{
    private static readonly string[] Vocabulary =
    [
        "dune", "2021", "1080p", "bluray", "x264", "group", "part", "web", "dl", "h264",
    ];

    [Property(MaxTest = 200)]
    [Trait("Category", "Property")]
    public bool Score_AlwaysStaysWithinThePercentageBounds(int leftSeed, int rightSeed, bool matchesMovieHash)
    {
        // arrange
        var video = Compose(leftSeed);
        var release = Compose(rightSeed);

        // act
        var score = SubtitleMatchScorer.Score(video, release, matchesMovieHash);

        // assert
        return score is >= 0 and <= 100;
    }

    [Property(MaxTest = 200)]
    [Trait("Category", "Property")]
    public bool Score_MovieHashAlwaysOverridesToExactlyOneHundred(int leftSeed, int rightSeed)
    {
        // arrange
        var video = Compose(leftSeed);
        var release = Compose(rightSeed);

        // act
        var score = SubtitleMatchScorer.Score(video, release, matchesMovieHash: true);

        // assert
        return score == SubtitleMatchScorer.MovieHashMatchPercent;
    }

    [Property(MaxTest = 200)]
    [Trait("Category", "Property")]
    public bool Score_WithoutHashIsSymmetricInItsTwoInputs(int leftSeed, int rightSeed)
    {
        // arrange
        var left = Compose(leftSeed);
        var right = Compose(rightSeed);

        // act
        var forward = SubtitleMatchScorer.Score(left, right, matchesMovieHash: false);
        var reversed = SubtitleMatchScorer.Score(right, left, matchesMovieHash: false);

        // assert
        return forward == reversed;
    }

    [Property(MaxTest = 200)]
    [Trait("Category", "Property")]
    public bool Score_IdenticalInputsWithoutHash_ReachExactlyOneHundred(int seed)
    {
        // arrange
        var value = Compose(seed);

        // act
        var score = SubtitleMatchScorer.Score(value, value, matchesMovieHash: false);

        // assert
        return score == 100;
    }

    [Property(MaxTest = 200)]
    [Trait("Category", "Property")]
    public bool Score_EmptyUnionIsAlwaysZero(int seed)
    {
        // arrange
        var video = Compose(seed);

        // act
        var score = SubtitleMatchScorer.Score(video, "---", matchesMovieHash: false);

        // assert
        return score == SubtitleMatchScorer.NoOverlapPercent;
    }

    [Property(MaxTest = 200)]
    [Trait("Category", "Property")]
    public bool Ordering_IsDeterministicAcrossRepeatedComparisons(int leftSeed, int rightSeed, int idSeed)
    {
        // arrange
        var left = Compose(leftSeed);
        var right = Compose(rightSeed);
        var leftId = idSeed.ToString("x8", System.Globalization.CultureInfo.InvariantCulture);
        var rightId = (idSeed ^ 0x5555).ToString("x8", System.Globalization.CultureInfo.InvariantCulture);

        // act
        var first = SubtitleMatchScorer.Compare(left, leftId, right, rightId);
        var second = SubtitleMatchScorer.Compare(left, leftId, right, rightId);
        var reversed = SubtitleMatchScorer.Compare(right, rightId, left, leftId);

        // assert
        return first == second && Math.Sign(first) == -Math.Sign(reversed);
    }

    [Property(MaxTest = 200)]
    [Trait("Category", "Property")]
    public bool DistinctTokens_ContainsOnlyDistinctNonBlankAlphanumericTokens(int seed)
    {
        // arrange
        var value = Compose(seed);

        // act
        var tokens = SubtitleMatchScorer.DistinctTokens(value);

        // assert
        return tokens.All(token => token.Length > 0 && token.All(char.IsLetterOrDigit))
            && tokens.Count == tokens.Distinct(StringComparer.Ordinal).Count();
    }

    private static string Compose(int seed)
    {
        var state = (uint)(seed | 1);
        var count = (int)(state % 5) + 1;
        var tokens = new string[count];
        for (var index = 0; index < count; index++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            tokens[index] = Vocabulary[state % Vocabulary.Length];
        }

        return string.Join('.', tokens);
    }
}
