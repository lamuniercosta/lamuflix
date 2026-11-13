using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LamuFlix.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.Pipeline;

public sealed class EnrichmentOptionsTests
{
    [Fact]
    public void Defaults_LengthenSweepIntervalForIdleCatalog()
    {
        // arrange
        // act
        var options = new EnrichmentOptions();

        // assert
        options.SweepInterval.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void SweepInterval_IsBoundFromConfiguration()
    {
        // arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Enrichment:SweepInterval"] = "00:10:00",
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<EnrichmentOptions>().BindConfiguration(EnrichmentOptions.SectionName);

        // act
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<EnrichmentOptions>>().Value;

        // assert
        options.SweepInterval.ShouldBe(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void TryValidateObject_MaxAttemptsZero_IsInvalid()
    {
        // arrange
        var options = new EnrichmentOptions { MaxAttempts = 0 };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // act
        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        // assert
        valid.ShouldBeFalse();
        results.Count.ShouldBe(1);
        results[0].MemberNames.ShouldContain(nameof(EnrichmentOptions.MaxAttempts));
    }

    [Fact]
    public void TryValidateObject_MaxAttemptsOne_IsValid()
    {
        // arrange
        var options = new EnrichmentOptions { MaxAttempts = 1 };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // act
        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        // assert
        valid.ShouldBeTrue();
        results.ShouldBeEmpty();
    }
}
