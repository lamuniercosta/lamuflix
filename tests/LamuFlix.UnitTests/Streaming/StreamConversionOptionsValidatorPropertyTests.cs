using System;
using FsCheck.Xunit;
using LamuFlix.Core.Options;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.Streaming;

public sealed class StreamConversionOptionsValidatorPropertyTests
{
    private readonly StreamConversionOptionsValidator validator = new();

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Validate_AllFieldsValid_Succeeds(int maxConcurrent, long idleTicks, long sizeLimit)
    {
        // arrange
        var options = new StreamConversionOptions
        {
            MaxConcurrentConversions = Positive(maxConcurrent),
            IdleExpiry = PositiveTimeSpan(idleTicks),
            ScratchSizeLimitBytes = PositiveLong(sizeLimit),
            ScratchDirectory = "cache/stream-conversion",
        };

        // act
        var result = validator.Validate(null, options);

        // assert
        return result == ValidateOptionsResult.Success;
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Validate_NonPositiveMaxConcurrentConversions_FailsOnlyThatField(int maxConcurrent)
    {
        // arrange
        var options = new StreamConversionOptions
        {
            MaxConcurrentConversions = NonPositive(maxConcurrent),
        };

        // act
        var result = validator.Validate(null, options);

        // assert
        return FailureMentions(
            result,
            maxConcurrent: true,
            idleExpiry: false,
            sizeLimit: false,
            scratchDirectory: false);
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Validate_NonPositiveIdleExpiry_FailsOnlyThatField(long idleTicks)
    {
        // arrange
        var options = new StreamConversionOptions
        {
            IdleExpiry = NonPositiveTimeSpan(idleTicks),
        };

        // act
        var result = validator.Validate(null, options);

        // assert
        return FailureMentions(
            result,
            maxConcurrent: false,
            idleExpiry: true,
            sizeLimit: false,
            scratchDirectory: false);
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Validate_NonPositiveSizeLimit_FailsOnlyThatField(long sizeLimit)
    {
        // arrange
        var options = new StreamConversionOptions
        {
            ScratchSizeLimitBytes = NonPositiveLong(sizeLimit),
        };

        // act
        var result = validator.Validate(null, options);

        // assert
        return FailureMentions(
            result,
            maxConcurrent: false,
            idleExpiry: false,
            sizeLimit: true,
            scratchDirectory: false);
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Validate_BlankScratchDirectory_FailsOnlyThatField(string? directory)
    {
        // arrange
        var options = new StreamConversionOptions
        {
            ScratchDirectory = string.IsNullOrWhiteSpace(directory) ? directory ?? string.Empty : " ",
        };

        // act
        var result = validator.Validate(null, options);

        // assert
        return FailureMentions(
            result,
            maxConcurrent: false,
            idleExpiry: false,
            sizeLimit: false,
            scratchDirectory: true);
    }

    private static bool FailureMentions(
        ValidateOptionsResult result,
        bool maxConcurrent,
        bool idleExpiry,
        bool sizeLimit,
        bool scratchDirectory)
    {
        if (!result.Failed)
        {
            return false;
        }

        var text = string.Join("; ", result.Failures);
        return text.Contains(nameof(StreamConversionOptions.MaxConcurrentConversions), StringComparison.Ordinal) == maxConcurrent
            && text.Contains(nameof(StreamConversionOptions.IdleExpiry), StringComparison.Ordinal) == idleExpiry
            && text.Contains(nameof(StreamConversionOptions.ScratchSizeLimitBytes), StringComparison.Ordinal) == sizeLimit
            && text.Contains(nameof(StreamConversionOptions.ScratchDirectory), StringComparison.Ordinal) == scratchDirectory;
    }

    private static int Positive(int value)
    {
        var magnitude = value == int.MinValue ? (long)int.MaxValue + 1 : Math.Abs((long)value);
        return (int)(magnitude % (int.MaxValue - 1)) + 1;
    }

    private static int NonPositive(int value)
    {
        if (value == 0)
        {
            return 0;
        }

        return value == int.MinValue ? int.MinValue : -Math.Abs(value);
    }

    private static long PositiveLong(long value)
    {
        var magnitude = value == long.MinValue ? (ulong)long.MaxValue + 1 : (ulong)Math.Abs(value);
        return (long)(magnitude % ((ulong)long.MaxValue - 1)) + 1;
    }

    private static long NonPositiveLong(long value)
    {
        if (value == 0)
        {
            return 0;
        }

        return value == long.MinValue ? long.MinValue : -Math.Abs(value);
    }

    private static TimeSpan PositiveTimeSpan(long ticks) => TimeSpan.FromTicks(PositiveLong(ticks));

    private static TimeSpan NonPositiveTimeSpan(long ticks) => TimeSpan.FromTicks(NonPositiveLong(ticks));
}
