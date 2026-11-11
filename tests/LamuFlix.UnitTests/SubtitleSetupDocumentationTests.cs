using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace LamuFlix.UnitTests;

public sealed class SubtitleSetupDocumentationTests
{
    private const string RootMarkerFileName = "Directory.Packages.props";
    private const int RootWalkLimit = 8;
    private const string ApiSettingsExampleRelativePath = "docker/api.settings.example.json";
    private const string ReadmeFileName = "README.md";
    private const string SubtitlesSectionName = "Subtitles";
    private const string SaveEnabledPropertyName = "SaveEnabled";
    private const string PreferredLanguagesPropertyName = "PreferredLanguages";
    private const string ProviderSectionName = "OpenSubtitles";

    private static readonly string[] DemoServingDocumentation =
    [
        "GET /api/movies/{id}/subtitles",
        "GET /api/movies/{id}/subtitles/{trackId}/content",
        "GET /api/movies/{id}/subtitles/sidecar",
        "POST /api/movies/{id}/subtitles/prepare",
        ":ro",
        "no provider traffic",
    ];

    private static readonly string[] WritableSettingsDocumentation =
    [
        "Subtitles:SaveEnabled",
        "Subtitles:PreferredLanguages",
        "Subtitles:OpenSubtitles:ApiKey",
        "Subtitles:OpenSubtitles:BaseUrl",
        "Subtitles:OpenSubtitles:UserAgent",
        "Subtitles:OpenSubtitles:DownloadAllowedHosts",
        "Subtitles:OpenSubtitles:MaxDownloadBytes",
        "Subtitles:OpenSubtitles:RequestTimeout",
        "Library:RootPath",
    ];

    [Fact]
    public void ApiSettingsExample_DemoDefaults_KeepSubtitleAcquisitionDisabledAndUnconfigured()
    {
        // arrange
        using var document = JsonDocument.Parse(ReadApiSettingsExample());
        var subtitles = document.RootElement.GetProperty(SubtitlesSectionName);

        // act
        var saveEnabled = subtitles.GetProperty(SaveEnabledPropertyName).GetBoolean();
        var preferredLanguages = subtitles.GetProperty(PreferredLanguagesPropertyName).GetArrayLength();

        // assert
        saveEnabled.ShouldBeFalse();
        preferredLanguages.ShouldBe(0);
        subtitles.TryGetProperty(ProviderSectionName, out _).ShouldBeFalse();
    }

    [Fact]
    public void ApiSettingsExample_Content_CarriesNoSubtitleProviderMaterial()
    {
        // arrange
        var content = ReadApiSettingsExample();

        // act & assert
        content.Contains(ProviderSectionName, StringComparison.Ordinal).ShouldBeFalse();
        content.Contains("DownloadAllowedHosts", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    public void Readme_DocumentsReadOnlyDemoServingWithoutAcquisition()
    {
        // arrange
        var readme = ReadReadme();

        // act & assert
        foreach (var entry in DemoServingDocumentation)
        {
            readme.Contains(entry, StringComparison.Ordinal).ShouldBeTrue($"README.md must document '{entry}'.");
        }
    }

    [Fact]
    public void Readme_DocumentsWritableDailyDriverSettings()
    {
        // arrange
        var readme = ReadReadme();

        // act & assert
        foreach (var entry in WritableSettingsDocumentation)
        {
            readme.Contains(entry, StringComparison.Ordinal).ShouldBeTrue($"README.md must document '{entry}'.");
        }

        readme.Contains("never commit", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public void Readme_CarriesNoCommittedSubtitleApiKeyValue()
    {
        // arrange
        var readme = ReadReadme();

        // act & assert
        readme.Contains("\"ApiKey\": \"changeme", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        readme.Contains("ApiKey=your", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    }

    private static string ReadApiSettingsExample() =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), ApiSettingsExampleRelativePath));

    private static string ReadReadme() =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), ReadmeFileName));

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
