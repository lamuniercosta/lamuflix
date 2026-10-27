using System;
using System.Globalization;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Library;

namespace LamuFlix.UnitTests.Library;

public sealed class MovieQueryRatingPropertyTests
{
    [Property(MaxTest = 200)]
    [Trait("Category", "Property")]
    public bool CanonicalTenthStep_ParsesToExpectedBound(int tenth)
    {
        var normalized = Math.Abs(tenth) % 101;
        var expected = normalized / 10m;
        var raw = expected.ToString("0.0", CultureInfo.InvariantCulture);

        return MovieQueryString.TryParseRatingBound(raw, out var bound)
            && bound == expected;
    }

    [Property(MaxTest = 200)]
    [Trait("Category", "Property")]
    public bool AcceptedBound_AgreesWithImdbRating(decimal candidate)
    {
        var raw = candidate.ToString("0.0####", CultureInfo.InvariantCulture);

        if (!MovieQueryString.TryParseRatingBound(raw, out var bound))
        {
            return true;
        }

        return bound is not null
            && ImdbRating.TryCreate(bound.Value, out _)
            && bound.Value.ToString("0.0", CultureInfo.InvariantCulture) == raw;
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool NullOrEmpty_RatingBound_ParsesToNull(string? raw)
    {
        if (!string.IsNullOrEmpty(raw))
        {
            return true;
        }

        return MovieQueryString.TryParseRatingBound(raw, out var bound) && bound is null;
    }
}
