using System;
using LamuFlix.Core.Options;
using LamuFlix.ServiceDefaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class EnrichmentOptionsValidatorTests
{
    [Fact]
    public void Validate_WithValidDurationsAndDefaults_Succeeds()
    {
        var result = new EnrichmentOptionsValidator().Validate(
            Microsoft.Extensions.Options.Options.DefaultName,
            new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(1), SweepInterval = TimeSpan.FromSeconds(30) });

        Assert.Equal(ValidateOptionsResult.Success, result);
        Assert.Equal(3, new EnrichmentOptions().MaxAttempts);
    }

    [Theory]
    [InlineData("ClaimLease", 0)]
    [InlineData("ClaimLease", -1)]
    [InlineData("SweepInterval", 0)]
    [InlineData("SweepInterval", -1)]
    public void Validate_WithNonPositiveDurationNamesInvalidKey(string key, int ticks)
    {
        var options = new EnrichmentOptions { ClaimLease = TimeSpan.FromTicks(1), SweepInterval = TimeSpan.FromTicks(1) };
        if (key == nameof(EnrichmentOptions.ClaimLease))
        {
            options = options with { ClaimLease = TimeSpan.FromTicks(ticks) };
        }
        else
        {
            options = options with { SweepInterval = TimeSpan.FromTicks(ticks) };
        }

        var result = new EnrichmentOptionsValidator().Validate(Microsoft.Extensions.Options.Options.DefaultName, options);

        Assert.True(result.Failed);
        Assert.Contains(key, string.Join("; ", result.Failures), StringComparison.Ordinal);
    }

    [Fact]
    public void AddLamuFlixOptions_RegistersValidatorAndStartupValidation()
    {
        var services = new ServiceCollection();

        services.AddLamuFlixOptions();

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IValidateOptions<EnrichmentOptions>)
            && descriptor.ImplementationType == typeof(EnrichmentOptionsValidator));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IStartupValidator));
    }
}