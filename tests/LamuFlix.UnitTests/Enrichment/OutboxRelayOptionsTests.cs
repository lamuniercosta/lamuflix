using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using LamuFlix.Infrastructure.Enrichment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.Enrichment;

public sealed class OutboxRelayOptionsTests
{
    private static readonly string[] PositiveDurationFields =
    [
        nameof(OutboxRelayOptions.PollInterval),
        nameof(OutboxRelayOptions.PublishTimeout),
        nameof(OutboxRelayOptions.FailureBackoffMin),
        nameof(OutboxRelayOptions.ProcessedRetention),
        nameof(OutboxRelayOptions.CleanupInterval),
        nameof(OutboxRelayOptions.BacklogAgeWarning),
    ];

    public static TheoryData<int, bool> BatchSizeEdges =>
        new()
        {
            { 1, true },
            { 1000, true },
            { 100, true },
            { 0, false },
            { -1, false },
            { 1001, false },
        };

    public static TheoryData<int, bool> CleanupBatchSizeEdges =>
        new()
        {
            { 1, true },
            { 10000, true },
            { 1000, true },
            { 0, false },
            { -1, false },
            { 10001, false },
        };

    public static TheoryData<string, string, bool> PositiveDurationEdges
    {
        get
        {
            var data = new TheoryData<string, string, bool>();
            foreach (var memberName in PositiveDurationFields)
            {
                data.Add(memberName, "00:00:00.0000001", true);
                data.Add(memberName, "7.00:00:00", true);
                data.Add(memberName, "00:00:00", false);
                data.Add(memberName, "-00:00:00.0000001", false);
            }

            return data;
        }
    }

