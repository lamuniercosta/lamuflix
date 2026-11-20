using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Evaluation.TestSupport;

namespace LamuFlix.Evaluation.RealLibrary;

public sealed class QualityReportRunnerTests
{
    private static readonly ImmutableArray<double> SingleBar = [0.35];

    [Fact]
    public void Run_every_coverage_line_partitions_the_enriched_selected_population()
    {
        // act
        var report = Run(StrongCollectionSnapshot(), new RecommendationOptions());

        // assert
        report.EnrichedSelectedCount.ShouldBe(3);

        foreach (var line in report.Coverage)
        {
            line.Buckets.Total.ShouldBe(
                report.EnrichedSelectedCount,
                $"bar {line.Bar} rule {line.Rule} must partition every enriched selected movie");
        }
    }

    [Fact]
    public void Run_counts_only_enriched_movies_in_the_population()
    {
        // arrange
        var snapshot = StrongCollectionSnapshot(
            BuildMovie(4, "Pending", directors: ["Ada"], status: EnrichmentStatus.Pending));

        // act
        var report = Run(snapshot, new RecommendationOptions());

        // assert
        report.EnrichedSelectedCount.ShouldBe(3);
        report.Coverage.ShouldAllBe(line => line.Buckets.Total == 3);
    }

    [Fact]
    public void Run_emits_a_score_only_and_a_score_or_strong_line_for_every_bar()
    {
        // act
        var report = Run(StrongCollectionSnapshot(), new RecommendationOptions());

        // assert
        report.Coverage.Select(line => line.Rule).Distinct().Order(StringComparer.Ordinal)
            .ShouldBe([QualityReportRunner.ScoreOnlyRule, QualityReportRunner.ScoreOrStrongRule]);
        report.Coverage.Length.ShouldBe(report.Bars.Length * 2);
        report.Coverage.Select(line => line.Bar).Distinct().ShouldBe(report.Bars);
    }

    [Fact]
    public void Run_score_or_strong_line_adds_sub_bar_strong_pairs_the_score_only_line_misses()
    {
        // act
        var report = Run(StrongCollectionSnapshot(), new RecommendationOptions(), SingleBar);

        // assert
        var scoreOnly = Line(report, 0.35, QualityReportRunner.ScoreOnlyRule);
        var scoreOrStrong = Line(report, 0.35, QualityReportRunner.ScoreOrStrongRule);

        scoreOnly.Buckets.ShouldBe(new CoverageBuckets(Zero: 1, OneToThree: 2, FourToTen: 0, AboveTen: 0));
        scoreOrStrong.Buckets.ShouldBe(new CoverageBuckets(Zero: 0, OneToThree: 3, FourToTen: 0, AboveTen: 0));
        scoreOrStrong.MedianFirstScore.ShouldNotBeNull().ShouldBe(6.0 / 15.0, 1e-12);
        scoreOrStrong.FirstScoreCount.ShouldBe(3);
        scoreOnly.MedianFirstScore.ShouldNotBeNull().ShouldBe(6.0 / 15.0, 1e-12);
        scoreOnly.FirstScoreCount.ShouldBe(2);
    }

    [Fact]
    public void Run_reports_null_medians_with_zero_contributing_counts_when_nothing_qualifies()
    {
        // act
        var report = Run(StrongCollectionSnapshot(), new RecommendationOptions(), [1.5]);

        // assert
        var line = Line(report, 1.5, QualityReportRunner.ScoreOnlyRule);
        line.Buckets.Total.ShouldBe(3);
        line.Buckets.Zero.ShouldBe(3);
        line.MedianFirstScore.ShouldBeNull();
        line.FirstScoreCount.ShouldBe(0);
        line.MedianTenthScore.ShouldBeNull();
        line.TenthScoreCount.ShouldBe(0);
    }

