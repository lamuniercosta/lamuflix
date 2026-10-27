using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class SecondaryMetadataProviderOptionsTests
{
    [Fact]
    public void Defaults_AllValues_MatchDocumentedContract()
    {
        // arrange
        var options = new TmdbOptions();

        // act
        var enabled = options.Enabled;
        var baseUrl = options.BaseUrl;
        var apiKey = options.ApiKey;

        // assert
        enabled.ShouldBeFalse();
        baseUrl.ShouldBe("https://api.themoviedb.org/3/");
        apiKey.ShouldBe(string.Empty);
    }

    [Fact]
    public void TryValidateObject_AbsoluteHttpsBaseUrl_ReturnsNoErrors()
    {
        // arrange
        var options = new TmdbOptions { BaseUrl = "https://api.themoviedb.org/3/" };

        // act
        var results = Validate(options);

        // assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void TryValidateObject_LoopbackHttpBaseUrl_ReturnsNoErrors()
    {
        // arrange
        var options = new TmdbOptions { BaseUrl = "http://localhost:8080/3/" };

        // act
        var results = Validate(options);

        // assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void TryValidateObject_NonLoopbackHttpBaseUrl_ReportsTheAllowedBaseUrlMessage()
    {
        // arrange
        var options = new TmdbOptions { BaseUrl = "http://example.com/3/" };

        // act
        var results = Validate(options);

        // assert
        results.Select(result => result.ErrorMessage).ShouldContain(
            "The BaseUrl field must be an absolute https URL, or http on a loopback host.");
        results.SelectMany(result => result.MemberNames).ShouldContain(nameof(TmdbOptions.BaseUrl));
    }

    [Fact]
    public void TryValidateObject_RelativeBaseUrl_ReportsTheBaseUrlMember()
    {
        // arrange
        var options = new TmdbOptions { BaseUrl = "/3/" };

        // act
        var results = Validate(options);

        // assert
        results.SelectMany(result => result.MemberNames).ShouldContain(nameof(TmdbOptions.BaseUrl));
    }

    [Theory]
    [MemberData(nameof(MissingApiKeyCases))]
    public void TryValidateObject_ApiKeyAbsent_ReturnsNoErrors(bool enabled)
    {
        // arrange
        var options = new TmdbOptions { Enabled = enabled, ApiKey = string.Empty };

        // act
        var results = Validate(options);

        // assert
        results.ShouldBeEmpty();
    }

    public static TheoryData<bool> MissingApiKeyCases => new() { false, true };

    private static IReadOnlyList<ValidationResult> Validate(TmdbOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
