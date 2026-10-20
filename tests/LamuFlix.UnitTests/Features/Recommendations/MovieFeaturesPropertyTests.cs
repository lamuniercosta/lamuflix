using System.Collections.Immutable;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Features.Recommendations;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class MovieFeaturesPropertyTests
{
    [Property(MaxTest = 100, Replay = "336001,100003")]
    [Trait("Category", "Property")]
    public bool Construction_PermutedCaseVariedDuplicatedSets_CompareEqualWithEqualHash(
        string[]? genres,
        string[]? directors,
        string[]? writers,
        string[]? cast)
    {
        // arrange
        var left = new MovieFeatures(Expand(genres), Expand(directors), Expand(writers), Expand(cast));
        var right = new MovieFeatures(Vary(genres), Vary(directors), Vary(writers), Vary(cast));

        // act & assert
        return left.Equals(right)
            && right.Equals(left)
            && left.GetHashCode() == right.GetHashCode();
    }

    [Property(MaxTest = 100, Replay = "336002,200003")]
    [Trait("Category", "Property")]
    public bool Construction_NullElementInEachSet_IsPreserved(
        string[]? genres,
        string[]? directors,
        string[]? writers,
        string[]? cast)
    {
        // arrange
        var features = new MovieFeatures(Expand(genres), Expand(directors), Expand(writers), Expand(cast));

        // act & assert
        return ContainsNull(features.Genres)
            && ContainsNull(features.Directors)
            && ContainsNull(features.Writers)
            && ContainsNull(features.Cast);
    }

    [Property(MaxTest = 100, Replay = "336003,300007")]
    [Trait("Category", "Property")]
    public bool Construction_RepeatedNullElements_CollapseToOneMemberInEachSet(
        string[]? genres,
        string[]? directors,
        string[]? writers,
        string[]? cast)
    {
        // arrange
        var features = new MovieFeatures(RepeatNull(genres), RepeatNull(directors), RepeatNull(writers), RepeatNull(cast));

        // act & assert
        return NullCount(features.Genres) == 1
            && NullCount(features.Directors) == 1
            && NullCount(features.Writers) == 1
            && NullCount(features.Cast) == 1;
    }

    private static string[] Expand(string[]? values) =>
        [.. (values ?? []), null!];

    private static string[] RepeatNull(string[]? values) =>
        [.. (values ?? []), null!, null!];

    private static string[] Vary(string[]? values)
    {
        var source = values ?? [];
        var combined = source
            .Concat(source.Reverse())
            .Append(null!)
            .Select(value => value is null ? null! : VaryAsciiCase(value));
        return [.. combined];
    }

    private static string VaryAsciiCase(string value) =>
        new([.. value.Select(VaryAsciiCase)]);

    private static char VaryAsciiCase(char value) =>
        value switch
        {
            >= 'a' and <= 'z' => (char)(value - 32),
            >= 'A' and <= 'Z' => (char)(value + 32),
            _ => value,
        };

    private static bool ContainsNull(ImmutableHashSet<string> values) =>
        NullCount(values) > 0;

    private static int NullCount(ImmutableHashSet<string> values) =>
        values.Count(value => value is null);
}
