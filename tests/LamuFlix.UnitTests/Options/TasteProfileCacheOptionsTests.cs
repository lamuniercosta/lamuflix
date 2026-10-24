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

    private const string ExpirationFailureMessage = "TasteProfileCache:Expiration must be greater than zero.";

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

    private static Dictionary<string, string?> Valid(string? expiration)
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

        return values;
    }
}
