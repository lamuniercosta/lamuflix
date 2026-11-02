using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.FileSystem;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.FileSystem;

public sealed class FileSystemLibraryFolderDiscoveryTests
{
    private const string Root = "/library";

    [Fact]
    public async Task DiscoverAsync_RootNestedParentNestedChildContainerEmpty_YieldsExactCandidateSetOnceEach()
    {
        // arrange
        var fileSystem = FileSystemWith(
            ($"{Root}/root.mkv", "root"),
            ($"{Root}/Parent/parent.mkv", "parent"),
            ($"{Root}/Parent/Child/child.mkv", "child"),
            ($"{Root}/Container/notes.txt", "notes"));
        fileSystem.AddDirectory($"{Root}/Empty");
        var discovery = Discovery(fileSystem, Root);
        var expected = new HashSet<string>(StringComparer.Ordinal)
        {
            fileSystem.DirectoryInfo.New(Root).FullName,
            Relative(fileSystem, "Parent"),
            Relative(fileSystem, "Parent", "Child"),
        };

        // act
        var candidates = await CollectAsync(discovery);

        // assert
        candidates.Count.ShouldBe(expected.Count);
        candidates.Select(path => path.Value).ShouldBe(expected, ignoreOrder: true);
    }

    [Fact]
    public async Task DiscoverAsync_LinkedChildViaReparsePointOrLinkTarget_NeitherYieldedNorDescended()
    {
        // arrange
        var fileSystem = FileSystemWith(
            ($"{Root}/Real/movie.mkv", "movie"),
            ($"/outside/outside.mkv", "outside"));
        fileSystem.Directory.CreateSymbolicLink($"{Root}/Linked", $"{Root}/Real");
        fileSystem.Directory.CreateSymbolicLink($"{Root}/Real/Clip", "/outside");
        var discovery = Discovery(fileSystem, Root);

        // act
        var candidates = await CollectAsync(discovery);

        // assert
        candidates.Select(path => path.Value).ShouldBe([Relative(fileSystem, "Real")]);
        candidates.ShouldNotContain(path => path.Value.Contains("Linked", StringComparison.Ordinal));
        candidates.ShouldNotContain(path => path.Value.Contains("Clip", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DiscoverAsync_ConfiguredRootReachedThroughLink_ResolvesFinalTargetAndBindsContainmentToResolvedRootOnly()
    {
        // arrange
        var fileSystem = FileSystemWith(
            ($"{Root}/real/root.mkv", "root"),
            ($"{Root}/real/MovieA/a.mkv", "a"));
        fileSystem.Directory.CreateSymbolicLink($"{Root}/rootlink", $"{Root}/real");
        var resolvedRoot = fileSystem.DirectoryInfo.New($"{Root}/real").FullName;
        var discovery = Discovery(fileSystem, $"{Root}/rootlink");

        // act
        var candidates = await CollectAsync(discovery);

        // assert
        candidates.Select(path => path.Value)
            .ShouldBe([resolvedRoot, Relative(fileSystem, "MovieA")], ignoreOrder: true);
        candidates.ShouldNotContain(path => path.Value.Contains("rootlink", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DiscoverAsync_DescendantCandidate_YieldsCanonicalRootRelativePath()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Movies/Heat (1995)/Heat.mkv", "heat"));
        var discovery = Discovery(fileSystem, Root);

        // act
        var candidates = await CollectAsync(discovery);

        // assert
        var candidate = candidates.ShouldHaveSingleItem().Value;
        candidate.ShouldBe(Relative(fileSystem, "Movies", "Heat (1995)"));
        candidate.ShouldNotStartWith(fileSystem.Path.DirectorySeparatorChar.ToString());
        candidate.ShouldNotContain("..", Case.Sensitive);
    }

    [Fact]
    public async Task DiscoverAsync_RootCandidate_YieldsCanonicalAbsolutePath()
    {
        // arrange
        var fileSystem = FileSystemWith(
            ($"{Root}/root.mkv", "root"),
            ($"{Root}/Nested/movie.mp4", "nested"));
        var discovery = Discovery(fileSystem, Root);

        // act
        var candidates = await CollectAsync(discovery);

        // assert
        candidates.Select(path => path.Value)
            .ShouldBe([fileSystem.DirectoryInfo.New(Root).FullName, Relative(fileSystem, "Nested")], ignoreOrder: true);
    }

    [Fact]
    public void IsContained_CaseDistinctSibling_RejectedUnderOrdinal_AdmittedSpellingUnderOrdinalIgnoreCase()
    {
        // arrange
        const string root = "/library/Heat";

        // act
        var ordinal = FileSystemLibraryFolderDiscovery.IsContained(
            root, "/Library/Heat/Extra/movie.mkv", StringComparison.Ordinal);
        var ordinalIgnoreCase = FileSystemLibraryFolderDiscovery.IsContained(
            root, "/Library/Heat/Extra/movie.mkv", StringComparison.OrdinalIgnoreCase);
        var siblingIgnoreCase = FileSystemLibraryFolderDiscovery.IsContained(
            root, "/library/Heat2/movie.mkv", StringComparison.OrdinalIgnoreCase);

        // assert
        ordinal.ShouldBeFalse();
        ordinalIgnoreCase.ShouldBeTrue();
        siblingIgnoreCase.ShouldBeFalse();
    }

    public static TheoryData<string, string, bool> FilesystemRootForms => new()
    {
        { "/", "/library", true },
        { "/", "/", true },
        { "/", "/library/Movie", true },
        { "/library/", "/library/Movie", true },
        { "/library", "/library2", false },
        { "C:\\", "C:\\library", true },
        { "C:\\", "C:\\", true },
        { "C:\\", "D:\\library", false },
        { "C:\\library", "C:\\library2", false },
    };

    [Theory]
    [MemberData(nameof(FilesystemRootForms))]
    public void IsContained_FilesystemRootForms_PreservedForSlashAndDriveRoot(
        string root,
        string candidate,
        bool expected)
    {
        // act
        var contained = FileSystemLibraryFolderDiscovery.IsContained(root, candidate, StringComparison.Ordinal);

        // assert
        contained.ShouldBe(expected);
    }

    [Fact]
    public void DiscoverAsync_HostSelectedComparison_WindowsOrdinalIgnoreCase_ElseOrdinal()
    {
        // arrange
        var expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        // act
        var comparison = FileSystemLibraryFolderDiscovery.HostComparison();

        // assert
        comparison.ShouldBe(expected);
    }

    public static TheoryData<string> SupportedExtensions()
    {
        var data = new TheoryData<string>();
        foreach (var extension in SupportedVideoExtensions.All)
        {
            data.Add(extension);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(SupportedExtensions))]
    public async Task DiscoverAsync_ExtensionParity_MatchesSupportedVideoExtensions(string extension)
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Movie/movie{extension}", "video"));
        var discovery = Discovery(fileSystem, Root);

        // act
        var candidates = await CollectAsync(discovery);

        // assert
        candidates.ShouldHaveSingleItem().Value.ShouldBe(Relative(fileSystem, "Movie"));
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".jpg")]
    [InlineData("")]
    public async Task DiscoverAsync_UnsupportedDirectTopLevelFile_YieldsNoCandidate(string extension)
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Movie/movie{extension}", "not a video"));
        var discovery = Discovery(fileSystem, Root);

        // act
        var candidates = await CollectAsync(discovery);

        // assert
        candidates.ShouldBeEmpty();
    }

    [Fact]
    public async Task DiscoverAsync_Depth1000NestedCandidateChain_YieldsExactCandidateSetOnceEachAndCompletes()
    {
        // arrange
        const int depth = 1000;
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData>(StringComparer.OrdinalIgnoreCase),
            new MockFileSystemOptions());
        var expected = new List<string>(depth);
        var chainRoot = Root;
        for (var level = 1; level <= depth; level++)
        {
            chainRoot = $"{chainRoot}/level{level:0000}";
            fileSystem.AddFile($"{chainRoot}/movie.mkv", new MockFileData("video"));
            expected.Add(chainRoot[(Root.Length + 1)..].Replace('/', fileSystem.Path.DirectorySeparatorChar));
        }

        var discovery = Discovery(fileSystem, Root);

        // act
        var candidates = await CollectAsync(discovery);

        // assert
        candidates.Count.ShouldBe(depth);
        candidates.Select(path => path.Value).ShouldBe(expected, ignoreOrder: true);
    }

    private static FileSystemLibraryFolderDiscovery Discovery(MockFileSystem fileSystem, string root) =>
        new(fileSystem, Microsoft.Extensions.Options.Options.Create(new LibraryOptions { RootPath = root }));

    private static async Task<List<LibraryPath>> CollectAsync(ILibraryFolderDiscovery discovery)
    {
        var collected = new List<LibraryPath>();
        await foreach (var candidate in discovery.DiscoverAsync(TestContext.Current.CancellationToken))
        {
            collected.Add(candidate);
        }

        return collected;
    }

    private static string Relative(MockFileSystem fileSystem, params string[] segments) =>
        fileSystem.Path.Combine(segments);

    private static MockFileSystem FileSystemWith(params (string Path, string Contents)[] files)
    {
        var entries = new Dictionary<string, MockFileData>(files.Length, StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            entries.Add(file.Path, new MockFileData(file.Contents));
        }

        return new MockFileSystem(entries, new MockFileSystemOptions());
    }
}
