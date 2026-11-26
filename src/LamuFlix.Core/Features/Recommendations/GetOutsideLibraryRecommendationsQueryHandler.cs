using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Features.Recommendations;

public sealed class GetOutsideLibraryRecommendationsQueryHandler(
    IRecommendationCandidateSource candidates,
    IOptions<RecommendationOptions> options)
    : IQueryHandler<GetOutsideLibraryRecommendationsQuery, IReadOnlyList<OutsideLibraryRecommendation>>
{
    public async Task<IReadOnlyList<OutsideLibraryRecommendation>> HandleAsync(
        GetOutsideLibraryRecommendationsQuery query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var snapshot = await candidates.GetOutsideLibraryRecommendationsAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        return snapshot.AdmittedCards ?? OutsideLibraryRanking.Rank(snapshot, options.Value);
    }
}
