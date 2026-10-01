using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class RabbitMqOptionsTests
{
    [Fact]
    public void Defaults_RetryDelayAndPrefetch_UseTheAgreedValues()
    {
        // arrange
        var options = new RabbitMqOptions { HostName = "localhost" };

        // act
        var retryDelay = options.RetryDelay;
        var prefetch = options.Prefetch;

        // assert
        retryDelay.ShouldBe(TimeSpan.FromSeconds(30));
        prefetch.ShouldBe((ushort)1);
    }

    [Fact]
    public void TryValidateObject_Defaults_AreValid()
    {
        // arrange
        var options = new RabbitMqOptions { HostName = "localhost" };

        // act
        var results = Validate(options);

        // assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void TryValidateObject_ZeroRetryDelay_IsInvalid()
    {
        // arrange
        var options = new RabbitMqOptions { HostName = "localhost", RetryDelay = TimeSpan.Zero };

        // act
        var results = Validate(options);

        // assert
        ShouldReject(results, nameof(RabbitMqOptions.RetryDelay));
    }

    [Fact]
    public void TryValidateObject_NegativeRetryDelay_IsInvalid()
    {
        // arrange
        var options = new RabbitMqOptions { HostName = "localhost", RetryDelay = TimeSpan.FromSeconds(-1) };

        // act
        var results = Validate(options);

        // assert
        ShouldReject(results, nameof(RabbitMqOptions.RetryDelay));
    }

    [Fact]
    public void TryValidateObject_RetryDelayBeyondIntegerMilliseconds_IsInvalid()
    {
        // arrange
        var options = new RabbitMqOptions
        {
            HostName = "localhost",
            RetryDelay = TimeSpan.FromDays(30),
        };

        // act
        var results = Validate(options);

        // assert
        ShouldReject(results, nameof(RabbitMqOptions.RetryDelay));
    }

    [Fact]
    public void TryValidateObject_SubMillisecondRetryDelay_IsInvalid()
    {
        // arrange
        var options = new RabbitMqOptions
        {
            HostName = "localhost",
            RetryDelay = TimeSpan.FromTicks(TimeSpan.TicksPerMillisecond / 10),
        };

        // act
        var results = Validate(options);

        // assert
        ShouldReject(results, nameof(RabbitMqOptions.RetryDelay));
    }

    [Fact]
    public void TryValidateObject_ZeroPrefetch_IsInvalid()
    {
        // arrange
        var options = new RabbitMqOptions { HostName = "localhost", Prefetch = 0 };

        // act
        var results = Validate(options);

        // assert
        ShouldReject(results, nameof(RabbitMqOptions.Prefetch));
    }

    [Fact]
    public void TryValidateObject_PositivePrefetch_IsValid()
    {
        // arrange
        var options = new RabbitMqOptions { HostName = "localhost", Prefetch = 32 };

        // act
        var results = Validate(options);

        // assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void ToString_Password_IsRedacted()
    {
        // arrange
        var options = new RabbitMqOptions
        {
            HostName = "localhost",
            UserName = "lamuflix",
            Password = "super-secret-value",
        };

        // act
        var printed = options.ToString();

        // assert
        printed.ShouldNotContain("super-secret-value");
        printed.ShouldContain("Password = ***");
    }

    [Fact]
    public void ToString_NonSecretMembers_AreStillPrinted()
    {
        // arrange
        var options = new RabbitMqOptions { HostName = "broker.internal", UserName = "lamuflix" };

        // act
        var printed = options.ToString();

        // assert
        printed.ShouldContain("broker.internal");
        printed.ShouldContain("lamuflix");
    }

    private static IReadOnlyList<ValidationResult> Validate(RabbitMqOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }

    private static void ShouldReject(IReadOnlyList<ValidationResult> results, string memberName)
    {
        results.ShouldNotBeEmpty();
        results.SelectMany(result => result.MemberNames).ShouldContain(memberName);
    }
}