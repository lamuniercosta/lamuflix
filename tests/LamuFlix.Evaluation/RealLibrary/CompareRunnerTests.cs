using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.Evaluation.RealLibrary;

public sealed class CompareRunnerTests
{
    private static readonly RecommendationOptions Options = new();

    [Fact]
    public void Run_scores_and_retains_every_other_enriched_movie_ordered_by_score_then_id()
    {
        // arrange
        var snapshot = Snapshot(
            Movies(
                BuildMovie(1, "Selected", genres: ["Drama"], directors: ["Ada"]),
                BuildMovie(2, "Same profile", genres: ["Drama"], directors: ["Ada"]),
                BuildMovie(3, "Partial profile", genres: ["Drama"]),
                BuildMovie(4, "No profile", genres: ["Comedy"])),
            Vectors((1, Vector(1.0)), (2, Vector(1.0)), (3, Vector(0.5)), (4, Vector(-1.0))));

        // act
        var report = CompareRunner.Run(snapshot, Options, 1, otherId: null, TestContext.Current.CancellationToken);

        // assert
        var rows = report.Envelope.Lists.ShouldHaveSingleItem().Rows;
        rows.Select(row => row.MovieId).ShouldBe([2, 3, 4]);
        rows.Select(row => row.Rank).ShouldBe([1, 2, 3]);
        rows[0].Score.ShouldBeGreaterThanOrEqualTo(rows[1].Score);
        rows[1].Score.ShouldBeGreaterThanOrEqualTo(rows[2].Score);
        report.IsPairInspect.ShouldBeFalse();
        report.SelectedVectorUsable.ShouldBeTrue();
    }

    [Fact]
    public void Run_never_drops_an_enriched_row_whose_vector_is_missing_and_emits_null_plot_diagnostics()
    {
        // arrange
        var snapshot = Snapshot(
            Movies(
                BuildMovie(1, "Selected", genres: ["Drama"]),
                BuildMovie(2, "With vector", genres: ["Drama"]),
                BuildMovie(3, "Missing vector", genres: ["Drama"])),
            Vectors((1, Vector(1.0)), (2, Vector(1.0))));

        // act
        var report = CompareRunner.Run(snapshot, Options, 1, otherId: null, TestContext.Current.CancellationToken);

        // assert
        var rows = report.Envelope.Lists[0].Rows;
        rows.Select(row => row.MovieId).ShouldBe([2, 3]);
        var missing = rows.Single(row => row.MovieId == 3).Diagnostics;
        missing.ShouldNotBeNull();
        missing.RawPlotCosine.ShouldBeNull();
        missing.PlotRank.ShouldBeNull();
        missing.InPool.ShouldBeFalse();
    }

    [Fact]
    public void Run_keeps_the_enriched_row_for_wrong_dimension_nonfinite_and_zero_norm_vectors()
    {
        // arrange
        var snapshot = Snapshot(
            Movies(
                BuildMovie(1, "Selected", genres: ["Drama"]),
                BuildMovie(2, "Wrong dimension", genres: ["Drama"]),
                BuildMovie(3, "Nonfinite", genres: ["Drama"]),
                BuildMovie(4, "Zero norm", genres: ["Drama"])),
            Vectors((1, Vector(1.0)), (2, [1f, 2f]), (3, WithNonfinite()), (4, new float[EmbeddingDimensions.Value])));

        // act
        var report = CompareRunner.Run(snapshot, Options, 1, otherId: null, TestContext.Current.CancellationToken);

        // assert
        var rows = report.Envelope.Lists[0].Rows;
        rows.Select(row => row.MovieId).ShouldBe([2, 3, 4]);

        foreach (var row in rows)
        {
            row.Diagnostics.ShouldNotBeNull();
            row.Diagnostics.RawPlotCosine.ShouldBeNull();
            row.Diagnostics.PlotRank.ShouldBeNull();
        }
    }

    private static RecommendationPairDiagnostics Diagnostics(RecommendationArtifactRow row) =>
        row.Diagnostics.ShouldNotBeNull();

