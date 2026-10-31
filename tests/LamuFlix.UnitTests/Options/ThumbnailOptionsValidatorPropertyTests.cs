using System;
using FsCheck.Xunit;
using LamuFlix.Core.Options;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class ThumbnailOptionsValidatorPropertyTests
{
    private readonly ThumbnailOptionsValidator validator = new();

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Validate_EnabledWithPositiveBoundsAndPaths_Succeeds(int maxWidth, int processTimeoutSeconds)
    {
        // arrange
        var options = new ThumbnailOptions
        {
            Enabled = true,
            MaxWidth = Positive(maxWidth),
            ProcessTimeoutSeconds = Positive(processTimeoutSeconds),
        };

        // act
        var result = validator.Validate(null, options);

        // assert
        return result == ValidateOptionsResult.Success;
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Validate_Disabled_AlwaysSucceeds(int maxWidth, int processTimeoutSeconds, bool blankPaths)
    {
        // arrange
        var options = new ThumbnailOptions
        {
            Enabled = false,
            FfmpegPath = blankPaths ? " " : "ffmpeg",
            CacheDirectory = blankPaths ? string.Empty : "cache/thumbnails",
            MaxWidth = maxWidth,
            ProcessTimeoutSeconds = processTimeoutSeconds,
        };

        // act
        var result = validator.Validate(null, options);

        // assert
        return result == ValidateOptionsResult.Success;
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Validate_EnabledWithNonPositiveMaxWidth_FailsOnlyThatField(int maxWidth)
    {
        // arrange
        var options = new ThumbnailOptions
        {
            Enabled = true,
            MaxWidth = NonPositive(maxWidth),
        };

        // act
        var result = validator.Validate(null, options);

        // assert
        return FailureMentions(result, maxWidth: true, processTimeoutSeconds: false);
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Validate_EnabledWithNonPositiveProcessTimeout_FailsOnlyThatField(int processTimeoutSeconds)
    {
        // arrange
        var options = new ThumbnailOptions
        {
            Enabled = true,
            ProcessTimeoutSeconds = NonPositive(processTimeoutSeconds),
        };

        // act
        var result = validator.Validate(null, options);

        // assert
        return FailureMentions(result, maxWidth: false, processTimeoutSeconds: true);
    }

    private static bool FailureMentions(
        ValidateOptionsResult result,
        bool maxWidth,
        bool processTimeoutSeconds)
    {
        if (!result.Failed)
        {
            return false;
        }

        var text = string.Join("; ", result.Failures);
        return text.Contains(nameof(ThumbnailOptions.MaxWidth), StringComparison.Ordinal) == maxWidth
            && text.Contains(nameof(ThumbnailOptions.ProcessTimeoutSeconds), StringComparison.Ordinal) == processTimeoutSeconds;
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
}
