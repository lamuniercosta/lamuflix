using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

/// <summary>
/// Fails when test code can bind a socket on every network interface. Such a bind makes Windows Firewall
/// prompt the developer during a test run; this has regressed three times. WireMock stubs must start through
/// <see cref="LoopbackWireMock"/>, and raw listeners must use a loopback address.
/// </summary>
public sealed class LoopbackBindGuardTests
{
    private const string RootMarkerFileName = "Directory.Packages.props";
    private const string TestsFolderName = "tests";
    private const int RootWalkLimit = 8;

    private static readonly string[] WireMockHelperFileNames = ["LoopbackWireMock.cs", "SnapshotWireMock.cs"];
    private static readonly string[] BuildOutputFolderNames = ["bin", "obj"];

    private static readonly Regex DirectWireMockStart = new(
        @"WireMockServer\.Start\w*\s*\(",
        RegexOptions.CultureInvariant);

    private static readonly Regex WildcardSocketBind = new(
        @"IPAddress\.(Any|IPv6Any)\b|ListenAnyIP\s*\(|new\s+TcpListener\s*\(\s*\d+\s*\)",
        RegexOptions.CultureInvariant);

    [Fact]
    public void TestSources_StartWireMockOnlyThroughTheLoopbackHelpers()
    {
        // arrange
        var sources = TestSourceFiles()
            .Where(file => !WireMockHelperFileNames.Contains(Path.GetFileName(file), StringComparer.OrdinalIgnoreCase));

        // act
        var violations = FindMatches(sources, DirectWireMockStart);

        // assert
        violations.ShouldBeEmpty(
            "Start WireMock with LoopbackWireMock.Start() (or SnapshotWireMock.Start() in Evaluation): "
            + "a bare WireMockServer.Start() binds all interfaces and raises the Windows Firewall prompt.");
    }

    [Fact]
    public void TestSources_NeverBindARawListenerOnEveryInterface()
    {
        // arrange
        var sources = TestSourceFiles();

        // act
        var violations = FindMatches(sources, WildcardSocketBind);

        // assert
        violations.ShouldBeEmpty(
            "Bind IPAddress.Loopback instead: an all-interface listener raises the Windows Firewall prompt.");
    }

    private static IEnumerable<string> TestSourceFiles()
    {
        var self = $"{nameof(LoopbackBindGuardTests)}.cs";
        return Directory.EnumerateFiles(Path.Combine(FindRepositoryRoot(), TestsFolderName), "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsBuildOutput(file))
            .Where(file => !string.Equals(Path.GetFileName(file), self, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsBuildOutput(string file) =>
        file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => BuildOutputFolderNames.Contains(segment, StringComparer.OrdinalIgnoreCase));

    private static List<string> FindMatches(IEnumerable<string> files, Regex pattern) =>
        [.. files.SelectMany(file => File.ReadLines(file)
            .Select((text, index) => (Text: text, Line: index + 1))
            .Where(line => pattern.IsMatch(line.Text) && !IsComment(line.Text))
            .Select(line => $"{Path.GetFileName(file)}:{line.Line}: {line.Text.Trim()}"))];

    private static bool IsComment(string text) =>
        text.TrimStart().StartsWith("//", StringComparison.Ordinal);

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
