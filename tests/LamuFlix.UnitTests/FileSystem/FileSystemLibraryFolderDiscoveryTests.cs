using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.FileSystem;
using NSubstitute;

namespace LamuFlix.UnitTests.FileSystem;

public sealed class FileSystemLibraryFolderDiscoveryTests
{
    private const string Root = "/library";
    private const string RootIdentity = ".";

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
            RootIdentity,
            Relative("Parent"),
            Relative("Parent", "Child"),
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
        candidates.Select(path => path.Value).ShouldBe([Relative("Real")]);
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
        var discovery = Discovery(fileSystem, $"{Root}/rootlink");

        // act
        var candidates = await CollectAsync(discovery);

        // assert
        candidates.Select(path => path.Value)
            .ShouldBe([RootIdentity, Relative("MovieA")], ignoreOrder: true);
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
        candidate.ShouldBe(Relative("Movies", "Heat (1995)"));
        candidate.ShouldNotStartWith(fileSystem.Path.DirectorySeparatorChar.ToString());
        candidate.ShouldNotContain("..", Case.Sensitive);
        new LibraryPath(candidate).Value.ShouldBe(candidate);
    }

    [Fact]
    public async Task DiscoverAsync_RootCandidate_YieldsCanonicalRootIdentity()
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
            .ShouldBe([RootIdentity, Relative("Nested")], ignoreOrder: true);
    }

    [Fact]
    public async Task DiscoverAsync_EveryCandidate_IsAlreadyCanonicalLibraryPath()
    {
        // arrange
        var fileSystem = FileSystemWith(
            ($"{Root}/root.mkv", "root"),
            ($"{Root}/A B/C D/movie.mkv", "nested"));
        var discovery = Discovery(fileSystem, Root);

        // act
        var candidates = await CollectAsync(discovery);

        // assert
        candidates.ShouldAllBe(candidate => candidate.Value == new LibraryPath(candidate.Value).Value);
        candidates.ShouldAllBe(candidate => candidate.Value.StartsWith('/') == false);
    }

    [Fact]
    public async Task DiscoverAsync_ProcessCurrentDirectoryDiffersFromRoot_StillDiscoversUnderActiveRoot()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Heat (1995)/Heat.mkv", "heat"));
        fileSystem.Directory.SetCurrentDirectory("/tmp/elsewhere");
        var discovery = Discovery(fileSystem, Root);

        // act
        var candidates = await CollectAsync(discovery);

        // assert
        candidates.ShouldHaveSingleItem().Value.ShouldBe(Relative("Heat (1995)"));
    }

    [Theory]
    [InlineData("library")]
    [InlineData("")]
    [InlineData("  ")]
    public async Task DiscoverAsync_NonAbsoluteConfiguredRoot_FailsClosedInsteadOfUsingProcessCurrentDirectory(string root)
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Movie/movie.mkv", "video"));
        var discovery = Discovery(fileSystem, root);

        // act
        var failure = await Record.ExceptionAsync(() => CollectAsync(discovery));

        // assert
        failure.ShouldNotBeNull();
        failure.Message.ShouldContain(LibraryOptions.SectionName);
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
        candidates.ShouldHaveSingleItem().Value.ShouldBe(Relative("Movie"));
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
        var fileSystem = NestedChainFileSystem(depth);
        var expected = new List<string>(depth);
        var chainRoot = Root;
        for (var level = 1; level <= depth; level++)
        {
            chainRoot = $"{chainRoot}/level{level:0000}";
            expected.Add(chainRoot[(Root.Length + 1)..]);
        }

        var discovery = Discovery(fileSystem, Root);

        // act
        var candidates = await CollectAsync(discovery);

        // assert
        candidates.Count.ShouldBe(depth);
        candidates.Select(path => path.Value).ShouldBe(expected, ignoreOrder: true);
    }

    private static FileSystemLibraryFolderDiscovery Discovery(IFileSystem fileSystem, string root) =>
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

    private static string Relative(params string[] segments) =>
        string.Join('/', segments);

    // NSubstitute tree, not MockFileSystem: MockFileSystem takes ~95 s on a 1,000-level chain; the proof is the same nested structure and exact result.
    private static IFileSystem NestedChainFileSystem(int depth)
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Path.GetRelativePath(Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => Path.GetRelativePath(call.ArgAt<string>(0), call.ArgAt<string>(1)));

        var fullNames = new string[depth + 1];
        fullNames[0] = Root;
        for (var level = 1; level <= depth; level++)
        {
            fullNames[level] = $"{fullNames[level - 1]}/level{level.ToString("0000", CultureInfo.InvariantCulture)}";
        }

        var directory = NestedDirectory(fullNames[depth], hasVideo: true, child: null);
        for (var level = depth - 1; level >= 0; level--)
        {
            directory = NestedDirectory(fullNames[level], hasVideo: level > 0, directory);
        }

        fileSystem.DirectoryInfo.New(Root).Returns(directory);
        return fileSystem;
    }

    private static IDirectoryInfo NestedDirectory(string fullName, bool hasVideo, IDirectoryInfo? child)
    {
        var directory = Substitute.For<IDirectoryInfo>();
        directory.Exists.Returns(true);
        directory.FullName.Returns(fullName);
        directory.LinkTarget.Returns((string?)null);
        directory.Attributes.Returns(FileAttributes.Directory);

        var files = new List<IFileInfo>();
        if (hasVideo)
        {
            var video = Substitute.For<IFileInfo>();
            video.Extension.Returns(".mkv");
            files.Add(video);
        }

        var entries = new List<IFileSystemInfo>(files);
        if (child is not null)
        {
            entries.Add(child);
        }

        directory.EnumerateFiles().Returns(files);
        directory.EnumerateFileSystemInfos().Returns(entries);
        return directory;
    }

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
