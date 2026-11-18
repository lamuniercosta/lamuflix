using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LamuFlix.Evaluation.RealLibrary;

public sealed class BandSampleSelectorTests
{
    private const string GoldenFixtureRelative = "RealLibrary/Fixtures/band-sample-hash.golden.json";

    [Fact]
    public void EncodedText_uses_invariant_decimal_with_no_whitespace_or_newline()
    {
        using var culture = new CultureScope("pt-BR");

        BandSampleSelector.EncodedText(12, 34).ShouldBe("12:34");
        BandSampleSelector.EncodedText(100, 250).ShouldBe("100:250");
        BandSampleSelector.EncodedText(1, 2).ShouldNotContain(" ");
        BandSampleSelector.EncodedText(1, 2).ShouldNotContain("\n");
        BandSampleSelector.EncodedText(1, 2).ShouldNotContain("\r");
    }

    [Fact]
    public void Digest_matches_every_golden_vector_including_direction_reversal()
    {
        // arrange
        var golden = LoadGolden();

        // act / assert
        foreach (var vector in golden.Vectors)
        {
            var expected = vector.Digest;
            var actual = Convert.ToHexString(BandSampleSelector.Digest(vector.SelectedId, vector.OtherId))
                .ToLowerInvariant();

            actual.ShouldBe(expected, $"{vector.SelectedId}:{vector.OtherId}");
            BandSampleSelector.EncodedText(vector.SelectedId, vector.OtherId).ShouldBe(vector.Text);
        }

        var reversedDigests = golden.Vectors
            .Where(vector => vector.DirectionReversalOf is not null)
            .Select(vector => vector.Digest)
            .ToImmutableArray();

        reversedDigests.ShouldAllBe(digest => digest.Length == 64);
        reversedDigests.Distinct().Count().ShouldBe(reversedDigests.Length, "directions are independent");
    }

    [Fact]
    public void Select_orders_by_full_digest_bytes_lexicographically_not_by_ids()
    {
        // arrange — the golden vectors prove the digest order is not the numeric id order
        var golden = LoadGolden();
        var candidates = golden.Vectors.Select(vector => new DirectedPair(vector.SelectedId, vector.OtherId));

        // act
        var sample = BandSampleSelector.Select("[a,b)", 0.15, 0.20, candidates);

        // assert
        sample.Pairs.Select(pair => BandSampleSelector.EncodedText(pair.SelectedId, pair.OtherId))
            .ShouldBe(golden.DigestOrder);
        sample.AvailableCount.ShouldBe(golden.Vectors.Length);
        sample.Undersupplied.ShouldBeTrue("seven candidates is fewer than the twelve-maximum band");
    }

    [Fact]
    public void Select_is_stable_across_calls_and_across_enumerations()
    {
        // arrange
        var candidates = Enumerable.Range(1, 40)
            .SelectMany(selected => Enumerable.Range(1, 5).Select(other => new DirectedPair(selected, other)))
            .Where(pair => pair.SelectedId != pair.OtherId)
            .ToArray();

        // act
        var first = BandSampleSelector.Select("[0.2,0.25)", 0.2, 0.25, candidates);
        var second = BandSampleSelector.Select("[0.2,0.25)", 0.2, 0.25, candidates.Reverse());

        // assert
        second.Pairs.ShouldBe(first.Pairs);
        first.Pairs.Length.ShouldBe(BandSampleSelector.MaxSamplesPerBand);
        first.Undersupplied.ShouldBeFalse();
    }

    [Fact]
    public void Select_keeps_both_directions_of_the_same_movie_pair_as_distinct_samples()
    {
        // arrange
        var candidates = new[] { new DirectedPair(12, 34), new DirectedPair(34, 12) };

        // act
        var sample = BandSampleSelector.Select("[a,b)", 0.1, 0.2, candidates);

        // assert
        sample.AvailableCount.ShouldBe(2);
        sample.Pairs.Select(pair => BandSampleSelector.EncodedText(pair.SelectedId, pair.OtherId))
            .ShouldBe(["34:12", "12:34"]);
    }

    [Fact]
    public void Select_never_includes_a_self_pair_and_discloses_undersupply()
    {
        // arrange
        var candidates = new[] { new DirectedPair(5, 5), new DirectedPair(5, 6), new DirectedPair(5, 6) };

        // act
        var sample = BandSampleSelector.Select("[a,b)", 0.1, 0.2, candidates);

        // assert
        sample.Pairs.ShouldBe([new DirectedPair(5, 6)]);
        sample.AvailableCount.ShouldBe(1);
        sample.Undersupplied.ShouldBeTrue();
    }

    [Fact]
    public void Select_reports_zero_available_for_an_empty_band()
    {
        // act
        var sample = BandSampleSelector.Select("[a,b)", 0.1, 0.2, []);

        // assert
        sample.Pairs.ShouldBeEmpty();
        sample.AvailableCount.ShouldBe(0);
        sample.Undersupplied.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-2, 3)]
    public void Digest_rejects_non_positive_ids(int selectedId, int otherId)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => BandSampleSelector.Digest(selectedId, otherId));
    }

    private static Golden LoadGolden()
    {
        using var document = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, GoldenFixtureRelative)));
        var root = document.RootElement;

        var vectors = root.GetProperty("vectors")
            .EnumerateArray()
            .Select(element => new GoldenVector(
                element.GetProperty("selectedId").GetInt32(),
                element.GetProperty("otherId").GetInt32(),
                element.GetProperty("text").GetString() ?? throw new InvalidOperationException("missing text"),
                element.GetProperty("digest").GetString() ?? throw new InvalidOperationException("missing digest"),
                element.TryGetProperty("directionReversalOf", out var reversal) ? reversal.GetString() : null))
            .ToImmutableArray();

        var order = root.GetProperty("digestOrderOfTheSevenVectors")
            .EnumerateArray()
            .Select(element => element.GetString() ?? throw new InvalidOperationException("missing order entry"))
            .ToImmutableArray();

        return new Golden(vectors, order);
    }

    private sealed record Golden(ImmutableArray<GoldenVector> Vectors, ImmutableArray<string> DigestOrder);

    private sealed record GoldenVector(
        int SelectedId,
        int OtherId,
        string Text,
        string Digest,
        string? DirectionReversalOf);

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo prior = CultureInfo.CurrentCulture;

        public CultureScope(string name)
        {
            CultureInfo.CurrentCulture = new CultureInfo(name);
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = prior;
        }
    }
}
