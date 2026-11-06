using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using LamuFlix.Core.Options;

namespace LamuFlix.Core.Features.Recommendations;

public static class RecommendationReasonBuilder
{
    private const string LikedReviewTemplate = "Because you liked {0}";
    private const string NoSharedFeaturesReason = "No shared features identified";
    private const string ViewingHistoryReason = "Matches your viewing history";
    private const string GenreInterestTemplate = "Matches your {0} interest";

    public static void AddProfileReasons(
        ImmutableList<Reason>.Builder builder,
        ProfileSimilarityEvidence profileSimilarity)
    {
        ArgumentNullException.ThrowIfNull(builder);

        foreach (var genre in profileSimilarity.OverlappingGenres)
        {
            var display = Sanitize(genre);
            if (display.Length > 0)
            {
                builder.Add(Reason.Personal(
                    string.Format(CultureInfo.InvariantCulture, GenreInterestTemplate, display)));
            }
        }

        if (profileSimilarity is { HasPlotChannel: true, PlotSimilarity: > 0.0 })
        {
            builder.Add(Reason.Personal(ViewingHistoryReason));
        }
    }

    public static void EnsureFallbackReason(ImmutableList<Reason>.Builder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.Count == 0)
        {
            builder.Add(Reason.Structural(NoSharedFeaturesReason));
        }
    }

    public static void AddLikedReviewReason(
        ImmutableList<Reason>.Builder builder,
        ReviewAffinityEvidence reviewAffinity,
        double profileContribution,
        RecommendationOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        if (!IsEligible(reviewAffinity, profileContribution, options, out var source))
        {
            return;
        }

        var display = Sanitize(source.Title);
        if (display.Length == 0)
        {
            return;
        }

        builder.Add(Reason.Personal(
            string.Format(CultureInfo.InvariantCulture, LikedReviewTemplate, display)));
    }

    private static bool IsEligible(
        ReviewAffinityEvidence reviewAffinity,
        double profileContribution,
        RecommendationOptions options,
        out LikedSourceEvidence source)
    {
        if (WeightsEnabled(options)
            && ChannelReady(reviewAffinity, profileContribution)
            && reviewAffinity.LikedSource is { } liked)
        {
            source = liked;
            return true;
        }

        source = default;
        return false;
    }

    private static bool WeightsEnabled(RecommendationOptions options) =>
        options.LikedReviewWeight > 0.0 && options.BlendProfileWeight > 0.0;

    private static bool ChannelReady(ReviewAffinityEvidence reviewAffinity, double profileContribution) =>
        reviewAffinity.HasReviewChannel && reviewAffinity.LikedAffinity > 0.0 && profileContribution > 0.0;

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
