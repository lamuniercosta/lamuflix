using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class SecondaryMetadataProviderOptionsPropertyTests
{
    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool TryValidateObject_HttpsBaseUrlWithAnyHostAndPort_HasNoBaseUrlErrors(uint hostSeed, int portSeed)
    {
        // arrange
        var options = new TmdbOptions
        {
            BaseUrl = $"https://host{hostSeed % 100_000}.example.test:{Port(portSeed)}/3/"
        };

        // act
        var errors = BaseUrlErrors(options);

        // assert
        return errors.Count == 0;
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool TryValidateObject_HttpLoopbackBaseUrlWithAnyPort_HasNoBaseUrlErrors(int portSeed)
    {
        // arrange
        var options = new TmdbOptions
        {
            BaseUrl = $"http://127.0.0.1:{Port(portSeed)}/3/"
        };

        // act
        var errors = BaseUrlErrors(options);

        // assert
        return errors.Count == 0;
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool TryValidateObject_HttpNonLoopbackBaseUrlWithAnyHostAndPort_ReportsBaseUrl(uint hostSeed, int portSeed)
    {
        // arrange
        var options = new TmdbOptions
        {
            BaseUrl = $"http://host{hostSeed % 100_000}.example.test:{Port(portSeed)}/3/"
        };

        // act
        var errors = BaseUrlErrors(options);

        // assert
        return errors.Count != 0;
    }

    private static int Port(int seed) => 1 + (int)((uint)seed % 65534);

    private static IReadOnlyList<ValidationResult> BaseUrlErrors(TmdbOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return [.. results.Where(result => result.MemberNames.Contains(nameof(TmdbOptions.BaseUrl)))];
    }
}
