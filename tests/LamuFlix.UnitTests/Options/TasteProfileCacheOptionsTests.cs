using System;
using System.Collections.Generic;
using System.Globalization;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class TasteProfileCacheOptionsTests
{
    private const string ExpirationKey = "TasteProfileCache:Expiration";

    private const string WarningFractionKey = "TasteProfileCache:WarningFraction";

    private const string ExpirationFailureMessage = "TasteProfileCache:Expiration must be greater than zero.";

    private const string WarningFractionFailureMessage =
        "TasteProfileCache:WarningFraction must be finite, greater than zero, and at most one.";

    [Fact]
    public void SectionName_IsTasteProfileCache()
    {
        // act
        var sectionName = TasteProfileCacheOptions.SectionName;

        // assert
        sectionName.ShouldBe("TasteProfileCache");
    }

    [Fact]
    public void Default_Expiration_IsFiveMinutes()
    {
        // arrange
        var options = new TasteProfileCacheOptions();

        // act
        var expiration = options.Expiration;

        // assert
        expiration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void DefaultWarningFraction_IsTheConstDefault()
    {
        // arrange
        var options = new TasteProfileCacheOptions();

        // act
        var warningFraction = options.WarningFraction;

        // assert
        TasteProfileCacheOptions.DefaultWarningFraction.ShouldBe(0.8);
        warningFraction.ShouldBe(TasteProfileCacheOptions.DefaultWarningFraction);
    }

    [Theory]
    [InlineData("00:10:00")]
    [InlineData("00:00:01")]
    public void PositiveExpiration_BindsFromSection(string configured)
    {
        // arrange
        using var provider = Compose(Configuration(Valid(configured))).BuildServiceProvider();

        // act
        var expiration = provider.GetRequiredService<IOptions<TasteProfileCacheOptions>>().Value.Expiration;

        // assert
        expiration.ShouldBe(TimeSpan.Parse(configured, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void MissingExpiration_FallsBackToFiveMinutes()
    {
        // arrange
        using var provider = Compose(Configuration(Valid(null))).BuildServiceProvider();

        // act
        var expiration = provider.GetRequiredService<IOptions<TasteProfileCacheOptions>>().Value.Expiration;

        // assert
        expiration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("-00:05:00")]
    public void NonPositiveExpiration_FailsStartupValidation(string configured)
    {
        // arrange
        using var provider = Compose(Configuration(Valid(configured))).BuildServiceProvider();

        // act
        var exception = Should.Throw<OptionsValidationException>(() =>
        {
            foreach (var validator in provider.GetServices<IStartupValidator>())
            {
                validator.Validate();
            }
        });

        // assert
        exception.Failures.ShouldContain(ExpirationFailureMessage);
    }

    [Theory]
    [InlineData("0.5")]
    [InlineData("0.8")]
    [InlineData("0.0001")]
    public void FiniteWarningFractionWithinDomain_BindsFromSection(string configured)
    {
        // arrange
        using var provider = Compose(Configuration(Valid("00:05:00", configured))).BuildServiceProvider();

        // act
        var warningFraction = provider.GetRequiredService<IOptions<TasteProfileCacheOptions>>().Value.WarningFraction;

        // assert
        warningFraction.ShouldBe(double.Parse(configured, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void WarningFractionAtOne_IsAcceptedAsTheUpperBound()
    {
        // arrange
        using var provider = Compose(Configuration(Valid("00:05:00", "1"))).BuildServiceProvider();

        // act
        var warningFraction = provider.GetRequiredService<IOptions<TasteProfileCacheOptions>>().Value.WarningFraction;

        // assert
        warningFraction.ShouldBe(1.0);
    }

    [Fact]
    public void MissingWarningFraction_FallsBackToTheConstDefault()
    {
        // arrange
        using var provider = Compose(Configuration(Valid("00:05:00"))).BuildServiceProvider();

        // act
        var warningFraction = provider.GetRequiredService<IOptions<TasteProfileCacheOptions>>().Value.WarningFraction;

        // assert
        warningFraction.ShouldBe(0.8);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.5")]
    [InlineData("1.0001")]
    [InlineData("2")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    public void NonFiniteOrOutOfDomainWarningFraction_FailsStartupValidation(string configured)
    {
        // arrange
        using var provider = Compose(Configuration(Valid("00:05:00", configured))).BuildServiceProvider();

        // act
        var exception = Should.Throw<OptionsValidationException>(() =>
        {
            foreach (var validator in provider.GetServices<IStartupValidator>())
            {
                validator.Validate();
            }
        });

        // assert
        exception.Failures.ShouldContain(WarningFractionFailureMessage);
    }

    private static ServiceCollection Compose(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLamuFlixOptions();
        services.AddLogging();
        services.AddLamuFlixPersistence(configuration);
        return services;
    }

    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static Dictionary<string, string?> Valid(string? expiration, string? warningFraction = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused;Username=unused;Password=unused",
            [$"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}"] = "C:/library",
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}"] = "not-a-real-key",
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = "https://example.invalid/",
            [$"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}"] = "localhost",
            ["Enrichment:ClaimLease"] = "00:05:00",
            ["Enrichment:SweepInterval"] = "00:01:00",
        };

        if (expiration is not null)
        {
            values[ExpirationKey] = expiration;
        }

        if (warningFraction is not null)
        {
            values[WarningFractionKey] = warningFraction;
        }

        return values;
    }
}
