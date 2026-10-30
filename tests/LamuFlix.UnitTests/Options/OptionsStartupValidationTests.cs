using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LamuFlix.Core.Options;
using LamuFlix.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class OptionsStartupValidationTests
{
    [Fact]
    public void MissingRootPath_FailsValidation()
    {
        var exception = Validate(ValidRequired(rootPath: ""));

        exception.Message.ShouldContain(nameof(LibraryOptions.RootPath));
    }

    [Fact]
    public void MissingApiKey_FailsValidation()
    {
        var exception = Validate(ValidRequired(apiKey: ""));

        exception.Message.ShouldContain(nameof(OmdbOptions.ApiKey));
    }

    [Fact]
    public void MissingBaseUrl_FailsValidation()
    {
        var exception = Validate(ValidRequired(baseUrl: ""));

        exception.Message.ShouldContain(nameof(OmdbOptions.BaseUrl));
    }

    [Fact]
    public void InvalidBaseUrl_FailsValidation()
    {
        var exception = Validate(ValidRequired(baseUrl: "not-a-url"));

        exception.Message.ShouldContain(nameof(OmdbOptions.BaseUrl));
    }

    [Fact]
    public void AbsoluteHttpsBaseUrl_PassesValidation()
    {
        using var provider = Build(ValidRequired());

        foreach (var validator in provider.GetServices<IStartupValidator>())
        {
            validator.Validate();
        }

        provider.GetRequiredService<IOptions<OmdbOptions>>().Value.BaseUrl.ShouldBe("https://www.omdbapi.com/");
    }

    [Fact]
    public void NonLoopbackHttpBaseUrl_FailsWithoutRevealingCredentials()
    {
        const string secret = "super-secret";
        const string baseUrl = "http://example.com/?apikey=super-secret";
        var exception = Validate(ValidRequired(apiKey: secret, baseUrl: baseUrl));

        exception.Message.ShouldContain(nameof(OmdbOptions.BaseUrl));
        exception.Message.ShouldNotContain(secret);
        exception.Message.ShouldNotContain(baseUrl);
    }

    [Fact]
    public void LoopbackHttpBaseUrl_PassesValidation()
    {
        const string baseUrl = "http://localhost:8080";
        using var provider = Build(ValidRequired(baseUrl: baseUrl));

        foreach (var validator in provider.GetServices<IStartupValidator>())
        {
            validator.Validate();
        }

        provider.GetRequiredService<IOptions<OmdbOptions>>().Value.BaseUrl.ShouldBe(baseUrl);
    }

    [Fact]
    public void LoopbackIpHttpBaseUrl_PassesValidation()
    {
        const string baseUrl = "http://127.0.0.1:8080";
        using var provider = Build(ValidRequired(baseUrl: baseUrl));

        foreach (var validator in provider.GetServices<IStartupValidator>())
        {
            validator.Validate();
        }

        provider.GetRequiredService<IOptions<OmdbOptions>>().Value.BaseUrl.ShouldBe(baseUrl);
    }

    [Fact]
    public void UnsupportedSchemeBaseUrl_FailsWithoutRevealingTheUrl()
    {
        const string baseUrl = "ftp://localhost/metadata";
        var exception = Validate(ValidRequired(baseUrl: baseUrl));

        exception.Message.ShouldContain(nameof(OmdbOptions.BaseUrl));
        exception.Message.ShouldNotContain(baseUrl);
    }

    [Fact]
    public void InvalidTmdbBaseUrl_FailsValidation()
    {
        var values = ValidRequired();
        values[$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.BaseUrl)}"] = "not-a-url";

        var exception = Validate(values);

        exception.Message.ShouldContain(nameof(TmdbOptions.BaseUrl));
    }

    [Fact]
    public void NonLoopbackHttpTmdbBaseUrl_FailsValidation()
    {
        var values = ValidRequired();
        values[$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.BaseUrl)}"] = "http://example.com/3/";

        var exception = Validate(values);

        exception.Message.ShouldContain(nameof(TmdbOptions.BaseUrl));
    }

    [Fact]
    public void TmdbDefaults_BindAndPassWithoutAnApiKey()
    {
        using var provider = Build(ValidRequired());

        foreach (var validator in provider.GetServices<IStartupValidator>())
        {
            validator.Validate();
        }

        var options = provider.GetRequiredService<IOptions<TmdbOptions>>().Value;
        options.Enabled.ShouldBeFalse();
        options.BaseUrl.ShouldBe("https://api.themoviedb.org/3/");
        options.ApiKey.ShouldBe(string.Empty);
    }

    [Fact]
    public void ConfiguredTmdbValues_BindThroughTheStartupPath()
    {
        var values = ValidRequired();
        values[$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.Enabled)}"] = "true";
        values[$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.BaseUrl)}"] = "http://127.0.0.1:8080/3/";
        values[$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.ApiKey)}"] = "not-a-real-key";
        using var provider = Build(values);

        foreach (var validator in provider.GetServices<IStartupValidator>())
        {
            validator.Validate();
        }

        var options = provider.GetRequiredService<IOptions<TmdbOptions>>().Value;
        options.Enabled.ShouldBeTrue();
        options.BaseUrl.ShouldBe("http://127.0.0.1:8080/3/");
        options.ApiKey.ShouldBe("not-a-real-key");
    }

    [Fact]
    public void DefaultOmdbDailyLimit_BindsTo1000AndPassesValidation()
    {
        using var provider = Build(ValidRequired());

        foreach (var validator in provider.GetServices<IStartupValidator>())
        {
            validator.Validate();
        }

        provider.GetRequiredService<IOptions<OmdbQuotaOptions>>().Value.DailyLimit.ShouldBe(1000);
    }

    [Fact]
    public void OmdbDailyLimitOne_PassesValidation()
    {
        var values = ValidRequired();
        values[$"{OmdbQuotaOptions.SectionName}:{nameof(OmdbQuotaOptions.DailyLimit)}"] = "1";
        using var provider = Build(values);

        foreach (var validator in provider.GetServices<IStartupValidator>())
        {
            validator.Validate();
        }

        provider.GetRequiredService<IOptions<OmdbQuotaOptions>>().Value.DailyLimit.ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveOmdbDailyLimit_FailsValidation(int dailyLimit)
    {
        var values = ValidRequired();
        values[$"{OmdbQuotaOptions.SectionName}:{nameof(OmdbQuotaOptions.DailyLimit)}"] =
            dailyLimit.ToString(CultureInfo.InvariantCulture);

        var exception = Validate(values);

        exception.Message.ShouldContain(nameof(OmdbQuotaOptions.DailyLimit));
    }

    [Fact]
    public void MissingHostName_FailsValidation()
    {
        var exception = Validate(ValidRequired(hostName: ""));

        exception.Message.ShouldContain(nameof(RabbitMqOptions.HostName));
    }

    [Fact]
    public void ZeroMaxAttempts_FailsValidation()
    {
        var values = ValidRequired();
        values[$"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.MaxAttempts)}"] = "0";

        var exception = Validate(values);

        exception.Message.ShouldContain(nameof(EnrichmentOptions.MaxAttempts));
    }

    [Fact]
    public void RequiredConfig_PassesAndKeepsEnrichmentDefault()
    {
        using var provider = Build(ValidRequired());

        foreach (var validator in provider.GetServices<IStartupValidator>())
        {
            validator.Validate();
        }

        provider.GetRequiredService<IOptions<EnrichmentOptions>>().Value.MaxAttempts.ShouldBe(3);
        provider.GetRequiredService<IOptions<FeatureOptions>>().Value.LocalPlay.ShouldBeTrue();
    }

    [Fact]
    public void InvalidOllamaEndpoint_FailsValidation()
    {
        var values = ValidRequired();
        values[$"{OllamaOptions.SectionName}:{nameof(OllamaOptions.Endpoint)}"] = "ftp://localhost:11434/";

        var exception = Validate(values);

        exception.Message.ShouldContain(nameof(OllamaOptions.Endpoint));
    }

    [Fact]
    public void ZeroOllamaMaxRetryAttempts_FailsValidation()
    {
        var values = ValidRequired();
        values[$"{OllamaResilienceOptions.SectionName}:{nameof(OllamaResilienceOptions.MaxRetryAttempts)}"] = "0";

        var exception = Validate(values);

        exception.Message.ShouldContain(nameof(OllamaResilienceOptions.MaxRetryAttempts));
    }

    [Fact]
    public void ZeroMetadataProviderMaxRetryAttempts_FailsValidation()
    {
        var values = ValidRequired();
        values[$"{MetadataProviderResilienceOptions.SectionName}:{nameof(MetadataProviderResilienceOptions.MaxRetryAttempts)}"] = "0";

        var exception = Validate(values);

        exception.Message.ShouldContain(nameof(MetadataProviderResilienceOptions.MaxRetryAttempts));
    }

    private static OptionsValidationException Validate(Dictionary<string, string?> values)
    {
        using var provider = Build(values);
        var validators = provider.GetServices<IStartupValidator>().ToArray();
        validators.ShouldNotBeEmpty();
        return Should.Throw<OptionsValidationException>(() =>
        {
            foreach (var validator in validators)
            {
                validator.Validate();
            }
        });
    }

    private static ServiceProvider Build(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLamuFlixOptions();
        return services.BuildServiceProvider();
    }

    private static Dictionary<string, string?> ValidRequired(
        string rootPath = @"C:\library",
        string apiKey = "test-key",
        string baseUrl = "https://www.omdbapi.com/",
        string hostName = "localhost") =>
        new()
        {
            [$"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}"] = rootPath,
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}"] = apiKey,
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = baseUrl,
            [$"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}"] = hostName,
            [$"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.ClaimLease)}"] = "00:05:00",
            [$"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.SweepInterval)}"] = "00:01:00",
            [$"{FeatureOptions.SectionName}:{nameof(FeatureOptions.LocalPlay)}"] = "true",
        };
}
