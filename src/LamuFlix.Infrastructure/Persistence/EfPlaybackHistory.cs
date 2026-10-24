using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence.Records;

namespace LamuFlix.Infrastructure.Persistence;

public sealed class EfPlaybackHistory(
    LamuFlixDbContext dbContext,
    HybridCachedRecommendationProfileSource? tasteProfileCache = null) : IPlaybackHistory
{
    public async Task RecordAsync(PlaybackEvent playbackEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(playbackEvent);

        dbContext.PlaybackEvents.Add(new PlaybackEventRecord
        {
            MovieId = playbackEvent.MovieId,
            PlayedAt = playbackEvent.PlayedAt,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        if (tasteProfileCache is not null)
        {
            await tasteProfileCache.InvalidateAsync(cancellationToken);
        }
    }
}