    [Fact]
    public void Run_reports_a_tenth_ordinal_median_once_ten_results_qualify()
    {
        // arrange
        var movies = Enumerable.Range(1, 12)
            .Select(id => BuildMovie(id, "Movie " + id.ToString(CultureInfo.InvariantCulture), genres: ["Drama"]))
            .ToArray();
        var snapshot = Snapshot(movies, SharedPlotVectors(movies));

        // act
        var report = Run(snapshot, ZeroWeightOptions(), SingleBar, strongOnlyBar: 1.5);

        // assert
        var line = Line(report, 0.35, QualityReportRunner.ScoreOnlyRule);
        line.Buckets.Total.ShouldBe(12);
        line.Buckets.AboveTen.ShouldBe(12);
        line.FirstScoreCount.ShouldBe(12);
        line.MedianFirstScore.ShouldBe(1.0);
        line.TenthScoreCount.ShouldBe(12);
        line.MedianTenthScore.ShouldBe(1.0);
    }

    [Fact]
    public void Run_never_treats_the_identity_fallback_entry_as_strong()
    {
        // arrange
        var snapshot = Snapshot(
        [
            BuildMovie(1, "Alpha", genres: ["Drama"]),
            BuildMovie(2, "Bravo", genres: ["Drama"]),
            BuildMovie(3, "Charlie", genres: ["Drama"]),
        ]);

        // act
        var report = Run(snapshot, ZeroWeightOptions(), SingleBar, strongOnlyBar: 1.5);

        // assert
        var scoreOnly = Line(report, 0.35, QualityReportRunner.ScoreOnlyRule);
        var scoreOrStrong = Line(report, 0.35, QualityReportRunner.ScoreOrStrongRule);

        scoreOnly.Buckets.Total.ShouldBe(3, "the fallback score still counts in coverage");
        scoreOrStrong.Buckets.ShouldBe(scoreOnly.Buckets, "the fallback entry is never strong-eligible");
    }

    [Fact]
    public void Run_never_synthesizes_a_themes_criterion()
    {
        // act
        var report = Run(StrongCollectionSnapshot(), new RecommendationOptions());

        // assert
        QualityReportRunner.StrongCriteria.ShouldNotContain("Themes");
        QualityReportRunner.StrongCriteria.ShouldBe(["Collection", "Director"], ignoreOrder: true);
        report.Disclosures.ShouldContain(disclosure => disclosure.Contains("Themes", StringComparison.Ordinal));
    }

    [Fact]
    public void Run_builds_lower_inclusive_upper_exclusive_bands_plus_one_strong_only_band()
    {
        // act
        var report = Run(
            StrongCollectionSnapshot(),
            new RecommendationOptions(),
            [0.30, 0.36, 0.40],
            strongOnlyBar: 0.20);

        // assert
        report.Bands.Select(band => band.Label).ShouldBe(
            ["[0.3,0.36)", "[0.36,0.4)", BandSampleSelector.StrongOnlyBandLabel]);

        var band = report.Bands[0];
        band.LowerInclusive.ShouldBe(0.30);
        band.UpperExclusive.ShouldBe(0.36);
        band.AvailableCount.ShouldBe(2);
        band.Undersupplied.ShouldBeTrue();
        band.Pairs.ShouldBe([new DirectedPair(1, 2), new DirectedPair(2, 1)]);

        report.Bands[1].AvailableCount.ShouldBe(0);
        report.Bands[1].Pairs.ShouldBeEmpty();

        var strongOnly = report.Bands[2];
        strongOnly.UpperExclusive.ShouldBe(0.20);
        strongOnly.LowerInclusive.ShouldBeNull();
        strongOnly.AvailableCount.ShouldBe(0, "no strong pair scores below 0.20");
    }

    [Fact]
    public void Run_strong_only_band_collects_sub_bar_strong_pairs_only()
    {
        // act
        var report = Run(
            StrongCollectionSnapshot(),
            new RecommendationOptions(),
            [0.55, 0.65, 0.75],
            strongOnlyBar: 0.36);

        // assert
        var strongOnly = report.Bands.Single(band => band.Label == BandSampleSelector.StrongOnlyBandLabel);
        strongOnly.AvailableCount.ShouldBe(2);
        strongOnly.Pairs.ShouldBe([new DirectedPair(1, 2), new DirectedPair(2, 1)]);
    }

