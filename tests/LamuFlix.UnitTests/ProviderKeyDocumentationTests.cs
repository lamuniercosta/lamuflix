using System;
using System.IO;
using System.Text.Json;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests;

public sealed class ProviderKeyDocumentationTests
{
    private const string RootMarkerFileName = "Directory.Packages.props";
    private const int RootWalkLimit = 8;
    private const string ApiSettingsExampleRelativePath = "docker/api.settings.example.json";
    private const string ReadmeFileName = "README.md";

    private static readonly string[] KeySetDocumentation =
    [
        "ProviderKeys:KeySet",
        "ProviderKeys__KeySet",
        "KeySet: test",
        "KeySet: prod",
        "OMDb",
        "TMDb",
        "OpenSubtitles",
    ];

    [Fact]
    public void ApiSettingsExample_ProviderKeysSection_DefaultsToTestKeySet()
    {
        // arrange
        using var document = JsonDocument.Parse(ReadApiSettingsExample());

        // act
        var keySet = document.RootElement
            .GetProperty(ProviderKeyOptions.SectionName)
            .GetProperty(nameof(ProviderKeyOptions.KeySet))
            .GetString();

        // assert
        keySet.ShouldBe(ProviderKeyOptions.TestKeySet);
    }

    [Fact]
    public void Readme_DocumentsBothKeySetsForAllThreeServices()
    {
        // arrange
        var readme = ReadReadme();

        // act & assert
        foreach (var entry in KeySetDocumentation)
        {
            readme.Contains(entry, StringComparison.Ordinal).ShouldBeTrue($"README.md must document '{entry}'.");
        }
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
