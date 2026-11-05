using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LamuFlix.UnitTests;

public sealed class TmdbComposeTests
{
    private const string RootMarkerFileName = "Directory.Packages.props";
    private const int RootWalkLimit = 8;
    private const string ComposeFileName = "docker-compose.yml";
    private const string ApiServiceHeading = "  api:";
    private const string EnvironmentHeading = "    environment:";
    private const string TmdbEnabledEntry = "Tmdb__Enabled: ${TMDB_ENABLED:-true}";
    private const string TmdbApiKeyEntry = "Tmdb__ApiKey: ${TMDB_API_KEY:-}";
    private const string TmdbKeyPrefix = "Tmdb__";
    private const string EnvExampleFileName = ".env.example";
    private const string EnvEnabledEntry = "TMDB_ENABLED=true";
    private const string EnvApiKeyEntry = "TMDB_API_KEY=";
    private const string TmdbSectionName = "Tmdb";
    private const string TmdbEnabledPropertyName = "Enabled";
    private const string ApiAppSettingsRelativePath = @"src\LamuFlix.Api\appsettings.json";
    private const string ApiSettingsExampleRelativePath = @"docker\api.settings.example.json";
    private const string TmdbNameFragment = "tmdb";
    private const string TmdbKeyVariable = "TMDB_API_KEY";
    private static readonly Regex ServiceBoundary = new(@"^\s{0,2}\S", RegexOptions.CultureInvariant);
    private static readonly Regex EnvironmentBoundary = new(@"^\s{0,4}\S", RegexOptions.CultureInvariant);
    private static readonly Regex NonEmptyTmdbKey = new(@"^TMDB_API_KEY=.+$", RegexOptions.CultureInvariant);
    private static readonly Regex TrailingComment = new(@"\s+#.*$", RegexOptions.CultureInvariant);

    [Fact]
    public void ComposeApiService_Environment_EnablesTmdbByDefaultFromEnv()
    {
        // arrange
        var environment = ReadApiServiceEnvironment();

        // act & assert
        environment.ShouldContain(TmdbEnabledEntry);
    }

    [Fact]
    public void ComposeApiService_Environment_ReadsTmdbApiKeyFromEnvWithEmptyDefault()
    {
        // arrange
        var environment = ReadApiServiceEnvironment();

        // act & assert
        environment.ShouldContain(TmdbApiKeyEntry);
    }

    [Fact]
    public void ComposeApiService_Environment_DeclaresNoOtherTmdbSetting()
    {
        // arrange
        var environment = ReadApiServiceEnvironment();

        // act
        var tmdbEntries = environment.Where(entry => entry.StartsWith(TmdbKeyPrefix, StringComparison.Ordinal)).ToList();

        // assert
        tmdbEntries.ShouldBe([TmdbEnabledEntry, TmdbApiKeyEntry]);
    }

    [Fact]
    public void EnvExample_ActiveEntries_DocumentTmdbEnablementAndEmptyKey()
    {
        // arrange
        var entries = ReadActiveEnvExampleEntries();

        // act & assert
        entries.ShouldContain(EnvEnabledEntry);
        entries.ShouldContain(EnvApiKeyEntry);
    }

    [Fact]
    public void EnvExample_ApiKeyPlaceholder_StaysEmpty()
    {
        // arrange
        var entries = ReadActiveEnvExampleEntries();

        // act
        var committedKeys = entries.Where(entry => NonEmptyTmdbKey.IsMatch(entry)).ToList();

        // assert
        committedKeys.ShouldBeEmpty();
    }

    [Fact]
    public void ApiAppSettings_TmdbSection_StaysDisabled()
    {
        // arrange
        var path = Path.Combine(FindRepositoryRoot(), ApiAppSettingsRelativePath);

        // act
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var enabled = document.RootElement.GetProperty(TmdbSectionName).GetProperty(TmdbEnabledPropertyName).GetBoolean();

        // assert
        enabled.ShouldBeFalse();
    }

    [Fact]
    public void ApiSettingsExample_Content_CarriesNoTmdbMaterial()
    {
        // arrange
        var path = Path.Combine(FindRepositoryRoot(), ApiSettingsExampleRelativePath);

        // act
        var content = File.ReadAllText(path);

        // assert
        content.Contains(TmdbNameFragment, StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        content.Contains(TmdbKeyVariable, StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    }

    private static IReadOnlyList<string> ReadApiServiceEnvironment()
    {
        var lines = File.ReadAllLines(Path.Combine(FindRepositoryRoot(), ComposeFileName));
        var serviceStart = Array.IndexOf(lines, ApiServiceHeading);
        serviceStart.ShouldBeGreaterThanOrEqualTo(0);

        var service = lines.Skip(serviceStart + 1).TakeWhile(line => !ServiceBoundary.IsMatch(line)).ToList();
        var environmentStart = service.IndexOf(EnvironmentHeading);
        environmentStart.ShouldBeGreaterThanOrEqualTo(0);

        return
        [
            .. service
                .Skip(environmentStart + 1)
                .TakeWhile(line => !EnvironmentBoundary.IsMatch(line))
                .Select(line => TrailingComment.Replace(line.Trim(), string.Empty)),
        ];
    }

    private static IReadOnlyList<string> ReadActiveEnvExampleEntries()
    {
        var path = Path.Combine(FindRepositoryRoot(), EnvExampleFileName);
        return
        [
            .. File.ReadAllLines(path)
                .Select(line => line.Trim())
                .Where(line => !line.StartsWith('#'))
                .Where(line => line.Length > 0),
        ];
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; current is not null && depth < RootWalkLimit; depth++, current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, RootMarkerFileName)))
            {
                return current.FullName;
            }
        }

        throw new DirectoryNotFoundException($"No {RootMarkerFileName} found above {AppContext.BaseDirectory}.");
    }
}
