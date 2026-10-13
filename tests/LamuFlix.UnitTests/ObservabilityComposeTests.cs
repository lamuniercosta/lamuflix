using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LamuFlix.UnitTests;

public sealed class ObservabilityComposeTests
{
    private const string RootMarkerFileName = "Directory.Packages.props";
    private const int RootWalkLimit = 8;
    private const string ComposeFileName = "docker-compose.observability.yml";
    private const string DashboardService = "aspire-dashboard:";
    private const string DashboardImage = "image: mcr.microsoft.com/dotnet/aspire-dashboard:13.6";
    private const string UnsecuredFrontend = "- DASHBOARD__FRONTEND__AUTHMODE=Unsecured";
    private static readonly string[] LoopbackPorts =
    [
        "127.0.0.1:18888:18888",
        "127.0.0.1:4317:18889",
        "127.0.0.1:4318:18890",
    ];
    private static readonly Regex PortMapping = new(
        @"^-\s*""?((?:\d{1,3}\.){3}\d{1,3}:\d+:\d+)""?$",
        RegexOptions.CultureInvariant);
    private static readonly Regex TrailingComment = new(@"\s+#.*$", RegexOptions.CultureInvariant);

    [Fact]
    public void ComposeFile_DeclaresAspireDashboardService()
    {
        // arrange
        var lines = ReadActiveComposeLines();

        // act & assert
        lines.ShouldContain(DashboardService);
    }

    [Fact]
    public void ComposeFile_PinsDashboardImageToVersion13_6()
    {
        // arrange
        var lines = ReadActiveComposeLines();

        // act & assert
        lines.ShouldContain(DashboardImage);
    }

    [Fact]
    public void ComposeFile_PublishesDashboardAndOtlpPortsOnLoopbackOnly()
    {
        // arrange
        var lines = ReadActiveComposeLines();

        // act
        var published = lines
            .Select(line => PortMapping.Match(line))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .ToList();

        // assert
        published.ShouldBe(LoopbackPorts);
    }

    [Fact]
    public void ComposeFile_RunsDashboardUnsecuredForLocalUseOnly()
    {
        // arrange
        var lines = ReadActiveComposeLines();

        // act & assert
        lines.ShouldContain(UnsecuredFrontend);
    }

    // Drops comment-only lines and trailing comments so only live YAML tokens are matched.
    private static IReadOnlyList<string> ReadActiveComposeLines()
    {
        var root = FindRepositoryRoot();
        return
        [
            .. File.ReadAllLines(Path.Combine(root, ComposeFileName))
                .Select(line => line.Trim())
                .Where(line => !line.StartsWith('#'))
                .Select(line => TrailingComment.Replace(line, string.Empty))
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
