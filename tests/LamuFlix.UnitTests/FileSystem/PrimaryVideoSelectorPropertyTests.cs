using System;
using System.IO.Abstractions;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Infrastructure.FileSystem;
using NSubstitute;

namespace LamuFlix.UnitTests.FileSystem;

public sealed class PrimaryVideoSelectorPropertyTests
{
    private static readonly string[] Names =
    [
        "Alpha.mkv",
        "beta.MP4",
        "Gamma.avi",
        "delta m4v.rmvb",
        "Epsilon.rm",
        "zeta.flv",
        "Eta.SRT",
        "theta-sample.mkv",
        "iota.trailer.mp4",
    ];

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Select_AnyListingPermutation_ReturnsTheSameCandidate(int seed)
    {
        // arrange
        var candidates = Candidates(seed);
        var expected = PrimaryVideoSelector.Select(Directory(candidates));
        var permuted = Permute(candidates, (uint)seed);
        var reordered = PrimaryVideoSelector.Select(Directory(permuted));

        // act
        var same = ReferenceEquals(expected, reordered)
            || (expected is not null && reordered is not null && string.Equals(expected.Name, reordered.Name, StringComparison.Ordinal));

        // assert
        return same;
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Select_AnyListingPermutation_NeverReturnsAnExcludedOrUnsupportedCandidate(int seed)
    {
        // arrange
        var selected = PrimaryVideoSelector.Select(Directory(Permute(Candidates(seed), (uint)seed)));

        // act
        var accepted = selected is null
            || (SupportedVideoExtensions.Contains(selected.Extension)
                && !selected.Name.Contains("sample", StringComparison.OrdinalIgnoreCase)
                && !selected.Name.Contains("trailer", StringComparison.OrdinalIgnoreCase));

        // assert
        return accepted;
    }

    private static IFileInfo[] Candidates(int seed)
    {
        var magnitude = Math.Abs((long)seed);
        var count = (int)(magnitude % Names.Length) + 1;
        return [.. Enumerable.Range(0, count).Select(index =>
        {
            var length = (magnitude >> (index * 3)) % 5;
            return FileInfo(Names[(int)((magnitude + index) % Names.Length)], length);
        })];
    }

    private static IFileInfo[] Permute(IFileInfo[] candidates, uint seed)
    {
        var shuffled = candidates.ToArray();
        for (var index = shuffled.Length - 1; index > 0; index--)
        {
            var swap = (int)((seed + (uint)index * 2654435761u) % (uint)(index + 1));
            (shuffled[index], shuffled[swap]) = (shuffled[swap], shuffled[index]);
        }

        return shuffled;
    }

    private static IDirectoryInfo Directory(IFileInfo[] files)
    {
        var directory = Substitute.For<IDirectoryInfo>();
        directory.EnumerateFiles().Returns(files);
        return directory;
    }

    private static IFileInfo FileInfo(string name, long length)
    {
        var info = Substitute.For<IFileInfo>();
        info.Name.Returns(name);
        info.Extension.Returns(System.IO.Path.GetExtension(name));
        info.Length.Returns(length);
        return info;
    }
}