    [Fact]
    public void Defaults_MatchApprovedOperationalValues()
    {
        // arrange
        // act
        var options = new OutboxRelayOptions();

        // assert
        options.BatchSize.ShouldBe(100);
        options.PollInterval.ShouldBe(TimeSpan.FromSeconds(1));
        options.PublishTimeout.ShouldBe(TimeSpan.FromSeconds(30));
        options.FailureBackoffMin.ShouldBe(TimeSpan.FromSeconds(1));
        options.FailureBackoffMax.ShouldBe(TimeSpan.FromSeconds(60));
        options.ProcessedRetention.ShouldBe(TimeSpan.FromDays(7));
        options.CleanupInterval.ShouldBe(TimeSpan.FromHours(1));
        options.CleanupBatchSize.ShouldBe(1000);
        options.BacklogAgeWarning.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void SectionName_IsOutboxRelay()
    {
        // arrange
        // act
        // assert
        OutboxRelayOptions.SectionName.ShouldBe("OutboxRelay");
    }

    [Theory]
    [MemberData(nameof(BatchSizeEdges))]
    public void BatchSize_IsValidatedWithinApprovedBounds(int value, bool valid)
    {
        // arrange
        var options = new OutboxRelayOptions { BatchSize = value };

        // act
        // assert
        AssertFieldOutcome(options, nameof(OutboxRelayOptions.BatchSize), valid);
    }

    [Theory]
    [MemberData(nameof(CleanupBatchSizeEdges))]
    public void CleanupBatchSize_IsValidatedWithinApprovedBounds(int value, bool valid)
    {
        // arrange
        var options = new OutboxRelayOptions { CleanupBatchSize = value };

        // act
        // assert
        AssertFieldOutcome(options, nameof(OutboxRelayOptions.CleanupBatchSize), valid);
    }

    [Theory]
    [MemberData(nameof(PositiveDurationEdges))]
    public void PositiveDurationFields_RequireStrictlyPositiveValues(string memberName, string duration, bool valid)
    {
        // arrange
        var options = WithDuration(new OutboxRelayOptions(), memberName, ParseDuration(duration));

        // act
        // assert
        AssertFieldOutcome(options, memberName, valid);
    }

    [Theory]
    [InlineData("00:00:00.0000001", false)]
    [InlineData("00:00:00.9999999", false)]
    [InlineData("00:00:01", true)]
    [InlineData("00:01:00", true)]
    [InlineData("01:00:00", true)]
    public void FailureBackoffMax_MustBeAtLeastFailureBackoffMin(string max, bool valid)
    {
        // arrange
        var options = new OutboxRelayOptions
        {
            FailureBackoffMin = TimeSpan.FromSeconds(1),
            FailureBackoffMax = ParseDuration(max),
        };

        // act
        // assert
        AssertFieldOutcome(options, nameof(OutboxRelayOptions.FailureBackoffMax), valid);
    }

    [Fact]
    public void Validate_NullContext_ThrowsArgumentNullException()
    {
        // arrange
        var options = new OutboxRelayOptions();

        // act
        // assert
        // ReSharper disable once NullableWarningSuppressionIsUsed // the null argument exercises the guard clause under test.
        Should.Throw<ArgumentNullException>(() => options.Validate(null!).ToArray());
    }

    [Fact]
    public void Validator_NullOptions_ThrowsArgumentNullExceptionNamingOptions()
    {
        // arrange
        var validator = new OutboxRelayOptionsValidator();

        // act
        // assert
        // ReSharper disable once NullableWarningSuppressionIsUsed // the null argument exercises the guard clause under test.
        var thrown = Should.Throw<ArgumentNullException>(() => validator.Validate(null, null!));
        thrown.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Validator_ValidOptions_Succeeds()
    {
        // arrange
        var options = new OutboxRelayOptions();

        // act
        var result = new OutboxRelayOptionsValidator().Validate(null, options);

        // assert
        result.ShouldBe(ValidateOptionsResult.Success);
    }

    [Fact]
    public void StartupValidation_WithoutConfiguration_AcceptsApprovedDefaults()
    {
        // arrange
        using var provider = BuildProvider([]);

        // act
        ValidateStartup(provider);

        // assert
        provider.GetRequiredService<IOptions<OutboxRelayOptions>>().Value.ShouldBe(new OutboxRelayOptions());
    }

    [Fact]
    public void StartupValidation_ZeroBatchSize_FailsNamingBatchSize()
    {
        // arrange
        // act
        var exception = StartupFailure(new() { [$"{OutboxRelayOptions.SectionName}:BatchSize"] = "0" });

        // assert
        exception.Message.ShouldContain(nameof(OutboxRelayOptions.BatchSize));
    }

    [Fact]
    public void StartupValidation_ZeroPollInterval_FailsNamingPollInterval()
    {
        // arrange
        // act
        var exception = StartupFailure(new() { [$"{OutboxRelayOptions.SectionName}:PollInterval"] = "00:00:00" });

        // assert
        exception.Message.ShouldContain(nameof(OutboxRelayOptions.PollInterval));
    }

    [Fact]
    public void StartupValidation_CleanupBatchSizeAboveMaximum_FailsNamingCleanupBatchSize()
    {
        // arrange
        // act
        var exception = StartupFailure(new() { [$"{OutboxRelayOptions.SectionName}:CleanupBatchSize"] = "10001" });

        // assert
        exception.Message.ShouldContain(nameof(OutboxRelayOptions.CleanupBatchSize));
    }

    [Fact]
    public void StartupValidation_BackoffMaxBelowMin_FailsNamingBothBackoffFields()
    {
        // arrange
        // act
        var exception = StartupFailure(new()
        {
            [$"{OutboxRelayOptions.SectionName}:FailureBackoffMin"] = "00:00:05",
            [$"{OutboxRelayOptions.SectionName}:FailureBackoffMax"] = "00:00:01",
        });

        // assert
        exception.Message.ShouldContain(nameof(OutboxRelayOptions.FailureBackoffMax));
        exception.Message.ShouldContain(nameof(OutboxRelayOptions.FailureBackoffMin));
    }

    [Fact]
    public void StartupValidation_ConfiguredValues_AreBound()
    {
        // arrange
        using var provider = BuildProvider(new()
        {
            [$"{OutboxRelayOptions.SectionName}:BatchSize"] = "7",
            [$"{OutboxRelayOptions.SectionName}:PollInterval"] = "00:00:00.250",
            [$"{OutboxRelayOptions.SectionName}:CleanupBatchSize"] = "25",
        });

        // act
        ValidateStartup(provider);

        // assert
        var options = provider.GetRequiredService<IOptions<OutboxRelayOptions>>().Value;
        options.BatchSize.ShouldBe(7);
        options.PollInterval.ShouldBe(TimeSpan.FromMilliseconds(250));
        options.CleanupBatchSize.ShouldBe(25);
        options.PublishTimeout.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void AddLamuFlixOutboxRelay_RegistersStateAsSingleton()
    {
        // arrange
        using var provider = BuildProvider([]);

        // act
        var first = provider.GetRequiredService<OutboxRelayState>();
        var second = provider.GetRequiredService<OutboxRelayState>();

        // assert
        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void AddLamuFlixOutboxRelay_NullServices_ThrowsArgumentNullException()
    {
        // arrange
        // act
        // assert
        // ReSharper disable once NullableWarningSuppressionIsUsed // the null argument exercises the guard clause under test.
        Should.Throw<ArgumentNullException>(() => OutboxRelayServiceCollectionExtensions.AddLamuFlixOutboxRelay(null!));
    }

    private static void AssertFieldOutcome(OutboxRelayOptions options, string memberName, bool valid)
    {
        var results = options.Validate(new ValidationContext(options)).ToArray();
        if (valid)
        {
            results.ShouldBeEmpty();
            return;
        }

        results.ShouldContain(result =>
            result.MemberNames.Contains(memberName, StringComparer.Ordinal));
    }

    private static TimeSpan ParseDuration(string value) => TimeSpan.Parse(value, CultureInfo.InvariantCulture);

    private static OutboxRelayOptions WithDuration(OutboxRelayOptions options, string memberName, TimeSpan value) =>
        memberName switch
        {
            nameof(OutboxRelayOptions.PollInterval) => options with { PollInterval = value },
            nameof(OutboxRelayOptions.PublishTimeout) => options with { PublishTimeout = value },
            nameof(OutboxRelayOptions.FailureBackoffMin) => options with
            {
                FailureBackoffMin = value,
                FailureBackoffMax = value > options.FailureBackoffMax ? value : options.FailureBackoffMax,
            },
            nameof(OutboxRelayOptions.ProcessedRetention) => options with { ProcessedRetention = value },
            nameof(OutboxRelayOptions.CleanupInterval) => options with { CleanupInterval = value },
            nameof(OutboxRelayOptions.BacklogAgeWarning) => options with { BacklogAgeWarning = value },
            _ => throw new ArgumentOutOfRangeException(
                nameof(memberName),
                memberName,
                "Unmapped positive duration member."),
        };

    private static OptionsValidationException StartupFailure(Dictionary<string, string?> values)
    {
        using var provider = BuildProvider(values);
        return Should.Throw<OptionsValidationException>(() => ValidateStartup(provider));
    }

    private static void ValidateStartup(IServiceProvider provider)
    {
        var validators = provider.GetServices<IStartupValidator>().ToArray();
        validators.ShouldNotBeEmpty();
        foreach (var validator in validators)
        {
            validator.Validate();
        }
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLamuFlixOutboxRelay();
        return services.BuildServiceProvider();
    }
}
