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
    private const string VolumesHeading = "    volumes:";
    private const string TmdbKeyPrefix = "Tmdb__";
    private const string EnvExampleFileName = ".env.example";
    private const string TmdbEnvPrefix = "TMDB_";
    private const string TmdbSectionName = "Tmdb";
    private const string OmdbSectionName = "Omdb";
    private const string SubtitlesSectionName = "Subtitles";
    private const string OpenSubtitlesSectionName = "OpenSubtitles";
    private const string ApiKeyPropertyName = "ApiKey";
    private const string BaseUrlPropertyName = "BaseUrl";
    private const string UserAgentPropertyName = "UserAgent";
    private const string DownloadAllowedHostsPropertyName = "DownloadAllowedHosts";
    private const string MaxDownloadBytesPropertyName = "MaxDownloadBytes";
    private const string RequestTimeoutPropertyName = "RequestTimeout";
    private const string EnabledPropertyName = "Enabled";
    private const string SaveEnabledPropertyName = "SaveEnabled";
    private const string PreferredLanguagesPropertyName = "PreferredLanguages";
    private const string ApiAppSettingsRelativePath = "src/LamuFlix.Api/appsettings.json";
    private const string ApiSettingsExampleRelativePath = "docker/api.settings.example.json";
    private const string PlaceholderApiKeyValue = "change";
    private const string SettingsSourceEntry = "source: ./docker/api.settings.json";
    private const string SettingsTargetEntry = "target: /app/appsettings.Production.json";
    private const string SettingsReadOnlyEntry = "read_only: true";
    private static readonly Regex ServiceBoundary = new(@"^\s{0,2}\S", RegexOptions.CultureInvariant);
    private static readonly Regex SectionBoundary = new(@"^\s{0,4}\S", RegexOptions.CultureInvariant);
    private static readonly Regex TrailingComment = new(@"\s+#.*$", RegexOptions.CultureInvariant);
    private static readonly string[] OpenSubtitlesProperties =
    [
        ApiKeyPropertyName,
        BaseUrlPropertyName,
        UserAgentPropertyName,
        DownloadAllowedHostsPropertyName,
        MaxDownloadBytesPropertyName,
        RequestTimeoutPropertyName,
    ];

    [Fact]
    public void ComposeApiService_Volumes_MountSettingsFileAtProductionPath()
    {
        // arrange
        var volumes = ReadApiServiceSection(VolumesHeading);

        // act
        var mountEntries = volumes
            .Where(line => line.StartsWith("source:", StringComparison.Ordinal)
                || line.StartsWith("target:", StringComparison.Ordinal)
                || line.StartsWith("read_only:", StringComparison.Ordinal))
            .ToList();

        // assert
        mountEntries.ShouldContain(SettingsSourceEntry);
        mountEntries.ShouldContain(SettingsTargetEntry);
        mountEntries.ShouldContain(SettingsReadOnlyEntry);
    }

    [Fact]
    public void ComposeApiService_Environment_DeclaresNoTmdbSettings()
    {
        // arrange
        var environment = ReadApiServiceSection(EnvironmentHeading);

        // act
        var tmdbEntries = environment.Where(entry => entry.StartsWith(TmdbKeyPrefix, StringComparison.Ordinal)).ToList();

        // assert
        tmdbEntries.ShouldBeEmpty();
    }

    [Fact]
    public void EnvExample_Content_DoesNotMentionTmdb()
    {
        // arrange
        var path = Path.Combine(FindRepositoryRoot(), EnvExampleFileName);

        // act
        var tmdbLines = File.ReadAllLines(path)
            .Where(line => line.Contains(TmdbEnvPrefix, StringComparison.OrdinalIgnoreCase)
                || line.Contains(TmdbSectionName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // assert
        tmdbLines.ShouldBeEmpty();
    }

    [Fact]
    public void ApiAppSettings_TmdbSection_StaysDisabledWithBlankKey()
    {
        // arrange
        var path = Path.Combine(FindRepositoryRoot(), ApiAppSettingsRelativePath);

        // act
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        document.RootElement.TryGetProperty(TmdbSectionName, out var tmdb).ShouldBeTrue();
        var enabled = tmdb.GetProperty(EnabledPropertyName).GetBoolean();
        var keyIsBlank = !tmdb.TryGetProperty(ApiKeyPropertyName, out var apiKey)
            || apiKey.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(apiKey.GetString());

        // assert
        enabled.ShouldBeFalse();
        keyIsBlank.ShouldBeTrue();
    }

    [Fact]
    public void ApiSettingsExample_OmdbSection_DocumentsPlaceholderKey()
    {
        // arrange
        var path = Path.Combine(FindRepositoryRoot(), ApiSettingsExampleRelativePath);

        // act
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var apiKey = document.RootElement.GetProperty(OmdbSectionName).GetProperty(ApiKeyPropertyName).GetString();

        // assert
        apiKey.ShouldBe(PlaceholderApiKeyValue);
    }

    [Fact]
    public void ApiSettingsExample_TmdbSection_EnablesWithBlankKey()
    {
        // arrange
        var path = Path.Combine(FindRepositoryRoot(), ApiSettingsExampleRelativePath);

        // act
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        document.RootElement.TryGetProperty(TmdbSectionName, out var tmdb).ShouldBeTrue();
        var enabled = tmdb.GetProperty(EnabledPropertyName).GetBoolean();
        var apiKey = tmdb.GetProperty(ApiKeyPropertyName).GetString();

        // assert
        enabled.ShouldBeTrue();
        apiKey.ShouldBe(string.Empty);
    }

    [Fact]
    public void ApiSettingsExample_SubtitlesSection_PreservesSiblingKeys()
    {
        // arrange
        var path = Path.Combine(FindRepositoryRoot(), ApiSettingsExampleRelativePath);

        // act
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var subtitles = document.RootElement.GetProperty(SubtitlesSectionName);
        var saveEnabled = subtitles.GetProperty(SaveEnabledPropertyName).GetBoolean();
        var preferredLanguages = subtitles.GetProperty(PreferredLanguagesPropertyName).GetArrayLength();

        // assert
        saveEnabled.ShouldBeFalse();
        preferredLanguages.ShouldBe(0);
    }

    [Fact]
    public void ApiSettingsExample_OpenSubtitlesSection_MatchesRatifiedContract()
    {
        // arrange
        var path = Path.Combine(FindRepositoryRoot(), ApiSettingsExampleRelativePath);

        // act
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        document.RootElement.TryGetProperty(SubtitlesSectionName, out var subtitles).ShouldBeTrue();
        subtitles.TryGetProperty(OpenSubtitlesSectionName, out var openSubtitles).ShouldBeTrue();
        var propertyNames = openSubtitles.EnumerateObject().Select(property => property.Name).ToArray();
        var allowedHosts = openSubtitles.GetProperty(DownloadAllowedHostsPropertyName).EnumerateArray().Select(host => host.GetString()).ToArray();

        // assert
        propertyNames.ShouldBe(OpenSubtitlesProperties);
        openSubtitles.GetProperty(ApiKeyPropertyName).GetString().ShouldBe(string.Empty);
        openSubtitles.GetProperty(BaseUrlPropertyName).GetString().ShouldBe("https://api.opensubtitles.com");
        openSubtitles.GetProperty(UserAgentPropertyName).GetString().ShouldBe("LamuFlix v1.0");
        allowedHosts.ShouldBe(["api.opensubtitles.com", "www.opensubtitles.com"]);
        openSubtitles.GetProperty(MaxDownloadBytesPropertyName).GetInt64().ShouldBe(1048576);
        openSubtitles.GetProperty(RequestTimeoutPropertyName).GetString().ShouldBe("00:00:15");
    }

    private static IReadOnlyList<string> ReadApiServiceSection(string heading)
    {
        var lines = File.ReadAllLines(Path.Combine(FindRepositoryRoot(), ComposeFileName));
        var serviceStart = Array.IndexOf(lines, ApiServiceHeading);
        serviceStart.ShouldBeGreaterThanOrEqualTo(0);

        var service = lines.Skip(serviceStart + 1).TakeWhile(line => !ServiceBoundary.IsMatch(line)).ToList();
        var sectionStart = service.IndexOf(heading);
        sectionStart.ShouldBeGreaterThanOrEqualTo(0);

        return
        [
            .. service
                .Skip(sectionStart + 1)
                .TakeWhile(line => !SectionBoundary.IsMatch(line))
                .Select(line => TrailingComment.Replace(line.Trim(), string.Empty)),
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
