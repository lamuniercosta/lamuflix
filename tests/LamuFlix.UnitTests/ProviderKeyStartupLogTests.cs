using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using LamuFlix.Api;
using LamuFlix.Core.Options;
using LamuFlix.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests;

public sealed class ProviderKeyStartupLogTests
{
    [Fact]
    public void Default_KeySet_IsTest()
    {
        // arrange
        var options = new ProviderKeyOptions();

        // act
        var keySet = options.KeySet;

        // assert
        keySet.ShouldBe(ProviderKeyOptions.TestKeySet);
    }

    [Fact]
    public void AddLamuFlixOptions_BindsKeySetFromProviderKeysSection()
    {
        // arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{ProviderKeyOptions.SectionName}:{nameof(ProviderKeyOptions.KeySet)}"] =
                    ProviderKeyOptions.ProdKeySet,
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLamuFlixOptions();

        // act
        using var provider = services.BuildServiceProvider();
        var keySet = provider.GetRequiredService<IOptions<ProviderKeyOptions>>().Value.KeySet;

        // assert
        keySet.ShouldBe(ProviderKeyOptions.ProdKeySet);
    }

    [Fact]
    public void ProviderKeyOptionsValidator_OmittedKeySet_KeepsTestDefault()
    {
        // arrange
        var validator = new ProviderKeyOptionsValidator();
        var options = new ProviderKeyOptions();

        // act
        var result = validator.Validate(null, options);

        // assert
        result.Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(ProviderKeyOptions.TestKeySet)]
    [InlineData(ProviderKeyOptions.ProdKeySet)]
    public void ProviderKeyOptionsValidator_ValidKeySet_Succeeds(string keySet)
    {
        // arrange
        var validator = new ProviderKeyOptionsValidator();
        var options = new ProviderKeyOptions { KeySet = keySet };

        // act
        var result = validator.Validate(null, options);

        // assert
        result.Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("staging")]
    public void ProviderKeyOptionsValidator_BlankOrUnknownKeySet_Fails(string keySet)
    {
        // arrange
        var validator = new ProviderKeyOptionsValidator();
        var options = new ProviderKeyOptions { KeySet = keySet };

        // act
        var result = validator.Validate(null, options);

        // assert
        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public void LogProviderKeySets_LogsOneLinePerServiceWithKeySetAndPresence()
    {
        // arrange
        var sink = new RecordingSink();

        // act
        ProviderKeyStartupLog.LogProviderKeySets(
            sink.CreateLogger("test"),
            ProviderKeyOptions.TestKeySet,
            omdbKeyPresent: true,
            tmdbKeyPresent: false,
            openSubtitlesKeyPresent: true,
            isDevelopment: false);

        // assert
        sink.Entries.ShouldBe(
            [
                new RecordedEntry(
                    LogLevel.Information,
                    "Provider key set test selected for OMDb; API key present."),
                new RecordedEntry(
                    LogLevel.Information,
                    "Provider key set test selected for TMDb; API key missing."),
                new RecordedEntry(
                    LogLevel.Information,
                    "Provider key set test selected for OpenSubtitles; API key present."),
            ]);
    }

    [Fact]
    public void LogProviderKeySets_LinesCarryNoKeyValuePrefixOrHash()
    {
        // arrange
        var sink = new RecordingSink();
        const string forbiddenValue = "unit-test-sentinel-value-4d2c";
        const string forbiddenPrefix = "unit-test-sentinel";
        const string forbiddenSuffix = "sentinel-value-4d2c";

        // act
        ProviderKeyStartupLog.LogProviderKeySets(
            sink.CreateLogger("test"),
            ProviderKeyOptions.TestKeySet,
            omdbKeyPresent: true,
            tmdbKeyPresent: true,
            openSubtitlesKeyPresent: false,
            isDevelopment: true);

        // assert
        foreach (var entry in sink.Entries)
        {
            entry.Message.Contains(forbiddenValue, StringComparison.Ordinal).ShouldBeFalse();
            entry.Message.Contains(forbiddenPrefix, StringComparison.Ordinal).ShouldBeFalse();
            entry.Message.Contains(forbiddenSuffix, StringComparison.Ordinal).ShouldBeFalse();
        }
    }

    [Fact]
    public void LogProviderKeySets_FromConfiguration_LogsProdSetAndPresenceWithoutKeyMaterial()
    {
        // arrange
        const string omdbKey = "omdb placeholder value alpha";
        const string openSubtitlesKey = "opensubtitles placeholder value beta";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Omdb:ApiKey"] = omdbKey,
                ["Subtitles:OpenSubtitles:ApiKey"] = openSubtitlesKey,
            })
            .Build();
        var sink = new RecordingSink();

