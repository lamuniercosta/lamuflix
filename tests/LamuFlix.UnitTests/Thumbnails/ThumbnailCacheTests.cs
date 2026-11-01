using System;
using System.IO;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Thumbnails;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace LamuFlix.UnitTests.Thumbnails;

public sealed class ThumbnailCacheTests
{
    private const byte SoiFirstByte = 0xFF;
    private const byte SoiSecondByte = 0xD8;
    private const int ContentOffset = 2;

    [Fact]
    public void TryPromote_WithContainedGeneratedJpeg_PromotesCanonicalFile()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var cache = CreateCache(temporary.Path);
        var movieId = new MovieId(42);
        var generatedPath = NewTemporaryPath(temporary.Path);
        WriteJpeg(generatedPath, 0x22);

        // act
        var promoted = cache.TryPromote(generatedPath, movieId);

        // assert
        promoted.ShouldBeTrue();
        var canonicalPath = Path.Combine(temporary.Path, "42.jpg");
        File.ReadAllBytes(canonicalPath).ShouldBe(new byte[] { SoiFirstByte, SoiSecondByte, 0x22, 0x00 });
        File.Exists(generatedPath).ShouldBeFalse();
    }

    [Fact]
    public void TryPromote_WithGeneratedFileOutsideCacheRoot_RejectsAndLeavesFileUntouched()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        var cache = CreateCache(temporary.Path);
        var generatedPath = Path.Combine(outside.Path, "unrelated.jpg");
        WriteJpeg(generatedPath, 0x33);

        // act
        var promoted = cache.TryPromote(generatedPath, new MovieId(7));

        // assert
        promoted.ShouldBeFalse();
        File.ReadAllBytes(generatedPath).ShouldBe(new byte[] { SoiFirstByte, SoiSecondByte, 0x33, 0x00 });
        HasNoFiles(temporary.Path).ShouldBeTrue();
    }

    [Fact]
    public void TryPromote_WithGeneratedFileLackingSoi_RejectsAndRemovesTemporary()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var cache = CreateCache(temporary.Path);
        var generatedPath = NewTemporaryPath(temporary.Path);
        File.WriteAllBytes(generatedPath, [0x49, 0x44]);

        // act
        var promoted = cache.TryPromote(generatedPath, new MovieId(9));

        // assert
        promoted.ShouldBeFalse();
        File.Exists(generatedPath).ShouldBeFalse();
        File.Exists(Path.Combine(temporary.Path, "9.jpg")).ShouldBeFalse();
    }

    [Fact]
    public void TryPromote_WithGeneratedFileShorterThanSoi_RejectsAndRemovesTemporary()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var cache = CreateCache(temporary.Path);
        var generatedPath = NewTemporaryPath(temporary.Path);
        File.WriteAllBytes(generatedPath, [SoiFirstByte]);

        // act
        var promoted = cache.TryPromote(generatedPath, new MovieId(11));

        // assert
        promoted.ShouldBeFalse();
        File.Exists(generatedPath).ShouldBeFalse();
    }

    [Fact]
    public void TryPromote_WhenCanonicalIsHeldWithoutSharing_PreservesPriorCanonicalJpeg()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "FileShare.None locking is Windows-specific.");
        // arrange
        using var temporary = new TemporaryDirectory();
        var cache = CreateCache(temporary.Path);
        var movieId = new MovieId(13);
        var canonicalPath = Path.Combine(temporary.Path, "13.jpg");
        WriteJpeg(canonicalPath, 0x11);
        var generatedPath = NewTemporaryPath(temporary.Path);
        WriteJpeg(generatedPath, 0x44);

        using (new FileStream(canonicalPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            // act
            var promoted = cache.TryPromote(generatedPath, movieId);

            // assert
            promoted.ShouldBeFalse();
            File.Exists(generatedPath).ShouldBeFalse();
        }

        File.ReadAllBytes(canonicalPath).ShouldBe(new byte[] { SoiFirstByte, SoiSecondByte, 0x11, 0x00 });
    }

    [Fact]
    public void TryPromote_WithCanonicalReparsePoint_RejectsAndLeavesOutsideTargetUntouched()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        var cache = CreateCache(temporary.Path);
        var movieId = new MovieId(17);
        var canonicalPath = Path.Combine(temporary.Path, "17.jpg");
        var outsidePath = Path.Combine(outside.Path, "outside.jpg");
        WriteJpeg(outsidePath, 0x55);
        Assert.SkipUnless(
            TryCreateFileLink(canonicalPath, outsidePath),
            "Symbolic link creation is not permitted in this environment.");
        var generatedPath = NewTemporaryPath(temporary.Path);
        WriteJpeg(generatedPath, 0x66);

        // act
        var promoted = cache.TryPromote(generatedPath, movieId);

        // assert
        promoted.ShouldBeFalse();
        File.ReadAllBytes(outsidePath).ShouldBe(new byte[] { SoiFirstByte, SoiSecondByte, 0x55, 0x00 });
        new FileInfo(canonicalPath).LinkTarget.ShouldNotBeNull();
        File.Exists(generatedPath).ShouldBeFalse();
    }

    [Fact]
    public void TryPromote_WithCacheRootBehindReparsePoint_Rejects()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var realRoot = Path.Combine(temporary.Path, "real");
        Directory.CreateDirectory(realRoot);
        var linkedRoot = Path.Combine(temporary.Path, "linked");
        Assert.SkipUnless(
            TryCreateDirectoryLink(linkedRoot, realRoot),
            "Symbolic link creation is not permitted in this environment.");
        var cache = CreateCache(linkedRoot);
        var generatedPath = Path.Combine(realRoot, Guid.NewGuid().ToString("N") + ".jpg");
        WriteJpeg(generatedPath, 0x77);

        // act
        var promoted = cache.TryPromote(generatedPath, new MovieId(19));

        // assert
        promoted.ShouldBeFalse();
        File.Exists(Path.Combine(realRoot, "19.jpg")).ShouldBeFalse();
    }

    [Fact]
    public void TryOpenCanonical_WithValidJpeg_ReturnsReadableStream()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var cache = CreateCache(temporary.Path);
        var canonicalPath = Path.Combine(temporary.Path, "23.jpg");
        WriteJpeg(canonicalPath, 0x88);

        // act
        var opened = cache.TryOpenCanonical(new MovieId(23), out var stream);

        // assert
        opened.ShouldBeTrue();
        using (stream)
        {
            var open = stream.ShouldNotBeNull();
            open.Length.ShouldBe(4);
            open.ReadByte().ShouldBe(SoiFirstByte);
            open.ReadByte().ShouldBe(SoiSecondByte);
        }
    }

    [Fact]
    public void TryOpenCanonical_WhileStreamIsOpen_PermitsConcurrentWriteAndDelete()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var cache = CreateCache(temporary.Path);
        var canonicalPath = Path.Combine(temporary.Path, "29.jpg");
        WriteJpeg(canonicalPath, 0x99);
        cache.TryOpenCanonical(new MovieId(29), out var stream).ShouldBeTrue();

        // act
        var sharingViolated = false;
        try
        {
            using (new FileStream(canonicalPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
            {
            }

            File.Delete(canonicalPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            sharingViolated = true;
        }

        // assert
        using (stream)
        {
            sharingViolated.ShouldBeFalse();
            stream.ShouldNotBeNull().ReadByte().ShouldBe(SoiFirstByte);
        }
    }

    [Fact]
    public void TryOpenCanonical_WithCorruptSoi_ReturnsFalse()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var cache = CreateCache(temporary.Path);
        File.WriteAllBytes(Path.Combine(temporary.Path, "31.jpg"), [0x49, 0x44, 0x33]);

        // act
        var opened = cache.TryOpenCanonical(new MovieId(31), out var stream);

        // assert
        opened.ShouldBeFalse();
        stream.ShouldBeNull();
    }

    [Fact]
    public void TryOpenCanonical_WithMissingFile_ReturnsFalse()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var cache = CreateCache(temporary.Path);

        // act
        var opened = cache.TryOpenCanonical(new MovieId(37), out var stream);

        // assert
        opened.ShouldBeFalse();
        stream.ShouldBeNull();
    }

    [Fact]
    public void TryOpenCanonical_WithCanonicalReparsePoint_ReturnsFalse()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        var cache = CreateCache(temporary.Path);
        var outsidePath = Path.Combine(outside.Path, "outside.jpg");
        WriteJpeg(outsidePath, 0xAA);
        Assert.SkipUnless(
            TryCreateFileLink(Path.Combine(temporary.Path, "41.jpg"), outsidePath),
            "Symbolic link creation is not permitted in this environment.");

        // act
        var opened = cache.TryOpenCanonical(new MovieId(41), out var stream);

        // assert
        opened.ShouldBeFalse();
        stream.ShouldBeNull();
    }

    [Fact]
    public void TryOpenCanonical_WhenCacheRootIsMissing_ReturnsFalse()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var cache = CreateCache(Path.Combine(temporary.Path, "absent"));

        // act
        var opened = cache.TryOpenCanonical(new MovieId(43), out var stream);

        // assert
        opened.ShouldBeFalse();
        stream.ShouldBeNull();
    }

    [Fact]
    public void TryPromote_WhileCanonicalIsOpenForSharedRead_ReplacesContentWithPriorReadsIntact()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var cache = CreateCache(temporary.Path);
        var movieId = new MovieId(47);
        var canonicalPath = Path.Combine(temporary.Path, "47.jpg");
        WriteJpeg(canonicalPath, 0x11);
        var generatedPath = NewTemporaryPath(temporary.Path);
        WriteJpeg(generatedPath, 0x22);
        using var priorReader = new FileStream(
            canonicalPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4096,
            FileOptions.Asynchronous);

        // act
        var promoted = cache.TryPromote(generatedPath, movieId);

        // assert
        promoted.ShouldBeTrue();
        ReadByteAt(priorReader, ContentOffset).ShouldBe(0x11);
        cache.TryOpenCanonical(movieId, out var refreshed).ShouldBeTrue();
        using (refreshed)
        {
            ReadByteAt(refreshed.ShouldNotBeNull(), ContentOffset).ShouldBe(0x22);
        }
    }

    [Fact]
    public void DeleteTemporary_WithCanonicalPath_KeepsCanonicalFile()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var cache = CreateCache(temporary.Path);
        var canonicalPath = Path.Combine(temporary.Path, "53.jpg");
        WriteJpeg(canonicalPath, 0xBB);

        // act
        cache.DeleteTemporary(canonicalPath);

        // assert
        File.Exists(canonicalPath).ShouldBeTrue();
    }

    [Fact]
    public void DeleteTemporary_WithTemporaryPath_RemovesOnlyThatFile()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var cache = CreateCache(temporary.Path);
        var generatedPath = NewTemporaryPath(temporary.Path);
        WriteJpeg(generatedPath, 0xCC);
        var neighbourPath = NewTemporaryPath(temporary.Path);
        WriteJpeg(neighbourPath, 0xDD);

        // act
        cache.DeleteTemporary(generatedPath);

        // assert
        File.Exists(generatedPath).ShouldBeFalse();
        File.Exists(neighbourPath).ShouldBeTrue();
    }

    [Fact]
    public void DeleteTemporary_WithPathOutsideCacheRoot_LeavesFileUntouched()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        var cache = CreateCache(temporary.Path);
        var outsidePath = Path.Combine(outside.Path, Guid.NewGuid().ToString("N") + ".jpg");
        WriteJpeg(outsidePath, 0xEE);

        // act
        cache.DeleteTemporary(outsidePath);

        // assert
        File.Exists(outsidePath).ShouldBeTrue();
    }

    private static bool HasNoFiles(string path) =>
        Directory.GetFileSystemEntries(path).Length == 0;

    private static ThumbnailCache CreateCache(string cacheRoot)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(cacheRoot);
        var options = Microsoft.Extensions.Options.Options.Create(
            new ThumbnailOptions { CacheDirectory = cacheRoot });
        return new ThumbnailCache(options, environment);
    }

    private static string NewTemporaryPath(string root) =>
        Path.Combine(root, Guid.NewGuid().ToString("N") + ".jpg");

    private static void WriteJpeg(string path, byte contentByte)
    {
        File.WriteAllBytes(path, [SoiFirstByte, SoiSecondByte, contentByte, 0x00]);
    }

    private static int ReadByteAt(FileStream stream, int offset)
    {
        stream.Position = offset;
        return stream.ReadByte();
    }

    private static bool TryCreateFileLink(string linkPath, string targetPath)
    {
        try
        {
            File.CreateSymbolicLink(linkPath, targetPath);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
    }

    private static bool TryCreateDirectoryLink(string linkPath, string targetPath)
    {
        try
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "lamuflix-thumbnail-cache-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Test cleanup is best effort.
            }
        }
    }
}
