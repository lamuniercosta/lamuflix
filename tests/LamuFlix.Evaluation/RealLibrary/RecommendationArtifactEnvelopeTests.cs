using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Text;
using LamuFlix.Evaluation.TestSupport;

namespace LamuFlix.Evaluation.RealLibrary;

public sealed class RecommendationArtifactEnvelopeTests
{
    private const string GoldenFixtureRelative = "RealLibrary/Fixtures/capture-envelope.golden.json";

    private const string ExpectedGoldenText =
        "{\"mode\":\"capture\",\"targets\":[1,2],\"lists\":["
        + "{\"kind\":\"for-you\",\"targetId\":null,\"take\":30,\"status\":\"ok\",\"rows\":["
        + "{\"rank\":1,\"movieId\":7,\"title\":\"Alpha\",\"score\":1,\"reasons\":["
        + "{\"kind\":\"structural\",\"text\":\"Same director: Ada\"},"
        + "{\"kind\":\"personal\",\"text\":\"Matches your ratings\"}]},"
        + "{\"rank\":2,\"movieId\":8,\"title\":\"Beta\",\"score\":0.1,\"reasons\":[]}]},"
        + "{\"kind\":\"related\",\"targetId\":1,\"take\":10,\"status\":\"ok\",\"rows\":["
        + "{\"rank\":1,\"movieId\":8,\"title\":\"Beta\",\"score\":0.30000000000000004,\"reasons\":["
        + "{\"kind\":\"structural\",\"text\":\"Shares Drama\"}]}]},"
        + "{\"kind\":\"related\",\"targetId\":2,\"take\":10,\"status\":\"empty\",\"rows\":[]}]}";

    [Fact]
    public void SerializeToUtf8Bytes_WritesTheGoldenCaptureEnvelope()
    {
        // act
        var text = Encoding.UTF8.GetString(RecommendationArtifactSerializer.SerializeToUtf8Bytes(GoldenEnvelope()));

        // assert
        text.ShouldBe(ExpectedGoldenText);
    }

