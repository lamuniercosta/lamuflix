using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class OllamaOptionsTests
{
    [Fact]
    public void Defaults_EndpointAndModelName_MatchTheAgreedValues()
    {
        // arrange
        var options = new OllamaOptions();

        // act
        var endpoint = options.Endpoint;
        var modelName = options.ModelName;

        // assert
        endpoint.ShouldBe("http://localhost:11434/");
        modelName.ShouldBe("all-minilm");
    }

    [Fact]
    public void SectionName_IsTheVendorMandatedConfigurationKey()
    {
        // arrange
        var sectionName = OllamaOptions.SectionName;

        // act
        var isExpected = string.Equals(sectionName, "Ollama", StringComparison.Ordinal);

        // assert
        isExpected.ShouldBeTrue();
    }

    [Fact]
    public void TryValidateObject_Defaults_AreValid()
    {
        // arrange
        var options = new OllamaOptions();

        // act
        var results = Validate(options);

        // assert
        results.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("http://localhost:11434/", true)]
    [InlineData("https://ollama.example/", true)]
    [InlineData("HTTP://localhost:11434/", true)]
    [InlineData("https://ollama.example/api/", true)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("localhost:11434", false)]
    [InlineData("/relative/path", false)]
    [InlineData("ftp://localhost:11434/", false)]
    [InlineData("file:///tmp/ollama", false)]
    public void TryValidateObject_Endpoint_RequiresAnAbsoluteHttpOrHttpsUrl(string endpoint, bool expectedValid)
    {
        // arrange
        var options = new OllamaOptions { Endpoint = endpoint };

        // act
        var results = Validate(options);

        // assert
        if (expectedValid)
        {
            results.ShouldBeEmpty();
            return;
        }

        results.SelectMany(result => result.MemberNames).ShouldContain(nameof(OllamaOptions.Endpoint));
    }

    [Fact]
    public void TryValidateObject_NonAbsoluteEndpoint_MessageNamesTheHttpOrHttpsRule()
    {
        // arrange
        var options = new OllamaOptions { Endpoint = "ftp://localhost:11434/" };

        // act
        var results = Validate(options);

        // assert
        results.Select(result => result.ErrorMessage).ShouldContain("The Endpoint field must be an absolute http or https URL.");
    }

    [Theory]
    [InlineData("all-minilm", true)]
    [InlineData("nomic-embed-text", true)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("  \t ", false)]
    public void TryValidateObject_ModelName_RequiresANonBlankName(string modelName, bool expectedValid)
    {
        // arrange
        var options = new OllamaOptions { ModelName = modelName };

        // act
        var results = Validate(options);

        // assert
        if (expectedValid)
        {
            results.ShouldBeEmpty();
            return;
        }

        results.SelectMany(result => result.MemberNames).ShouldContain(nameof(OllamaOptions.ModelName));
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Validate_AbsoluteHttpOrHttpsEndpoint_IsAccepted(bool useHttps, uint hostSeed)
    {
        var scheme = useHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
        var options = new OllamaOptions
        {
            Endpoint = $"{scheme}://host{hostSeed % 100_000}.example/",
            ModelName = "all-minilm",
        };

        return Validate(options).Count == 0;
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Validate_Endpoint_MatchesTheAbsoluteHttpOrHttpsRule(string endpoint)
    {
        var options = new OllamaOptions { Endpoint = endpoint, ModelName = "all-minilm" };

        var expected = Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));

        return (Validate(options).Count == 0) == expected;
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Validate_ModelName_IsValidExactlyWhenItIsNotBlank(string modelName)
    {
        var options = new OllamaOptions { ModelName = modelName };

        return (Validate(options).Count == 0) == !string.IsNullOrWhiteSpace(modelName);
    }

    private static IReadOnlyList<ValidationResult> Validate(OllamaOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
