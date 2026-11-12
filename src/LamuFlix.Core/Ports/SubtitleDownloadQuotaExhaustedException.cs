using System;

namespace LamuFlix.Core.Ports;

public sealed class SubtitleDownloadQuotaExhaustedException : Exception
{
    public SubtitleDownloadQuotaExhaustedException(SubtitleQuota quota)
        : base("The subtitle provider download quota is exhausted.")
    {
        Quota = quota ?? throw new ArgumentNullException(nameof(quota));
    }

    public SubtitleDownloadQuotaExhaustedException()
        : this(SubtitleQuota.Unknown)
    {
    }

    public SubtitleDownloadQuotaExhaustedException(string message, SubtitleQuota quota, Exception innerException)
        : base(message, innerException)
    {
        Quota = quota ?? throw new ArgumentNullException(nameof(quota));
    }

    public SubtitleQuota Quota { get; }
}
