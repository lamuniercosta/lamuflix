using System.Collections.Generic;
using LamuFlix.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests;

public sealed class DatabaseOptionsTests
{
    [Fact]
    public void Default_ApplyMigrationsOnStartup_IsFalse()
    {
        // arrange
        var options = new DatabaseOptions();

        // act
        var applyMigrations = options.ApplyMigrationsOnStartup;

        // assert
        applyMigrations.ShouldBeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ApplyMigrationsOnStartup_BindsFromDatabaseSection(bool expected)
    {
        // arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{DatabaseOptions.SectionName}:{nameof(DatabaseOptions.ApplyMigrationsOnStartup)}"] =
                    expected.ToString(),
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // act
        using var provider = services.BuildServiceProvider();
        var bound = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value.ApplyMigrationsOnStartup;

        // assert
        bound.ShouldBe(expected);
    }
}