    [Fact]
    public void Run_ranks_usable_pairs_by_cosine_descending_then_id_ascending()
    {
        // arrange
        var snapshot = Snapshot(
            Movies(
                BuildMovie(1, "Selected", genres: ["Drama"]),
                BuildMovie(2, "Tie high id", genres: ["Drama"]),
                BuildMovie(3, "Tie low id", genres: ["Drama"]),
                BuildMovie(4, "Lower cosine", genres: ["Drama"])),
            Vectors((1, Vector(1.0)), (2, Vector(0.5)), (3, Vector(0.5)), (4, Vector(-1.0))));

        // act
        var report = CompareRunner.Run(snapshot, Options, 1, otherId: null, TestContext.Current.CancellationToken);

        // assert
        var rows = report.Envelope.Lists[0].Rows;
        var byId = rows.ToDictionary(row => row.MovieId);
        Diagnostics(byId[2]).PlotRank.ShouldBe(1);
        Diagnostics(byId[3]).PlotRank.ShouldBe(2);
        Diagnostics(byId[4]).PlotRank.ShouldBe(3);
        Diagnostics(byId[2]).RawPlotCosine.ShouldNotBeNull().ShouldBe(
            Diagnostics(byId[3]).RawPlotCosine.ShouldNotBeNull(),
            1e-6);
    }

    [Fact]
    public void Run_marks_ticket_pool_membership_by_plot_rank_or_same_collection()
    {
        // arrange
        var options = Options with { CandidatePoolSize = 1, Take = 1 };
        var snapshot = Snapshot(
            Movies(
                BuildMovie(1, "Selected", genres: ["Drama"], collection: (7, "Franchise")),
                BuildMovie(2, "Best plot", genres: ["Drama"]),
                BuildMovie(3, "Franchise member", genres: ["Drama"], collection: (7, "Franchise")),
                BuildMovie(4, "Out of pool", genres: ["Drama"])),
            Vectors((1, Vector(1.0)), (2, Vector(1.0)), (3, Vector(-1.0)), (4, Vector(0.0))));

        // act
        var report = CompareRunner.Run(snapshot, options, 1, otherId: null, TestContext.Current.CancellationToken);

        // assert
        var byId = report.Envelope.Lists[0].Rows.ToDictionary(row => row.MovieId);
        Diagnostics(byId[2]).InPool.ShouldBeTrue("plot rank 1 fits a pool of 1");
        Diagnostics(byId[3]).InPool.ShouldBeTrue("same collection satisfies the ticket formula");
        Diagnostics(byId[4]).InPool.ShouldBeFalse();
    }

    [Fact]
    public void Run_with_no_usable_selected_vector_emits_all_null_plot_ranks_and_discloses_it()
    {
        // arrange
        var snapshot = Snapshot(
            Movies(BuildMovie(1, "Selected", genres: ["Drama"]), BuildMovie(2, "Other", genres: ["Drama"])),
            Vectors((2, Vector(1.0))));

        // act
        var report = CompareRunner.Run(snapshot, Options, 1, otherId: null, TestContext.Current.CancellationToken);

        // assert
        report.SelectedVectorUsable.ShouldBeFalse();

        foreach (var row in report.Envelope.Lists[0].Rows)
        {
            row.Diagnostics.ShouldNotBeNull();
            row.Diagnostics.RawPlotCosine.ShouldBeNull();
            row.Diagnostics.PlotRank.ShouldBeNull();
        }

        report.Disclosures.ShouldContain(disclosure => disclosure.Contains("no usable persisted plot vector", StringComparison.Ordinal));
    }

    [Fact]
    public void Run_always_discloses_the_ticket_pool_formula()
    {
        // arrange
        var snapshot = Snapshot(
            Movies(BuildMovie(1, "Selected", genres: ["Drama"]), BuildMovie(2, "Other", genres: ["Drama"])),
            Vectors((1, Vector(1.0)), (2, Vector(0.5))));

        // act
        var report = CompareRunner.Run(snapshot, Options, 1, otherId: null, TestContext.Current.CancellationToken);

        // assert
        report.Disclosures.ShouldContain(disclosure => disclosure.Contains("ticket diagnostic formula", StringComparison.Ordinal));
    }

