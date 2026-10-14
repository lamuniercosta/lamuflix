using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class OmdbOptionsTests
{
    [Fact]
    public void Defaults_ApiKeyAndBaseUrl_AreEmpty()
    {
        // arrange
        var options = new OmdbOptions();

        // act
        var apiKey = options.ApiKey;
        var baseUrl = options.BaseUrl;

        // assert
        apiKey.ShouldBe(string.Empty);
        baseUrl.ShouldBe(string.Empty);
    }

    [Fact]
    public void TryValidateObject_NonLoopbackHttpBaseUrl_ReportsTheAllowedBaseUrlMessage()
    {
        // arrange
        var options = new OmdbOptions { ApiKey = "test-key", BaseUrl = "http://example.com/" };

        // act
        var results = Validate(options);

        // assert
        results.Select(result => result.ErrorMessage).ShouldContain(
            "The BaseUrl field must be an absolute https URL, or http on a loopback host.");
        results.SelectMany(result => result.MemberNames).ShouldContain(nameof(OmdbOptions.BaseUrl));
    }

    [Fact]
    public void AllowedBaseUrlAttribute_ContextNamesAnotherMember_ReportsThatMember()
    {
        // arrange
        var attribute = typeof(OmdbOptions)
            .GetProperty(nameof(OmdbOptions.BaseUrl))!
            .GetCustomAttributes<ValidationAttribute>()
            .Single(candidate => candidate.GetType().Name == "AllowedBaseUrlAttribute");
        var context = new ValidationContext(new OmdbOptions()) { MemberName = "Endpoint" };

        // act
        var result = attribute.GetValidationResult("http://example.com/", context);

        // assert
        result.ShouldNotBeNull();
        result.MemberNames.ShouldBe(["Endpoint"]);
    }

    private static IReadOnlyList<ValidationResult> Validate(OmdbOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