        // act
        ProviderKeyStartupLog.LogProviderKeySets(
            sink.CreateLogger("test"),
            configuration,
            ProviderKeyOptions.ProdKeySet,
            isDevelopment: false);

        // assert
        sink.Entries.ShouldBe(
            [
                new RecordedEntry(
                    LogLevel.Information,
                    "Provider key set prod selected for OMDb; API key present."),
                new RecordedEntry(
                    LogLevel.Information,
                    "Provider key set prod selected for TMDb; API key missing."),
                new RecordedEntry(
                    LogLevel.Information,
                    "Provider key set prod selected for OpenSubtitles; API key present."),
            ]);
        var forbidden = new[]
        {
            omdbKey,
            openSubtitlesKey,
            "omdb placeholder",
            "opensubtitles placeholder",
            "value alpha",
            "value beta",
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(omdbKey))),
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(openSubtitlesKey))),
        };
        foreach (var entry in sink.Entries)
        {
            foreach (var material in forbidden)
            {
                entry.Message.Contains(material, StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
            }
        }
    }

    [Fact]
    public void LogProviderKeySets_DevelopmentWithProdKeySet_LogsWarning()
    {
        // arrange
        var sink = new RecordingSink();

        // act
        ProviderKeyStartupLog.LogProviderKeySets(
            sink.CreateLogger("test"),
            ProviderKeyOptions.ProdKeySet,
            omdbKeyPresent: true,
            tmdbKeyPresent: true,
            openSubtitlesKeyPresent: true,
            isDevelopment: true);

        // assert
        sink.Entries.Count.ShouldBe(4);
        var warning = sink.Entries[^1];
        warning.Level.ShouldBe(LogLevel.Warning);
        warning.Message.Contains(ProviderKeyOptions.ProdKeySet, StringComparison.Ordinal).ShouldBeTrue();
    }

    [Theory]
    [InlineData(ProviderKeyOptions.TestKeySet, true)]
    [InlineData(ProviderKeyOptions.ProdKeySet, false)]
    [InlineData(ProviderKeyOptions.TestKeySet, false)]
    public void LogProviderKeySets_WithoutDevProdCombination_LogsNoWarning(string keySet, bool isDevelopment)
    {
        // arrange
        var sink = new RecordingSink();

        // act
        ProviderKeyStartupLog.LogProviderKeySets(
            sink.CreateLogger("test"),
            keySet,
            omdbKeyPresent: true,
            tmdbKeyPresent: true,
            openSubtitlesKeyPresent: true,
            isDevelopment);

        // assert
        sink.Entries.Count.ShouldBe(3);
        sink.Entries.ShouldAllBe(entry => entry.Level == LogLevel.Information);
    }

    private sealed class RecordingSink : ILoggerProvider
    {
        public List<RecordedEntry> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(Entries);

        public void Dispose()
        {
        }

        private sealed class RecordingLogger(List<RecordedEntry> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                ArgumentNullException.ThrowIfNull(formatter);
                entries.Add(new RecordedEntry(logLevel, formatter(state, exception)));
            }
        }
    }

    private sealed record RecordedEntry(LogLevel Level, string Message);
}
