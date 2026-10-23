using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Recommendations;

public sealed record RecommendationCandidate(Movie Movie, double CosineSimilarity);