    [Fact]
    public void Run_pair_inspect_keeps_only_the_directed_pair_and_reports_matched_names()
    {
        // arrange
        var snapshot = Snapshot(
            Movies(
                BuildMovie(1, "Selected", genres: ["Drama"], directors: ["Ada"], cast: ["Ann"], keywords: [(9, "heist")], collection: (4, "Franchise")),
                BuildMovie(2, "Other", genres: ["Drama"], directors: ["Ada"], cast: ["Ann"], keywords: [(9, "heist")], collection: (4, "Franchise")),
                BuildMovie(3, "Third", genres: ["Comedy"])),
            Vectors((1, Vector(1.0)), (2, Vector(0.5)), (3, Vector(0.0))));

        // act
        var report = CompareRunner.Run(snapshot, Options, 1, otherId: 2, TestContext.Current.CancellationToken);

        // assert
        var rows = report.Envelope.Lists[0].Rows;
        rows.ShouldHaveSingleItem().MovieId.ShouldBe(2);
        report.IsPairInspect.ShouldBeTrue();
        report.Envelope.Targets.ShouldBe([1, 2]);

        report.MatchedNames.ShouldContain(new RecommendationMatchedName("Director", "Ada"));
        report.MatchedNames.ShouldContain(new RecommendationMatchedName("Cast", "Ann"));
        report.MatchedNames.ShouldContain(new RecommendationMatchedName("Genre", "Drama"));
        report.MatchedNames.ShouldContain(new RecommendationMatchedName("Keyword", "heist"));
        report.MatchedNames.ShouldContain(new RecommendationMatchedName("Collection", "Franchise"));
        report.MatchedNames.ShouldNotContain(name => name.Criterion == "Writer");
    }

    [Fact]
    public void Run_pair_diagnostic_contributions_sum_to_the_pair_score()
    {
        // arrange
        var snapshot = Snapshot(
            Movies(
                BuildMovie(1, "Selected", genres: ["Drama"], directors: ["Ada"]),
                BuildMovie(2, "Other", genres: ["Drama"], directors: ["Ada"])),
            Vectors((1, Vector(1.0)), (2, Vector(0.5))));

        // act
        var report = CompareRunner.Run(snapshot, Options, 1, otherId: 2, TestContext.Current.CancellationToken);

        // assert
        var row = report.Envelope.Lists[0].Rows[0];
        row.Diagnostics.ShouldNotBeNull();
        row.Diagnostics.Breakdown.ShouldNotBeEmpty();
        row.Diagnostics.Breakdown.Sum(criterion => criterion.Contribution).ShouldBe(row.Score, 1e-9);
    }

