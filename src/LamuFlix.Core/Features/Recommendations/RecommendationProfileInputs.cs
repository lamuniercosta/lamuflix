using System.Collections.Immutable;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Recommendations;

public sealed record RecommendationProfileInputs
{
    public RecommendationProfileInputs(
        ImmutableArray<TasteProfileMovie> movies,
        ImmutableDictionary<MovieId, ImmutableArray<float>> candidateEmbeddings)
        : this(movies, candidateEmbeddings, ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty)
    {
    }

    public RecommendationProfileInputs(
        ImmutableArray<TasteProfileMovie> movies,
        ImmutableDictionary<MovieId, ImmutableArray<float>> candidateEmbeddings,
        ImmutableDictionary<MovieId, ImmutableArray<float>> likedSourceEmbeddings)
    {
        Movies = movies;
        CandidateEmbeddings = candidateEmbeddings;
        LikedSourceEmbeddings = likedSourceEmbeddings;
    }

    public ImmutableArray<TasteProfileMovie> Movies { get; }

    public ImmutableDictionary<MovieId, ImmutableArray<float>> CandidateEmbeddings { get; }

    public ImmutableDictionary<MovieId, ImmutableArray<float>> LikedSourceEmbeddings { get; }
}
