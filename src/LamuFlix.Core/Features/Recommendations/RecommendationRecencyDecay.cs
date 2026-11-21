using System;
using LamuFlix.Core.Options;

namespace LamuFlix.Core.Features.Recommendations;

public sealed class RecommendationRecencyDecay
{
    public const double UnscaledWeight = 1.0;

    private readonly TimeProvider timeProvider;
    private readonly double lambda;
    private readonly double floor;

    public RecommendationRecencyDecay(TimeProvider timeProvider, RecommendationOptions options)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(options);

        if (!double.IsFinite(options.RecencyDecayLambda) || options.RecencyDecayLambda <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.RecencyDecayLambda,
                "RecencyDecayLambda must be finite and greater than 0.0.");
        }

        if (!double.IsFinite(options.RecencyFloor) || options.RecencyFloor <= 0.0 || options.RecencyFloor > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.RecencyFloor,
                "RecencyFloor must be finite and greater than 0.0 and at most 1.0.");
        }

        this.timeProvider = timeProvider;
        lambda = options.RecencyDecayLambda;
        floor = options.RecencyFloor;
    }

    public double Floor => floor;

    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();

    public static double AgeInDays(DateTimeOffset now, DateTimeOffset playedAt) =>
        Math.Max(0.0, (now - playedAt).TotalDays);

    public double AgeInDays(DateTimeOffset playedAt) => AgeInDays(UtcNow, playedAt);

    public double RelativeWeight(double age, double referenceAge)
    {
        RequireFiniteNonNegative(age, nameof(age));
        RequireFiniteNonNegative(referenceAge, nameof(referenceAge));

        return Math.Exp(-lambda * (age - referenceAge));
    }

    public double Weight(DateTimeOffset playedAt, DateTimeOffset referencePlayedAt) =>
        Weight(UtcNow, playedAt, referencePlayedAt);

    public double Weight(DateTimeOffset now, DateTimeOffset playedAt, DateTimeOffset referencePlayedAt) =>
        Math.Max(floor, RelativeWeight(AgeInDays(now, playedAt), AgeInDays(now, referencePlayedAt)));

    private static void RequireFiniteNonNegative(double value, string paramName)
    {
        if (!double.IsFinite(value) || value < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                "Ages must be finite and greater than or equal to 0.0.");
        }
    }
}
