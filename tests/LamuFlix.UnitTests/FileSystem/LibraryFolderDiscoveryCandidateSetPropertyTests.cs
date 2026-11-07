using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.FileSystem;

namespace LamuFlix.UnitTests.FileSystem;

public sealed class LibraryFolderDiscoveryCandidateSetPropertyTests
{
    private const string Root = "/library";
    private const int MaxDepth = 3;
    private const int MaxChildren = 3;

    [Property(MaxTest = 50)]
    [Trait("Category", "Property")]
    public bool DiscoverAsync_GeneratedTree_YieldsExactCandidateSetOnceEach(int seed)
    {
        // arrange
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData>(StringComparer.OrdinalIgnoreCase),
            new MockFileSystemOptions());
        var rootInfo = fileSystem.DirectoryInfo.New(Root);
        fileSystem.AddDirectory(rootInfo.FullName);
        var expected = new List<string>();
        AddNode(fileSystem, rootInfo.FullName, (uint)seed, 0, expected);
        var discovery = new FileSystemLibraryFolderDiscovery(
            fileSystem,
            Microsoft.Extensions.Options.Options.Create(new LibraryOptions { RootPath = Root }));

        // act
        var actual = Collect(discovery)
            .Select(path => path.Value)
            .ToList();

        // assert
        var expectedValues = expected
            .Select(path => string.Equals(path, rootInfo.FullName, StringComparison.Ordinal)
                ? "."
                : fileSystem.Path.GetRelativePath(rootInfo.FullName, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
        var actualValues = actual.OrderBy(path => path, StringComparer.Ordinal).ToList();

        return actualValues.SequenceEqual(expectedValues, StringComparer.Ordinal)
            && actual.Count == actual.Distinct(StringComparer.Ordinal).Count();
    }

    private static void AddNode(
        MockFileSystem fileSystem,
        string directory,
        uint seed,
        int depth,
        List<string> expected)
    {
        fileSystem.AddDirectory(directory);
        if ((seed & 1) == 1)
        {
            fileSystem.AddFile(
                fileSystem.Path.Combine(directory, "movie.mkv"),
                new MockFileData("video"));
            expected.Add(directory);
        }

        var children = depth >= MaxDepth ? 0 : (int)((seed >> 1) % (MaxChildren + 1));
        for (var child = 0; child < children; child++)
        {
            var next = directory.Length == 0
                ? $"child{child}"
                : $"{directory}/child{child}";
            AddNode(fileSystem, next, seed >> 3, depth + 1, expected);
        }
    }

    private static List<LibraryPath> Collect(ILibraryFolderDiscovery discovery)
    {
        var collected = new List<LibraryPath>();
        var enumerator = discovery.DiscoverAsync(TestContext.Current.CancellationToken).GetAsyncEnumerator();
        try
        {
            while (enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult())
            {
                collected.Add(enumerator.Current);
            }
        }
        finally
        {
            enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        return collected;
    }
}
