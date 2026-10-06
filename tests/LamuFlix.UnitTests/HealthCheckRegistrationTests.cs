using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests;

public sealed class HealthCheckRegistrationTests
{
    private const string PlaceholderConnectionString =
        "Host=localhost;Database=unused;Username=unused;Password=unused";

    [Fact]
    public void ProductionRegistrations_ReadyGroup_IsPostgresAndRabbitMqWithUntaggedMetadataProvider()
    {
        // arrange
        var services = ComposeProductionHealthRegistrations();

        // act
        using var provider = services.BuildServiceProvider();
        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;
        var ready = registrations.Where(registration => registration.Tags.Contains(HealthCheckTags.Ready)).ToArray();
        var metadata = registrations.Single(registration => registration.Name == "metadata-provider");

        // assert
        ready.Length.ShouldBe(2);
        ready.Select(registration => registration.Name).OrderBy(name => name)
            .ShouldBe(["postgres", "rabbitmq"]);
        metadata.Tags.ShouldBeEmpty();
    }

    private static ServiceCollection ComposeProductionHealthRegistrations()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(ValidConfiguration()).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLamuFlixOptions();
        services.AddLamuFlixPersistence(configuration);
        services.AddMetadataProvider();
        services.AddLamuFlixRabbitMq();
        return services;
    }

    private static Dictionary<string, string?> ValidConfiguration() =>
        new()
        {
            ["ConnectionStrings:DefaultConnection"] = PlaceholderConnectionString,
            [$"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}"] = "C:/library",
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}"] = "not-a-real-key",
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = "https://example.invalid/",
            [$"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}"] = "localhost",
            ["Enrichment:ClaimLease"] = "00:05:00",
        };
}
