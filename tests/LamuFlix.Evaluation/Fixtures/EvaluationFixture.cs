using System.Collections.Immutable;

namespace LamuFlix.Evaluation.Fixtures;

public sealed record EvaluationFixture(
    string FixtureId,
    ImmutableArray<EvaluationCatalogMovie> Catalog,
    EvaluationRankingInputs RankingInputs,
    ImmutableArray<EvaluationEvent> Events);
