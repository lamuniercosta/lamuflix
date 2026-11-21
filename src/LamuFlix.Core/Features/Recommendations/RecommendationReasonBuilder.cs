using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace LamuFlix.Core.Features.Recommendations;

public static class RecommendationReasonBuilder
{
    private const string LikedReviewTemplate = "Because you liked {0}";
    private const string WatchedSeedTemplate = "Because you watched {0}";
    private const string WatchlistSeedTemplate = "Because {0} is on your watchlist";
    private const string NoSharedFeaturesReason = "No shared features identified";

    public static ImmutableList<Reason> BuildSeedReasons(
        SeedSignal signal,
        string? title,
        ImmutableList<Reason> evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        var builder = ImmutableList.CreateBuilder<Reason>();
        var display = Sanitize(title);

        if (display.Length > 0)
        {
            builder.Add(Reason.Personal(SeedTemplate(signal, display)));
        }

        builder.AddRange(evidence);
        return builder.ToImmutable();
    }

    private static string SeedTemplate(SeedSignal signal, string display) =>
        signal switch
        {
            SeedSignal.Liked => string.Format(CultureInfo.InvariantCulture, LikedReviewTemplate, display),
            SeedSignal.WatchedWithoutReview => string.Format(CultureInfo.InvariantCulture, WatchedSeedTemplate, display),
            _ => string.Format(CultureInfo.InvariantCulture, WatchlistSeedTemplate, display),
        };

    public static void EnsureFallbackReason(ImmutableList<Reason>.Builder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.Count == 0)
        {
            builder.Add(Reason.Structural(NoSharedFeaturesReason));
        }
    }

    private static string Sanitize(string? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            builder.Append(char.IsControl(character) ? ' ' : character);
        }

        return builder.ToString().Trim();
    }
}