    [Fact]
    public void Run_repeats_identical_band_samples_on_unchanged_data()
    {
        // arrange
        var movies = Enumerable.Range(1, 20)
            .Select(id => BuildMovie(
                id,
                "Movie " + id.ToString(CultureInfo.InvariantCulture),
                genres: ["Drama"],
                collection: (7, "Franchise")))
            .ToArray();
        var snapshot = Snapshot(movies);

        // act
        var first = Run(snapshot, new RecommendationOptions(), [0.30, 0.36], strongOnlyBar: 0.1);
        var second = Run(snapshot, new RecommendationOptions(), [0.30, 0.36], strongOnlyBar: 0.1);

        // assert
        second.Bands.Select(band => band.Pairs).ShouldBe(first.Bands.Select(band => band.Pairs));
        first.Bands[0].Pairs.Length.ShouldBe(BandSampleSelector.MaxSamplesPerBand);
        first.Bands[0].Undersupplied.ShouldBeFalse();
    }

    [Fact]
    public void Run_computes_directed_good_and_bad_shares_over_valid_pairs_only()
    {
        // act
        var report = Run(StrongCollectionSnapshot(), new RecommendationOptions(), SingleBar, judged:
        [
            new JudgedPair(1, 3, JudgedVerdict.Good, "recovered known miss"),
            new JudgedPair(3, 1, JudgedVerdict.Good, null),
            new JudgedPair(1, 2, JudgedVerdict.Good, "strong but sub-bar"),
            new JudgedPair(2, 1, JudgedVerdict.Bad, "only a franchise sibling"),
        ]);

        // assert
        report.JudgedPairs.Valid.Length.ShouldBe(4);
        report.JudgedPairs.Skipped.ShouldBeEmpty();

        var good = report.JudgedPairs.Good;
        good.ValidCount.ShouldBe(3);
        good.RankOneShare.ShouldNotBeNull().ShouldBe(2.0 / 3.0, 1e-12);
        good.TopThreeShare.ShouldNotBeNull().ShouldBe(1.0, 1e-12);
        good.StrongShare.ShouldNotBeNull().ShouldBe(1.0, 1e-12);
        good.BarShares.ShouldHaveSingleItem().Bar.ShouldBe(0.35);
        good.BarShares[0].Share.ShouldNotBeNull().ShouldBe(2.0 / 3.0, 1e-12);

        var bad = report.JudgedPairs.Bad;
        bad.ValidCount.ShouldBe(1);
        bad.RankOneShare.ShouldBeNull("bad pairs never report a rank-1 share");
        bad.TopThreeShare.ShouldNotBeNull().ShouldBe(1.0, 1e-12);
        bad.StrongShare.ShouldNotBeNull().ShouldBe(1.0, 1e-12);
        bad.BarShares[0].Share.ShouldNotBeNull().ShouldBe(0.0, 1e-12);
    }

    [Fact]
    public void Run_skips_unknown_and_unenriched_ids_visibly_and_excludes_them_from_denominators()
    {
        // arrange
        var snapshot = StrongCollectionSnapshot(
            BuildMovie(4, "Pending", directors: ["Ada"], status: EnrichmentStatus.Pending));

        // act
        var report = Run(snapshot, new RecommendationOptions(), SingleBar, judged:
        [
            new JudgedPair(99, 1, JudgedVerdict.Good, "unknown selected"),
            new JudgedPair(1, 99, JudgedVerdict.Good, "unknown other"),
            new JudgedPair(1, 4, JudgedVerdict.Good, "unenriched other"),
            new JudgedPair(4, 1, JudgedVerdict.Bad, "unenriched selected"),
            new JudgedPair(1, 3, JudgedVerdict.Good, "valid"),
        ]);

        // assert
        report.JudgedPairs.Skipped.Length.ShouldBe(4);
        report.JudgedPairs.Skipped.ShouldAllBe(skipped => !string.IsNullOrWhiteSpace(skipped.Reason));
        report.JudgedPairs.Good.ValidCount.ShouldBe(1);
        report.JudgedPairs.Good.RankOneShare.ShouldNotBeNull().ShouldBe(1.0, 1e-12);
        report.JudgedPairs.Bad.ValidCount.ShouldBe(0);
    }

