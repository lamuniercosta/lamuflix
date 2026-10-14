using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class RabbitMqOptionsSurvivorTests
{
    [Fact]
    public void Defaults_TextMembers_AreEmpty()
    {
        // arrange
        var options = new RabbitMqOptions();

        // act
        var hostName = options.HostName;
        var userName = options.UserName;
        var password = options.Password;

        // assert
        hostName.ShouldBe(string.Empty);
        userName.ShouldBe(string.Empty);
        password.ShouldBe(string.Empty);
    }

    [Fact]
    public void ToString_AllMembers_PrintsEveryLabelledValueWithRedactedPassword()
    {
        // arrange
        var options = new RabbitMqOptions
        {
            HostName = "broker.internal",
            Port = 5673,
            UserName = "lamuflix",
            Password = "super-secret-value",
            RetryDelay = TimeSpan.FromSeconds(45),
            Prefetch = 7,
        };

        // act
        var printed = options.ToString();

        // assert
        printed.ShouldBe(
            "RabbitMqOptions { HostName = broker.internal, Port = 5673, UserName = lamuflix, Password = ***, RetryDelay = 00:00:45, Prefetch = 7 }");
    }

    [Theory]
    [InlineData(1d, true)]
    [InlineData(int.MaxValue, true)]
    [InlineData(int.MaxValue + 1d, false)]
    public void TryValidateObject_RetryDelayAtIntegerMillisecondBoundary_MatchesTheRange(double milliseconds, bool expectedValid)
    {
        // arrange
        var options = new RabbitMqOptions
        {
            HostName = "localhost",
            RetryDelay = TimeSpan.FromMilliseconds(milliseconds),
        };

        // act
        var results = Validate(options);

        // assert
        if (expectedValid)
        {
            results.ShouldBeEmpty();
        }
        else
        {
            results.SelectMany(result => result.MemberNames).ShouldContain(nameof(RabbitMqOptions.RetryDelay));
        }
    }

    [Fact]
    public void TryValidateObject_InvalidRetryDelay_ReportsTheMillisecondLimitMessage()
    {
        // arrange
        var options = new RabbitMqOptions { HostName = "localhost", RetryDelay = TimeSpan.Zero };

        // act
        var results = Validate(options);

        // assert
        results.Select(result => result.ErrorMessage).ShouldContain(
            "RetryDelay must be positive and expressible as a whole number of milliseconds up to 2147483647.");
    }

    private static IReadOnlyList<ValidationResult> Validate(RabbitMqOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
