using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using LamuFlix.Core.Options;

namespace LamuFlix.Core.Features.Recommendations;

public static class RecommendationReasonBuilder
{
    private const string LikedReviewTemplate = "Because you liked {0}";

    public static void AddLikedReviewReason(
        ImmutableList<Reason>.Builder builder,
        ReviewAffinityEvidence reviewAffinity,
        double profileContribution,
        RecommendationOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        if (options.LikedReviewWeight <= 0.0
            || options.BlendProfileWeight <= 0.0
            || !reviewAffinity.HasReviewChannel
            || reviewAffinity.LikedAffinity <= 0.0
            || profileContribution <= 0.0
            || reviewAffinity.LikedSource is not { } source)
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

    public static string Sanitize(string? value)
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