    [Fact]
    public void Run_zero_valid_denominators_give_null_shares_not_zero()
    {
        // act
        var report = Run(StrongCollectionSnapshot(), new RecommendationOptions(), SingleBar, judged:
        [
            new JudgedPair(99, 1, JudgedVerdict.Good, null),
        ]);

        // assert
        report.JudgedPairs.Valid.ShouldBeEmpty();
        report.JudgedPairs.Good.ValidCount.ShouldBe(0);
        report.JudgedPairs.Good.RankOneShare.ShouldBeNull();
        report.JudgedPairs.Good.TopThreeShare.ShouldBeNull();
        report.JudgedPairs.Good.StrongShare.ShouldBeNull();
        report.JudgedPairs.Good.BarShares.All(share => share.Share is null).ShouldBeTrue();
        report.JudgedPairs.Bad.RankOneShare.ShouldBeNull();
        report.JudgedPairs.Bad.TopThreeShare.ShouldBeNull();
        report.JudgedPairs.Bad.StrongShare.ShouldBeNull();
    }

    [Fact]
    public void Run_reports_each_valid_pairs_rank_score_and_bar_membership()
    {
        // act
        var report = Run(StrongCollectionSnapshot(), new RecommendationOptions(), SingleBar, judged:
        [
            new JudgedPair(1, 3, JudgedVerdict.Good, null),
            new JudgedPair(1, 2, JudgedVerdict.Good, null),
        ]);

        // assert
        var recovered = report.JudgedPairs.Valid.Single(outcome => outcome.Pair.OtherId == 3);
        recovered.Rank.ShouldBe(1);
        recovered.Score.ShouldBe(6.0 / 15.0, 1e-12);
        recovered.IsRankOne.ShouldBeTrue();
        recovered.BarsMet.ShouldBe([0.35]);

        var sibling = report.JudgedPairs.Valid.Single(outcome => outcome.Pair.OtherId == 2);
        sibling.Rank.ShouldBe(2);
        sibling.Score.ShouldBe(5.0 / 15.0, 1e-12);
        sibling.IsRankOne.ShouldBeFalse();
        sibling.IsTopThree.ShouldBeTrue();
        sibling.IsStrong.ShouldBeTrue("Collection value is 1.0 even though the score is sub-bar");
        sibling.BarsMet.ShouldBeEmpty();
    }

    [Fact]
    public void Run_reads_the_judged_pairs_file_from_the_configured_path()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("judged-pairs.json");
        File.WriteAllText(
            path,
            "[{\"movieId\":1,\"otherId\":3,\"verdict\":\"good\",\"note\":\"The Naked Gun -> Hot Shots!\"}]",
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        // act
        var report = QualityReportRunner.Run(
            StrongCollectionSnapshot(),
            new RecommendationOptions(),
            new QualityParameters(SingleBar, QualityReportRunner.DefaultStrongOnlyBar, path),
            TestContext.Current.CancellationToken);

        // assert
        report.JudgedPairs.Valid.ShouldHaveSingleItem().Pair.Note.ShouldBe("The Naked Gun -> Hot Shots!");
    }

    [Fact]
    public void Run_with_no_judged_pairs_file_discloses_empty_shares_instead_of_failing()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var parameters = new QualityParameters(
            SingleBar,
            QualityReportRunner.DefaultStrongOnlyBar,
            directory.Combine("absent.json"));

        // act
        var report = QualityReportRunner.Run(StrongCollectionSnapshot(), new RecommendationOptions(), parameters, TestContext.Current.CancellationToken);

