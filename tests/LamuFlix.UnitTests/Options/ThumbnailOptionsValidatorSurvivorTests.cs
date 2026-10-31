using System;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class ThumbnailOptionsValidatorSurvivorTests
{
    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullExceptionNamingOptions()
    {
        // arrange
        var validator = new ThumbnailOptionsValidator();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var thrown = Should.Throw<ArgumentNullException>(() => validator.Validate(null, null!));

        // assert
        thrown.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Validate_EnabledWithSmallestPositiveBounds_Succeeds()
    {
        // arrange
        var options = new ThumbnailOptions
        {
            Enabled = true,
            MaxWidth = 1,
            ProcessTimeoutSeconds = 1,
        };

        // act
        var result = new ThumbnailOptionsValidator().Validate(null, options);

        // assert
        result.ShouldBe(Microsoft.Extensions.Options.ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_EnabledFailureMessages_NameTheirSectionAndField()
    {
        // arrange
        var options = new ThumbnailOptions
        {
            Enabled = true,
            FfmpegPath = string.Empty,
        };

        // act
        var result = new ThumbnailOptionsValidator().Validate(null, options);

        // assert
        result.Failed.ShouldBeTrue();
        result.Failures.ShouldBe([$"{ThumbnailOptions.SectionName}:{nameof(ThumbnailOptions.FfmpegPath)} must not be blank."]);
    }
}
