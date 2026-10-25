using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;

namespace LamuFlix.Evaluation.Fixtures;

public sealed record EvaluationCatalogMovie(MovieId MovieId, MovieFeatures Features);
