using System;
using FsCheck.Xunit;
using LamuFlix.Core.Options;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class EnrichmentOptionsValidatorPropertyTests
{
    private readonly EnrichmentOptionsValidator validator = new();

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Validate_PositiveDurations_Succeeds(long claimTicks, long sweepTicks)
    {
        var options = Options(PositiveDuration(claimTicks), PositiveDuration(sweepTicks));

        var result = validator.Validate(null, options);

        return result == ValidateOptionsResult.Success;
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Validate_NonPositiveClaimLease_FailsOnlyThatField(long claimTicks, long sweepTicks)
    {
        var options = Options(NonPositiveDuration(claimTicks), PositiveDuration(sweepTicks));

        return FailureMentions(validator.Validate(null, options), claimLease: true, sweepInterval: false);
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Validate_NonPositiveSweepInterval_FailsOnlyThatField(long claimTicks, long sweepTicks)
    {
        var options = Options(PositiveDuration(claimTicks), NonPositiveDuration(sweepTicks));

        return FailureMentions(validator.Validate(null, options), claimLease: false, sweepInterval: true);
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Validate_BothNonPositive_FailsBothFields(long claimTicks, long sweepTicks)
    {
        var options = Options(NonPositiveDuration(claimTicks), NonPositiveDuration(sweepTicks));

        return FailureMentions(validator.Validate(null, options), claimLease: true, sweepInterval: true);
    }

    private static EnrichmentOptions Options(TimeSpan claimLease, TimeSpan sweepInterval) =>
        new() { ClaimLease = claimLease, SweepInterval = sweepInterval };

    private static bool FailureMentions(ValidateOptionsResult result, bool claimLease, bool sweepInterval)
    {
        if (!result.Failed)
        {
            return false;
        }

        var text = string.Join("; ", result.Failures);
        return text.Contains(nameof(EnrichmentOptions.ClaimLease), StringComparison.Ordinal) == claimLease
            && text.Contains(nameof(EnrichmentOptions.SweepInterval), StringComparison.Ordinal) == sweepInterval;
    }

    private static TimeSpan PositiveDuration(long ticks)
    {
        var magnitude = ticks == long.MinValue ? long.MaxValue : Math.Abs(ticks);
        return TimeSpan.FromTicks((magnitude % (TimeSpan.MaxValue.Ticks - 1)) + 1);
    }

    private static TimeSpan NonPositiveDuration(long ticks)
    {
        if (ticks == 0)
        {
            return TimeSpan.Zero;
        }

        if (ticks == long.MinValue)
        {
            return TimeSpan.MinValue;
        }

        return TimeSpan.FromTicks(ticks > 0 ? -ticks : ticks);
    }
}
