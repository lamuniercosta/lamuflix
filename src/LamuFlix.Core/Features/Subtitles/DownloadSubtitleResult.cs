using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Subtitles;

public sealed record DownloadSubtitleResult(SubtitleTrack Track, SubtitleQuota Quota);