    [Fact]
    public void GoldenFixture_IsTheCanonicalSerialization()
    {
        // arrange
        var fixture = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, GoldenFixtureRelative));

        // act
        var canonical = Encoding.UTF8.GetString(
            RecommendationArtifactSerializer.SerializeToUtf8Bytes(GoldenEnvelope()));

        // assert
        fixture.ShouldBe(canonical);
    }

    [Fact]
    public void ReadFromUtf8Bytes_RoundTripsTheGoldenEnvelope()
    {
        // act
        var envelope = RecommendationArtifactSerializer.ReadFromUtf8Bytes(
            Encoding.UTF8.GetBytes(ExpectedGoldenText));
        var reserialized = Encoding.UTF8.GetString(
            RecommendationArtifactSerializer.SerializeToUtf8Bytes(envelope));

        // assert
        envelope.ShouldSatisfyAllConditions(
            value => value.Mode.ShouldBe(RecommendationArtifactContract.CaptureMode),
            value => value.Targets.ShouldBe([1, 2]),
            value => value.Lists.Length.ShouldBe(3),
            value => value.Lists[2].Status.ShouldBe(RecommendationArtifactContract.EmptyStatus));
        reserialized.ShouldBe(ExpectedGoldenText);
    }

    [Fact]
    public void SerializeToUtf8Bytes_WritesUtf8WithoutBomOrTrailingNewline()
    {
        // act
        var bytes = RecommendationArtifactSerializer.SerializeToUtf8Bytes(GoldenEnvelope());

        // assert
        bytes[0].ShouldBe((byte)'{');
        bytes[^1].ShouldBe((byte)'}');
        bytes.ShouldAllBe(value => value != (byte)'\n' && value != (byte)'\r');
    }

    [Fact]
    public void SerializeToUtf8Bytes_IsByteStableAndCultureInvariant()
    {
        // arrange
        var first = RecommendationArtifactSerializer.SerializeToUtf8Bytes(GoldenEnvelope());
        var original = CultureInfo.CurrentCulture;

        try
        {
            // act
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
            var second = RecommendationArtifactSerializer.SerializeToUtf8Bytes(GoldenEnvelope());

            // assert
            second.ShouldBe(first);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void SerializeToUtf8Bytes_KeepsRoundTripPrecisionForAdversarialFiniteValues()
    {
        // arrange
        var envelope = GoldenEnvelope() with
        {
            Lists =
            [
                GoldenEnvelope().Lists[0] with
                {
                    Rows =
                    [
                        Row(1, 7, "Alpha", 0.1 + 0.2),
                        Row(2, 8, "Beta", 1E-300),
                        Row(3, 9, "Gamma", 1.7976931348623157E+308),
                        Row(4, 10, "Delta", 0.0),
                    ],
                },
            ],
        };

        // act
        var text = Encoding.UTF8.GetString(RecommendationArtifactSerializer.SerializeToUtf8Bytes(envelope));

        // assert
        text.ShouldContain("\"score\":0." + (0.1 + 0.2).ToString("R", CultureInfo.InvariantCulture)[2..]);
        text.ShouldNotContain("\"score\":0,3");

        var parsed = RecommendationArtifactSerializer.ReadFromUtf8Bytes(Encoding.UTF8.GetBytes(text));
        parsed.Lists[0].Rows[0].Score.ShouldBe(0.1 + 0.2);
        parsed.Lists[0].Rows[1].Score.ShouldBe(1E-300);
        parsed.Lists[0].Rows[2].Score.ShouldBe(1.7976931348623157E+308);
        parsed.Lists[0].Rows[3].Score.ShouldBe(0.0);
    }

    [Fact]
    public void SerializeToUtf8Bytes_DeclaresExactlyTheEnvelopeFieldsWithoutSchemaVersion()
    {
        // act
        var text = Encoding.UTF8.GetString(RecommendationArtifactSerializer.SerializeToUtf8Bytes(GoldenEnvelope()));

        // assert
        text.ShouldStartWith("{\"mode\":");
        text.ShouldNotContain("schemaVersion");
        text.ShouldNotContain("generatedAt");
        text.ShouldNotContain("timestamp");
        text.ShouldNotContain("host");
    }

    [Fact]
    public void SerializeToUtf8Bytes_CompareModeEmitsBreakdownPlotAndPoolDiagnostics()
    {
        // arrange
        var envelope = new RecommendationArtifactEnvelope(
            RecommendationArtifactContract.CompareMode,
            [7],
            [
                new RecommendationArtifactList(
                    RecommendationArtifactContract.CompareListKind,
                    7,
                    null,
                    RecommendationArtifactContract.OkStatus,
                    [
                        new RecommendationArtifactRow(
                            1,
                            8,
                            "Beta",
                            0.5,
                            [],
                            new RecommendationPairDiagnostics(
                                [new RecommendationCriterionContribution("Director", 1.0, 3.0, 0.5)],
                                0.25,
                                4,
                                false)),
                        new RecommendationArtifactRow(2, 9, "Gamma", 0.4, [], null),
                    ]),
            ]);

        // act
        var text = Encoding.UTF8.GetString(RecommendationArtifactSerializer.SerializeToUtf8Bytes(envelope));

        // assert
        text.ShouldContain(
            "\"diagnostics\":{\"breakdown\":[{\"criterion\":\"Director\",\"value\":1,\"weight\":3,\"contribution\":0.5}],"
            + "\"rawPlotCosine\":0.25,\"plotRank\":4,\"inPool\":false}");
        text.ShouldContain("{\"rank\":2,\"movieId\":9,\"title\":\"Gamma\",\"score\":0.4,\"reasons\":[]}");
    }

    [Fact]
    public void SerializeToUtf8Bytes_UnusablePairWritesNullCosineAndNullOrPlotRank()
    {
        // arrange
        var envelope = new RecommendationArtifactEnvelope(
            RecommendationArtifactContract.CompareMode,
            [7],
            [
                new RecommendationArtifactList(
                    RecommendationArtifactContract.CompareListKind,
                    7,
                    null,
                    RecommendationArtifactContract.OkStatus,
                    [
                        new RecommendationArtifactRow(
                            1,
                            8,
                            "Beta",
                            0.3,
                            [],
                            new RecommendationPairDiagnostics([], null, null, false)),
                    ]),
            ]);

        // act
        var text = Encoding.UTF8.GetString(RecommendationArtifactSerializer.SerializeToUtf8Bytes(envelope));

        // assert
        text.ShouldContain("\"rawPlotCosine\":null,\"plotRank\":null,\"inPool\":false");
    }

    [Fact]
    public void SerializeToUtf8Bytes_RejectsNonFiniteNumbers()
    {
        // arrange
        var envelope = GoldenEnvelope() with
        {
            Lists = [GoldenEnvelope().Lists[0] with { Rows = [Row(1, 7, "Alpha", double.NaN)] }],
        };

        // act / assert
        Should.Throw<ArgumentException>(() => RecommendationArtifactSerializer.SerializeToUtf8Bytes(envelope));
    }

    [Fact]
    public void SerializeToUtf8Bytes_RejectsDuplicateTargets()
    {
        // arrange
        var envelope = GoldenEnvelope() with { Targets = [1, 1] };

        // act / assert
        Should.Throw<ArgumentException>(() => RecommendationArtifactSerializer.SerializeToUtf8Bytes(envelope));
    }

    [Fact]
    public void SerializeToUtf8Bytes_RejectsDuplicateMovieIdsInsideAList()
    {
        // arrange
        var envelope = GoldenEnvelope() with
        {
            Lists =
            [
                GoldenEnvelope().Lists[0] with
                {
                    Rows = [Row(1, 7, "Alpha", 1.0), Row(2, 7, "Alpha again", 0.5)],
                },
            ],
        };

        // act / assert
        Should.Throw<ArgumentException>(() => RecommendationArtifactSerializer.SerializeToUtf8Bytes(envelope));
    }

    [Fact]
    public void SerializeToUtf8Bytes_RejectsAnEmptyStatusListThatCarriesRows()
    {
        // arrange
        var envelope = GoldenEnvelope() with
        {
            Lists =
            [
                GoldenEnvelope().Lists[2] with { Rows = [Row(1, 7, "Alpha", 1.0)] },
            ],
        };

        // act / assert
        Should.Throw<ArgumentException>(() => RecommendationArtifactSerializer.SerializeToUtf8Bytes(envelope));
    }

    [Fact]
    public void SerializeToUtf8Bytes_RejectsPairDiagnosticsOutsideCompareMode()
    {
        // arrange
        var envelope = GoldenEnvelope() with
        {
            Lists =
            [
                GoldenEnvelope().Lists[0] with
                {
                    Rows = [Row(1, 7, "Alpha", 1.0) with { Diagnostics = new RecommendationPairDiagnostics([], 0.5, 1, true) }],
                },
            ],
        };

        // act / assert
        Should.Throw<ArgumentException>(() => RecommendationArtifactSerializer.SerializeToUtf8Bytes(envelope));
    }

    [Theory]
    [InlineData("{\"mode\":\"capture\",\"targets\":[1],\"lists\":[{\"kind\":\"for-you\",\"targetId\":null,\"take\":30,\"status\":\"ok\",\"rows\":[{\"rank\":1,\"movieId\":7,\"title\":\"A\",\"score\":1,\"reasons\":[]},{\"rank\":2,\"movieId\":7,\"title\":\"B\",\"score\":1,\"reasons\":[]}]}]}")]
    [InlineData("{\"mode\":\"bogus\",\"targets\":[1],\"lists\":[]}")]
    [InlineData("{\"mode\":\"capture\",\"targets\":[1]}")]
    [InlineData("{\"mode\":\"capture\",\"targets\":[1],\"lists\":[{\"kind\":\"for-you\",\"targetId\":null,\"take\":30,\"status\":\"ok\",\"rows\":[{\"rank\":2,\"movieId\":7,\"title\":\"A\",\"score\":1,\"reasons\":[]}]}]}")]
    [InlineData("{\"mode\":\"capture\",\"targets\":[1],\"lists\":[{\"kind\":\"for-you\",\"targetId\":null,\"take\":30,\"status\":\"ok\",\"rows\":[{\"rank\":1,\"movieId\":7,\"title\":\"A\",\"score\":\"1\",\"reasons\":[]}]}]}")]
    [InlineData("not json")]
    public void ReadFromUtf8Bytes_RejectsMalformedArtifacts(string json)
    {
        // act / assert
        Should.Throw<Exception>(() => RecommendationArtifactSerializer.ReadFromUtf8Bytes(Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void WriteToFile_PublishesAtomicallyAndLeavesNoPartialFile()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("nested", "capture.json");
        var envelope = GoldenEnvelope();

        // act
        RecommendationArtifactSerializer.WriteToFile(envelope, path);

        // assert
        File.ReadAllBytes(path).ShouldBe(RecommendationArtifactSerializer.SerializeToUtf8Bytes(envelope));
        var directoryPath = Path.GetDirectoryName(path);
        directoryPath.ShouldNotBeNull();
        Directory.GetFiles(directoryPath).ShouldBe([path]);
    }

    [Fact]
    public void WriteToFile_WhenSerializationFails_PublishesNothing()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("capture.json");
        var envelope = GoldenEnvelope() with
        {
            Lists = [GoldenEnvelope().Lists[0] with { Rows = [Row(1, 7, "Alpha", double.PositiveInfinity)] }],
        };

        // act / assert
        Should.Throw<ArgumentException>(() => RecommendationArtifactSerializer.WriteToFile(envelope, path));
        Directory.GetFiles(directory.FullPath).ShouldBeEmpty();
    }

    private static RecommendationArtifactRow Row(int rank, int movieId, string title, double score) =>
        new(rank, movieId, title, score, ImmutableArray<RecommendationArtifactReason>.Empty, null);

    private static RecommendationArtifactEnvelope GoldenEnvelope() =>
        new(
            RecommendationArtifactContract.CaptureMode,
            [1, 2],
            [
                new RecommendationArtifactList(
                    RecommendationArtifactContract.ForYouListKind,
                    null,
                    30,
                    RecommendationArtifactContract.OkStatus,
                    [
                        new RecommendationArtifactRow(
                            1,
                            7,
                            "Alpha",
                            1.0,
                            [
                                new RecommendationArtifactReason(RecommendationArtifactContract.StructuralReasonKind, "Same director: Ada"),
                                new RecommendationArtifactReason(RecommendationArtifactContract.PersonalReasonKind, "Matches your ratings"),
                            ],
                            null),
                        Row(2, 8, "Beta", 0.1),
                    ]),
                new RecommendationArtifactList(
                    RecommendationArtifactContract.RelatedListKind,
                    1,
                    10,
                    RecommendationArtifactContract.OkStatus,
                    [
                        new RecommendationArtifactRow(
                            1,
                            8,
                            "Beta",
                            0.1 + 0.2,
                            [new RecommendationArtifactReason(RecommendationArtifactContract.StructuralReasonKind, "Shares Drama")],
                            null),
                    ]),
                new RecommendationArtifactList(
                    RecommendationArtifactContract.RelatedListKind,
                    2,
                    10,
                    RecommendationArtifactContract.EmptyStatus,
                    []),
            ]);
}
