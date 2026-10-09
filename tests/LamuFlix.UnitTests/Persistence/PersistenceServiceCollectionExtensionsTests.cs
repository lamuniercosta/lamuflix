using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.ServiceDefaults;
using LamuFlix.UnitTests.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.Persistence;

public sealed class PersistenceServiceCollectionExtensionsTests
{
    private const string PlaceholderConnectionString =
        "Host=localhost;Database=unused;Username=unused;Password=unused";

    private const string ClaimLeaseKey = "Enrichment:ClaimLease";

    private const string ClaimLeaseFailureMessage =
        "Enrichment:ClaimLease must be explicitly configured and greater than zero when production persistence is registered";

    private static readonly DateTimeOffset FixedNow = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AddLamuFlixPersistence_MissingConnectionString_ThrowsNamingUserSecrets()
    {
        // arrange
        var configuration = Configuration(WithoutConnectionString());

        // act
        var exception = Should.Throw<InvalidOperationException>(() => Compose(configuration));

        // assert
        exception.Message.ShouldContain("user secrets");
        exception.Message.ShouldContain("ConnectionStrings__DefaultConnection");
    }

    [Fact]
    public void AddLamuFlixPersistence_WhitespaceConnectionString_ThrowsWithoutEchoingTheValue()
    {
        // arrange
        const string whitespace = " \t ";
        var configuration = Configuration(WithConnectionString(whitespace));

        // act
        var exception = Should.Throw<InvalidOperationException>(() => Compose(configuration));

        // assert
        exception.Message.ShouldContain("user secrets");
        exception.Message.ShouldContain("ConnectionStrings__DefaultConnection");
        exception.Message.ShouldNotContain(whitespace);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("00:00:00")]
    [InlineData("-00:05:00")]
    public void AddLamuFlixPersistence_InvalidClaimLease_FailsStartupValidation(string? lease)
    {
        // act
        var failures = StartupValidationFailures(Configuration(WithClaimLease(lease)));

        // assert
        failures.ShouldContain(ClaimLeaseFailureMessage);
        failures.ShouldContain("ClaimLease must be greater than zero.");
    }

    [Fact]
    public void AddLamuFlixPersistence_RegistersScopedRepositoryScopedContextAndSingletonClock()
    {
        // arrange
        var services = RegisteredServices(Configuration(Valid()));

        // act
        var repository = services.Single(descriptor => descriptor.ServiceType == typeof(IMovieRepository));
        var context = services.Single(descriptor => descriptor.ServiceType == typeof(LamuFlixDbContext));
        var clock = services.Single(descriptor => descriptor.ServiceType == typeof(TimeProvider));

        // assert
        repository.ImplementationType.ShouldBe(typeof(EfMovieRepository));
        repository.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        context.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        clock.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        clock.ImplementationInstance.ShouldBeSameAs(TimeProvider.System);
    }

    [Fact]
    public void AddLamuFlixPersistence_PreRegisteredClock_SurvivesTheCall()
    {
        // arrange
        var clock = new FixedTimeProvider(FixedNow);
        var configuration = Configuration(Valid());
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(configuration);
        services.AddLamuFlixOptions();
        services.AddLogging();

        // act
        services.AddLamuFlixPersistence(configuration);
        using var provider = services.BuildServiceProvider(Validating());

        // assert
        provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(clock);
    }

    [Fact]
    public void AddLamuFlixPersistence_ValidConfiguration_BuildsAndPassesStartupValidation()
    {
        // arrange
        var services = Compose(Configuration(Valid()));

        // act
        using var provider = services.BuildServiceProvider(Validating());

        // assert
        var validators = provider.GetServices<IStartupValidator>().ToArray();
        validators.ShouldNotBeEmpty();
        foreach (var validator in validators)
        {
            validator.Validate();
        }

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMovieRepository>().ShouldBeOfType<EfMovieRepository>();
    }

    private static List<ServiceDescriptor> RegisteredServices(IConfiguration configuration) =>
        [.. Compose(configuration)];

    private static ServiceCollection Compose(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLamuFlixOptions();
        services.AddLogging();
        services.AddLamuFlixPersistence(configuration);
        return services;
    }

    private static IEnumerable<string> StartupValidationFailures(IConfiguration configuration)
    {
        using var provider = Compose(configuration).BuildServiceProvider();
        var exception = Should.Throw<OptionsValidationException>(() =>
        {
            foreach (var validator in provider.GetServices<IStartupValidator>())
            {
                validator.Validate();
            }
        });

        return exception.Failures;
    }

    private static ServiceProviderOptions Validating() =>
        new() { ValidateOnBuild = true, ValidateScopes = true };

    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static Dictionary<string, string?> WithoutConnectionString()
    {
        var values = Valid();
        values.Remove("ConnectionStrings:DefaultConnection");
        return values;
    }

    private static Dictionary<string, string?> WithConnectionString(string value)
    {
        var values = Valid();
        values["ConnectionStrings:DefaultConnection"] = value;
        return values;
    }

    private static Dictionary<string, string?> WithClaimLease(string? lease)
    {
        var values = Valid();
        if (lease is null)
        {
            values.Remove(ClaimLeaseKey);
        }
        else
        {
            values[ClaimLeaseKey] = lease;
        }

        return values;
    }

    private static Dictionary<string, string?> Valid() =>
        new()
        {
            ["ConnectionStrings:DefaultConnection"] = PlaceholderConnectionString,
            [$"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}"] = "C:/library",
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}"] = "not-a-real-key",
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = "https://example.invalid/",
            [$"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}"] = "localhost",
            [ClaimLeaseKey] = "00:05:00",
            ["Enrichment:SweepInterval"] = "00:01:00",
        };
}
