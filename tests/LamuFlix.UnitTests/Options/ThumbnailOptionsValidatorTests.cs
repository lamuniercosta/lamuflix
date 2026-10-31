using System;
using LamuFlix.Core.Options;
using LamuFlix.ServiceDefaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class ThumbnailOptionsValidatorTests
{
    [Fact]
    public void Validate_DisabledWithDefaults_Succeeds()
    {
        // act
        var result = Validate(new ThumbnailOptions());

        // assert
        result.ShouldBe(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_EnabledWithDefaults_Succeeds()
    {
        // act
        var result = Validate(new ThumbnailOptions { Enabled = true });

        // assert
        result.ShouldBe(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_DisabledWithBlankPathsAndNonPositiveBounds_Succeeds()
    {
        // arrange
        var options = new ThumbnailOptions
        {
            Enabled = false,
            FfmpegPath = " ",
            CacheDirectory = string.Empty,
            MaxWidth = 0,
            ProcessTimeoutSeconds = -1,
        };

        // act
        var result = Validate(options);

        // assert
        result.ShouldBe(ValidateOptionsResult.Success);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Validate_EnabledWithBlankFfmpegPath_FailsNamingFfmpegPath(string ffmpegPath)
    {
        // act
        var result = Validate(new ThumbnailOptions { Enabled = true, FfmpegPath = ffmpegPath });

        // assert
        ShouldFailNaming(result, nameof(ThumbnailOptions.FfmpegPath));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Validate_EnabledWithBlankCacheDirectory_FailsNamingCacheDirectory(string cacheDirectory)
    {
        // act
        var result = Validate(new ThumbnailOptions { Enabled = true, CacheDirectory = cacheDirectory });

        // assert
        ShouldFailNaming(result, nameof(ThumbnailOptions.CacheDirectory));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_EnabledWithNonPositiveMaxWidth_FailsNamingMaxWidth(int maxWidth)
    {
        // act
        var result = Validate(new ThumbnailOptions { Enabled = true, MaxWidth = maxWidth });

        // assert
        ShouldFailNaming(result, nameof(ThumbnailOptions.MaxWidth));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_EnabledWithNonPositiveProcessTimeout_FailsNamingProcessTimeout(int processTimeoutSeconds)
    {
        // act
        var result = Validate(new ThumbnailOptions { Enabled = true, ProcessTimeoutSeconds = processTimeoutSeconds });

        // assert
        ShouldFailNaming(result, nameof(ThumbnailOptions.ProcessTimeoutSeconds));
    }

    [Fact]
    public void Validate_EnabledWithEveryInvalidValue_FailsNamingEachField()
    {
        // arrange
        var options = new ThumbnailOptions
        {
            Enabled = true,
            FfmpegPath = string.Empty,
            CacheDirectory = string.Empty,
            MaxWidth = 0,
            ProcessTimeoutSeconds = 0,
        };

        // act
        var result = Validate(options);

        // assert
        result.Failed.ShouldBeTrue();
        var failures = string.Join("; ", result.Failures);
        failures.ShouldContain(nameof(ThumbnailOptions.FfmpegPath));
        failures.ShouldContain(nameof(ThumbnailOptions.CacheDirectory));
        failures.ShouldContain(nameof(ThumbnailOptions.MaxWidth));
        failures.ShouldContain(nameof(ThumbnailOptions.ProcessTimeoutSeconds));
    }

    [Fact]
    public void AddLamuFlixOptions_RegistersValidatorAndStartupValidation()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddLamuFlixOptions();

        // assert
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IValidateOptions<ThumbnailOptions>)
            && descriptor.ImplementationType == typeof(ThumbnailOptionsValidator));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IStartupValidator));
    }

    private static ValidateOptionsResult Validate(ThumbnailOptions options) =>
        new ThumbnailOptionsValidator().Validate(global::Microsoft.Extensions.Options.Options.DefaultName, options);

    private static void ShouldFailNaming(ValidateOptionsResult result, string memberName)
    {
        result.Failed.ShouldBeTrue();
        string.Join("; ", result.Failures).ShouldContain(memberName);
    }
}