        // assert
        report.JudgedPairs.Valid.ShouldBeEmpty();
        report.JudgedPairs.Good.ValidCount.ShouldBe(0);
    }

    [Fact]
    public void Run_observes_a_cancelled_token_instead_of_scoring()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var parameters = new QualityParameters(
            SingleBar,
            QualityReportRunner.DefaultStrongOnlyBar,
            directory.Combine("absent.json"));

        // act / assert
        Should.Throw<OperationCanceledException>(() =>
            QualityReportRunner.Run(
                StrongCollectionSnapshot(),
                new RecommendationOptions(),
                parameters,
                new CancellationToken(canceled: true)));
    }

    [Fact]
    public void Run_rejects_a_duplicate_directed_pair_without_partial_success()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("judged-pairs.json");
        File.WriteAllText(
            path,
            "[{\"movieId\":1,\"otherId\":2,\"verdict\":\"good\"},{\"movieId\":1,\"otherId\":2,\"verdict\":\"bad\"}]");
        var parameters = new QualityParameters(SingleBar, QualityReportRunner.DefaultStrongOnlyBar, path);

        // act / assert
        Should.Throw<JudgedPairsFormatException>(
            () => QualityReportRunner.Run(StrongCollectionSnapshot(), new RecommendationOptions(), parameters, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("[] trailing")]
    [InlineData("{}")]
    [InlineData("[{\"movieId\":1,\"otherId\":2}]")]
    [InlineData("[{\"movieId\":0,\"otherId\":2,\"verdict\":\"good\"}]")]
    [InlineData("[{\"movieId\":1,\"otherId\":\"2\",\"verdict\":\"good\"}]")]
    [InlineData("[{\"movieId\":1,\"otherId\":2,\"verdict\":\"maybe\"}]")]
    [InlineData("[{\"movieId\":1,\"otherId\":1,\"verdict\":\"good\"}]")]
    [InlineData("[{\"movieId\":1,\"otherId\":2,\"verdict\":\"good\",\"note\":5}]")]
    public void JudgedPairReader_rejects_malformed_entries(string json)
    {
        Should.Throw<JudgedPairsFormatException>(
            () => JudgedPairReader.ReadFromUtf8Bytes(Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void JudgedPairReader_treats_both_directions_as_independent_entries()
    {
        // act
        var pairs = JudgedPairReader.ReadFromUtf8Bytes(Encoding.UTF8.GetBytes(
            "[{\"movieId\":1,\"otherId\":2,\"verdict\":\"good\"},{\"movieId\":2,\"otherId\":1,\"verdict\":\"bad\"}]"));

        // assert
        pairs.Length.ShouldBe(2);
        pairs[0].Verdict.ShouldBe(JudgedVerdict.Good);
        pairs[1].Verdict.ShouldBe(JudgedVerdict.Bad);
    }

    [Fact]
    public void ReadParameters_defaults_bars_strong_only_bar_and_judged_pairs_path()
    {
        // act
        var parameters = QualityReportRunner.ReadParameters(_ => null);

        // assert
        parameters.Bars.ShouldBe([0.15, 0.20, 0.225, 0.25, 0.30, 0.35, 0.40]);
        parameters.StrongOnlyBar.ShouldBe(0.225);
        parameters.JudgedPairsPath.ShouldBe(
            Path.GetFullPath(Path.Combine(EvaluationPaths.Root, "artifacts", "recommendations", "judged-pairs.json")));
    }

    [Fact]
    public void ReadParameters_resolves_a_relative_judged_pairs_path_under_the_repository_root()
    {
        // arrange
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [QualityReportRunner.JudgedPairsPathVariable] = "artifacts/recommendations/custom-judged.json",
        };

        // act
        var parameters = QualityReportRunner.ReadParameters(name => environment.GetValueOrDefault(name));

        // assert
        parameters.JudgedPairsPath.ShouldBe(
            Path.GetFullPath(Path.Combine(EvaluationPaths.Root, "artifacts", "recommendations", "custom-judged.json")));
    }

    [Fact]
    public void ReadParameters_accepts_overrides_from_the_pinned_variable_names()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("mine.json");
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [QualityReportRunner.BarsVariable] = "0.10, 0.30 ,0.6",
            [QualityReportRunner.StrongOnlyBarVariable] = "0.275",
            [QualityReportRunner.JudgedPairsPathVariable] = path,
        };

        // act
        var parameters = QualityReportRunner.ReadParameters(name => environment.GetValueOrDefault(name));

        // assert
        parameters.Bars.ShouldBe([0.10, 0.30, 0.6]);
        parameters.StrongOnlyBar.ShouldBe(0.275);
        parameters.JudgedPairsPath.ShouldBe(Path.GetFullPath(path));
    }

    [Theory]
    [InlineData("0.15,abc")]
    [InlineData("0.15,NaN")]
    [InlineData("0.15,Infinity")]
    [InlineData("0.15,1e400")]
    [InlineData("0.20,0.15")]
    [InlineData("0.15,0.15")]
    [InlineData("-0.15,0.20")]
    [InlineData("0.15,,")]
    public void ReadParameters_rejects_nonfinite_non_positive_duplicate_or_descending_bars(string text)
    {
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [QualityReportRunner.BarsVariable] = text,
        };

        Should.Throw<InvalidOperationException>(
            () => QualityReportRunner.ReadParameters(name => environment.GetValueOrDefault(name)));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("0")]
    [InlineData("-0.225")]
    public void ReadParameters_rejects_a_nonfinite_or_non_positive_strong_only_bar(string text)
    {
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [QualityReportRunner.StrongOnlyBarVariable] = text,
        };

        Should.Throw<InvalidOperationException>(
            () => QualityReportRunner.ReadParameters(name => environment.GetValueOrDefault(name)));
    }

    [Fact]
    public void ReadParameters_parses_bars_with_the_invariant_culture()
    {
        // arrange
        using var culture = new CultureScope("pt-BR");
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [QualityReportRunner.BarsVariable] = "0.15,0.225",
        };

        // act
        var parameters = QualityReportRunner.ReadParameters(name => environment.GetValueOrDefault(name));

        // assert
        parameters.Bars.ShouldBe([0.15, 0.225]);
    }

    [Fact]
    public void SerializeToUtf8Bytes_is_byte_identical_for_repeated_runs()
    {
        // arrange
        var snapshot = StrongCollectionSnapshot();
        var parameters = new QualityParameters(
            SingleBar,
            QualityReportRunner.DefaultStrongOnlyBar,
            MissingJudgedPath());
        var report = QualityReportRunner.Run(snapshot, new RecommendationOptions(), parameters, TestContext.Current.CancellationToken);

        // act
        var first = QualityReportSerializer.SerializeToUtf8Bytes(report);
        var second = QualityReportSerializer.SerializeToUtf8Bytes(
            QualityReportRunner.Run(snapshot, new RecommendationOptions(), parameters, TestContext.Current.CancellationToken));

        // assert
        second.ShouldBe(first);

        var text = Encoding.UTF8.GetString(first);
        text.ShouldStartWith("{\"mode\":\"quality\",\"bars\":[0.35],\"strongOnlyBar\":0.225,");
        text.ShouldContain("\"judgedPairs\":{");
        var lowered = text.ToLowerInvariant();
        lowered.ShouldNotContain("timestamp");
        lowered.ShouldNotContain("hostname");
    }

    [Fact]
    public void SerializeToUtf8Bytes_writes_null_for_unavailable_medians_and_shares()
    {
        // act
        var text = Encoding.UTF8.GetString(QualityReportSerializer.SerializeToUtf8Bytes(
            QualityReportRunner.Run(
                StrongCollectionSnapshot(),
                new RecommendationOptions(),
                new QualityParameters(
                    SingleBar,
                    QualityReportRunner.DefaultStrongOnlyBar,
                    MissingJudgedPath()),
                TestContext.Current.CancellationToken)));

        // assert
        text.ShouldContain("\"medianTenthScore\":null");
        text.ShouldContain("\"rankOneShare\":null");
        text.ShouldContain("\"undersupplied\":true");
        JsonDocument.Parse(text).RootElement.GetProperty("mode").GetString().ShouldBe("quality");
    }

    private static CoverageLine Line(QualityReport report, double bar, string rule)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator // the test passes in the exact bar it looks up
        return report.Coverage.Single(line => line.Bar == bar && line.Rule == rule);
    }

    private static string MissingJudgedPath() =>
        Path.Combine(
            Path.GetTempPath(),
            "lamuflix-quality-tests",
            Guid.NewGuid().ToString("N") + "-absent.json");

    private static QualityReport Run(
        RealLibrarySnapshot snapshot,
        RecommendationOptions options,
        ImmutableArray<double>? bars = null,
        double strongOnlyBar = QualityReportRunner.DefaultStrongOnlyBar,
        ImmutableArray<JudgedPair> judged = default)
    {
        var resolvedBars = bars is { IsDefault: false } given ? given : QualityReportRunner.DefaultBars;

        return QualityReportRunner.Run(
            snapshot,
            options,
            new QualityParameters(resolvedBars, strongOnlyBar, WriteJudged(judged)));
    }

    private static string WriteJudged(ImmutableArray<JudgedPair> judged)
    {
        if (judged.IsDefault)
        {
            return MissingJudgedPath();
        }

        var directory = Path.Combine(Path.GetTempPath(), "lamuflix-quality-tests");
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".json");
        var json = new StringBuilder("[");
        for (var index = 0; index < judged.Length; index++)
        {
            var pair = judged[index];
            if (index > 0)
            {
                json.Append(',');
            }

            json.Append('{')
                .Append("\"movieId\":").Append(pair.MovieId.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append("\"otherId\":").Append(pair.OtherId.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append("\"verdict\":\"").Append(pair.Verdict == JudgedVerdict.Good ? "good" : "bad").Append('"');

            if (pair.Note is { } note)
            {
                json.Append(",\"note\":").Append(JsonSerializer.Serialize(note));
            }

            json.Append('}');
        }

        json.Append(']');
        File.WriteAllText(path, json.ToString(), new UTF8Encoding(false));

        return path;
    }

    private static RecommendationOptions ZeroWeightOptions() =>
        new()
        {
            Weights = new SimilarityWeights
            {
                Director = 0.0,
                Themes = 0.0,
                Plot = 1.0,
                Cast = 0.0,
                Genres = 0.0,
                Collection = 0.0,
                ReviewPlotWeight = 1.0,
            },
        };

    private static RealLibrarySnapshot StrongCollectionSnapshot(params Movie[] extra)
    {
        Movie[] movies =
        [
            BuildMovie(1, "Alpha", genres: ["Drama"], directors: ["Ada"], cast: ["Ann"], collection: (7, "Franchise")),
            BuildMovie(2, "Bravo", genres: ["Drama"], directors: ["Bob"], collection: (7, "Franchise")),
            BuildMovie(3, "Charlie", genres: ["Drama"], directors: ["Ada"], cast: ["Ann"]),
            .. extra,
        ];

        return Snapshot(movies);
    }

    private static ImmutableDictionary<MovieId, ImmutableArray<float>> SharedPlotVectors(Movie[] movies) =>
        movies.ToImmutableDictionary(
            movie => movie.Id,
            _ => ImmutableArray.Create(1.0f, 0.0f));

    private static RealLibrarySnapshot Snapshot(
        Movie[] movies,
        ImmutableDictionary<MovieId, ImmutableArray<float>>? plotEmbeddings = null)
    {
        var genres = movies
            .SelectMany(movie => movie.Metadata?.Genres ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(genre => genre, _ => (long)movies.Length, StringComparer.OrdinalIgnoreCase);

        return new RealLibrarySnapshot(
            [.. movies],
            [],
            plotEmbeddings ?? ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty,
            new GenreCounts(movies.Length, genres));
    }

    private static Movie BuildMovie(
        int id,
        string title,
        ImmutableArray<string> genres = default,
        ImmutableArray<string> directors = default,
        ImmutableArray<string> cast = default,
        (int Id, string Name)? collection = null,
        EnrichmentStatus? status = null) =>
        Movie.Rehydrate(
            new MovieId(id),
            title,
            new LibraryPath($"C:/library/{id}.mkv"),
            new MediaFormat("mkv"),
            isInWatchlist: false,
            metadata: new MovieMetadata(
                title,
                directors: directors.IsDefault ? [] : directors,
                actors: cast.IsDefault ? [] : cast,
                genres: genres.IsDefault ? ["Drama"] : genres,
                collectionId: collection?.Id,
                collectionName: collection?.Name,
                signals: collection is null
                    ? null
                    : MovieSignalSnapshot.FromPersisted([], [], collectionAvailable: true)),
            status: status ?? EnrichmentStatus.Enriched,
            enrichedAt: null,
            enrichmentAttempts: 0,
            lastFailureCategory: null,
            lastAttemptAt: null);

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
