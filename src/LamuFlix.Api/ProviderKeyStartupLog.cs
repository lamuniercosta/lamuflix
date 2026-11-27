using System;
using LamuFlix.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LamuFlix.Api;

public static class ProviderKeyStartupLog
{
    private const string OmDbServiceName = "OMDb";

    private const string TmDbServiceName = "TMDb";

    private const string OpenSubtitlesServiceName = "OpenSubtitles";

    public static void LogProviderKeySets(
        ILogger logger,
        IConfiguration configuration,
        string keySet,
        bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(configuration);
        LogProviderKeySets(
            logger,
            keySet,
            !string.IsNullOrWhiteSpace(configuration["Omdb:ApiKey"]),
            !string.IsNullOrWhiteSpace(configuration["Tmdb:ApiKey"]),
            !string.IsNullOrWhiteSpace(configuration["Subtitles:OpenSubtitles:ApiKey"]),
            isDevelopment);
    }

    public static void LogProviderKeySets(
        ILogger logger,
        string keySet,
        bool omdbKeyPresent,
        bool tmdbKeyPresent,
        bool openSubtitlesKeyPresent,
        bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(keySet);
        LogService(logger, OmDbServiceName, keySet, omdbKeyPresent);
        LogService(logger, TmDbServiceName, keySet, tmdbKeyPresent);
        LogService(logger, OpenSubtitlesServiceName, keySet, openSubtitlesKeyPresent);
        if (isDevelopment && keySet.Equals(ProviderKeyOptions.ProdKeySet, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Development environment is using the {KeySet} provider key set; switch to the test key set for local runs.",
                keySet);
        }
    }

    private static void LogService(ILogger logger, string service, string keySet, bool keyPresent) =>
        logger.LogInformation(
            "Provider key set {KeySet} selected for {Service}; API key {KeyPresence}.",
            keySet,
            service,
            keyPresent ? "present" : "missing");
}
