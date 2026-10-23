using System.Collections.Immutable;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Recommendations;

public sealed class RecommendationProfileInputs
{
    public RecommendationProfileInputs(
        ImmutableArray<TasteProfileMovie> movies,
        ImmutableDictionary<MovieId, ImmutableArray<float>> candidateEmbeddings)
    {
        Movies = movies;
        CandidateEmbeddings = candidateEmbeddings;
    }

    public ImmutableArray<TasteProfileMovie> Movies { get; }

    public ImmutableDictionary<MovieId, ImmutableArray<float>> CandidateEmbeddings { get; }
}