    [Fact]
    public void Run_pair_inspect_rejects_an_unknown_or_non_enriched_other_id()
    {
        // arrange
        var snapshot = Snapshot(
            Movies(
                BuildMovie(1, "Selected", genres: ["Drama"]),
                BuildMovie(2, "Pending", genres: ["Drama"], status: EnrichmentStatus.Pending)),
            Vectors((1, Vector(1.0)), (2, Vector(0.5))));

        // act / assert
        Should.Throw<InvalidOperationException>(() => CompareRunner.Run(snapshot, Options, 1, otherId: 2, TestContext.Current.CancellationToken));
        Should.Throw<InvalidOperationException>(() => CompareRunner.Run(snapshot, Options, 1, otherId: 99, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Run_rejects_a_non_positive_selected_id(int selectedId)
    {
        // arrange
        var snapshot = Snapshot(
            Movies(BuildMovie(1, "Selected", genres: ["Drama"]), BuildMovie(2, "Other", genres: ["Drama"])),
            Vectors((1, Vector(1.0)), (2, Vector(0.5))));

        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(() => CompareRunner.Run(snapshot, Options, selectedId, null));
    }

    [Fact]
    public void Run_rejects_an_other_id_equal_to_the_selected_id()
    {
        // arrange
        var snapshot = Snapshot(
            Movies(BuildMovie(1, "Selected", genres: ["Drama"]), BuildMovie(2, "Other", genres: ["Drama"])),
            Vectors((1, Vector(1.0)), (2, Vector(0.5))));

        // act / assert
        Should.Throw<ArgumentException>(() => CompareRunner.Run(snapshot, Options, 1, otherId: 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Run_artifact_is_diff_compatible_and_round_trips_through_the_serializer()
    {
        // arrange
        var snapshot = Snapshot(
            Movies(
                BuildMovie(1, "Selected", genres: ["Drama"], directors: ["Ada"]),
                BuildMovie(2, "Other", genres: ["Drama"], directors: ["Ada"])),
            Vectors((1, Vector(1.0)), (2, Vector(0.5))));

        // act
        var report = CompareRunner.Run(snapshot, Options, 1, otherId: null, TestContext.Current.CancellationToken);
        var bytes = RecommendationArtifactSerializer.SerializeToUtf8Bytes(report.Envelope);
        var parsed = RecommendationArtifactSerializer.ReadFromUtf8Bytes(bytes);

        // assert
        parsed.Mode.ShouldBe(RecommendationArtifactContract.CompareMode);
        parsed.Lists[0].Kind.ShouldBe(RecommendationArtifactContract.CompareListKind);
        parsed.Lists[0].TargetId.ShouldBe(1);
        parsed.Lists[0].Rows.Length.ShouldBe(1);
        RecommendationArtifactSerializer.SerializeToUtf8Bytes(parsed).ShouldBe(bytes);
    }

    [Fact]
    public void Run_serializes_pair_matched_names_inside_the_list()
    {
        // arrange
        var snapshot = Snapshot(
            Movies(
                BuildMovie(1, "Selected", genres: ["Drama"], directors: ["Ada"]),
                BuildMovie(2, "Other", genres: ["Drama"], directors: ["Ada"])),
            Vectors((1, Vector(1.0)), (2, Vector(0.5))));

        // act
        var report = CompareRunner.Run(snapshot, Options, 1, otherId: 2, TestContext.Current.CancellationToken);
        var text = System.Text.Encoding.UTF8.GetString(
            RecommendationArtifactSerializer.SerializeToUtf8Bytes(report.Envelope));

        // assert
        text.ShouldContain("\"matchedNames\":[");
        text.ShouldContain("{\"criterion\":\"Director\",\"name\":\"Ada\"}");
    }

    [Fact]
    public void Run_observes_a_cancelled_token_instead_of_scoring()
    {
        // arrange
        var snapshot = Snapshot(
            Movies(
                BuildMovie(1, "Selected", genres: ["Drama"]),
                BuildMovie(2, "Other", genres: ["Drama"])),
            Vectors((1, Vector(1.0)), (2, Vector(0.5))));

        // act / assert
        Should.Throw<OperationCanceledException>(() =>
            CompareRunner.Run(snapshot, Options, 1, otherId: null, new CancellationToken(canceled: true)));
    }

    private static RealLibrarySnapshot Snapshot(
        ImmutableArray<Movie> movies,
        ImmutableDictionary<MovieId, ImmutableArray<float>> vectors)
    {
        var genres = movies
            .SelectMany(movie => movie.Metadata?.Genres ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(genre => genre, _ => (long)movies.Length, StringComparer.OrdinalIgnoreCase);

        return new RealLibrarySnapshot(
            movies,
            [],
            vectors,
            new GenreCounts(movies.Length, genres));
    }

    private static ImmutableArray<Movie> Movies(params Movie[] movies) => [.. movies];

    private static ImmutableDictionary<MovieId, ImmutableArray<float>> Vectors(
        params (int Id, float[] Vector)[] vectors) =>
        vectors.ToImmutableDictionary(
            entry => new MovieId(entry.Id),
            entry => ImmutableArray.CreateRange(entry.Vector));

    private static Movie BuildMovie(
        int id,
        string title,
        ImmutableArray<string> genres = default,
        ImmutableArray<string> directors = default,
        ImmutableArray<string> cast = default,
        ImmutableArray<(int Id, string Name)> keywords = default,
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
                signals: keywords.IsDefault
                    ? null
                    : MovieSignalSnapshot.FromPersisted(
                        keywords.Select(keyword => new MovieKeyword(keyword.Id, keyword.Name)),
                        [],
                        collection is not null)),
            status: status ?? EnrichmentStatus.Enriched,
            enrichedAt: null,
            enrichmentAttempts: 0,
            lastFailureCategory: null,
            lastAttemptAt: null);

    private static float[] Vector(double cosine)
    {
        var values = new float[EmbeddingDimensions.Value];
        values[0] = (float)cosine;
        values[1] = (float)Math.Sqrt(Math.Max(0.0, 1.0 - (cosine * cosine)));
        return values;
    }

    private static float[] WithNonfinite()
    {
        var values = new float[EmbeddingDimensions.Value];
        values[0] = float.NaN;
        values[1] = 1f;
        return values;
    }
}
