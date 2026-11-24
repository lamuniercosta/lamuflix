using Microsoft.Extensions.Logging;

namespace LamuFlix.Infrastructure.Adapters;

public sealed partial class OpenSubtitlesSubtitleProvider
{
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Warning,
            Message = "Subtitle provider returned a download link on refused host {RefusedLinkHost}. Subtitle search and download stay disabled until Subtitles:OpenSubtitles:DownloadAllowedHosts and DownloadLinkHost are corrected and the API restarts.")]
        public static partial void RefusedLinkHost(ILogger logger, string refusedLinkHost);
    }
}
