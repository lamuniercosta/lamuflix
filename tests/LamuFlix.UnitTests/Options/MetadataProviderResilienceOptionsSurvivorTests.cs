using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class MetadataProviderResilienceOptionsSurvivorTests
{
    [Fact]
    public void TryValidateObject_SamplingDurationBelowTwiceAttemptTimeout_ReportsTheCrossFieldMessage()
    {
        // arrange
        var options = new MetadataProviderResilienceOptions
        {
            AttemptTimeout = TimeSpan.FromSeconds(10),
            TotalTimeout = TimeSpan.FromSeconds(30),
            SamplingDuration = TimeSpan.FromSeconds(19),
        };

        // act
        var results = Validate(options);

        // assert
        results.Select(result => result.ErrorMessage).ShouldContain(
            "SamplingDuration must be at least twice AttemptTimeout; got 00:00:19 and 00:00:10.");
        results.SelectMany(result => result.MemberNames).ShouldContain(nameof(MetadataProviderResilienceOptions.SamplingDuration));
    }

    [Fact]
    public void TryValidateObject_AttemptTimeoutAboveTotalTimeout_ReportsTheCrossFieldMessage()
    {
        // arrange
        var options = new MetadataProviderResilienceOptions
        {
            AttemptTimeout = TimeSpan.FromSeconds(10),
            TotalTimeout = TimeSpan.FromSeconds(5),
        };

        // act
        var results = Validate(options);

        // assert
        results.Select(result => result.ErrorMessage).ShouldContain(
            "AttemptTimeout must be less than or equal to TotalTimeout; got 00:00:10 and 00:00:05.");
        results.SelectMany(result => result.MemberNames).ShouldContain(nameof(MetadataProviderResilienceOptions.AttemptTimeout));
    }

    private static IReadOnlyList<ValidationResult> Validate(MetadataProviderResilienceOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
