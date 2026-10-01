using System;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.RabbitMq;

namespace LamuFlix.UnitTests.RabbitMq;

public sealed class RabbitMqConsumerOptionsValidatorTests
{
    [Fact]
    public void Validate_AnUnsetClaimLease_FailsAndNamesTheOption()
    {
        // arrange
        var validator = Validating(TimeSpan.Zero);

        // act
        var result = validator.Validate(null, Rabbit(TimeSpan.FromSeconds(30)));

        // assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(nameof(EnrichmentOptions.ClaimLease));
    }

    [Fact]
    public void Validate_ANegativeClaimLease_FailsAndNamesTheOption()
    {
        // arrange
        var validator = Validating(TimeSpan.FromSeconds(-1));

        // act
        var result = validator.Validate(null, Rabbit(TimeSpan.FromSeconds(30)));

        // assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(nameof(EnrichmentOptions.ClaimLease));
    }

    [Fact]
    public void Validate_RetryDelayEqualToTheClaimLease_Fails()
    {
        // arrange
        var validator = Validating(TimeSpan.FromSeconds(5));

        // act
        var result = validator.Validate(null, Rabbit(TimeSpan.FromSeconds(5)));

        // assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(nameof(RabbitMqOptions.RetryDelay));
    }

    [Fact]
    public void Validate_RetryDelayShorterThanTheClaimLease_Fails()
    {
        // arrange
        var validator = Validating(TimeSpan.FromSeconds(5));

        // act
        var result = validator.Validate(null, Rabbit(TimeSpan.FromSeconds(4)));

        // assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(nameof(RabbitMqOptions.RetryDelay));
    }

    [Fact]
    public void Validate_AClaimLeaseJustUnderTheRoundedDelay_Passes()
    {
        // arrange
        var validator = Validating(TimeSpan.FromMilliseconds(999.6));

        // act
        var result = validator.Validate(null, Rabbit(TimeSpan.FromSeconds(1)));

        // assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ARoundingStepThatWouldEraseTheMargin_Fails()
    {
        // arrange
        var validator = Validating(TimeSpan.FromMilliseconds(1000.4));

        // act
        var result = validator.Validate(null, Rabbit(TimeSpan.FromMilliseconds(1000)));

        // assert
        result.Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ASubMillisecondRetryDelay_Fails()
    {
        // arrange
        var validator = Validating(TimeSpan.FromMilliseconds(1));

        // act
        var result = validator.Validate(null, Rabbit(TimeSpan.FromTicks(TimeSpan.TicksPerMillisecond / 10)));

        // assert
        result.Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_AnUnrepresentableRetryDelay_Fails()
    {
        // arrange
        var validator = Validating(TimeSpan.FromMilliseconds(1));

        // act
        var result = validator.Validate(null, Rabbit(TimeSpan.FromDays(30)));

        // assert
        result.Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_AZeroRetryDelay_Fails()
    {
        // arrange
        var validator = Validating(TimeSpan.FromMilliseconds(1));

        // act
        var result = validator.Validate(null, Rabbit(TimeSpan.Zero));

        // assert
        result.Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ADelayWellAboveTheClaimLease_Succeeds()
    {
        // arrange
        var validator = Validating(TimeSpan.FromSeconds(5));

        // act
        var result = validator.Validate(null, Rabbit(TimeSpan.FromSeconds(30)));

        // assert
        result.Succeeded.ShouldBeTrue();
        result.FailureMessage.ShouldBeNull();
    }

    private static RabbitMqConsumerOptionsValidator Validating(TimeSpan claimLease) =>
        new(Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { MaxAttempts = 3, ClaimLease = claimLease }));

    private static RabbitMqOptions Rabbit(TimeSpan retryDelay) =>
        new() { HostName = "broker", RetryDelay = retryDelay };
}
