using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.FileSystem;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace LamuFlix.UnitTests.FileSystem;

public sealed class LibraryFileReaderTests
{
    private const string MovieFolder = "Heat (1995)";
    private const string PrimaryFileName = "Heat.mkv";
    private const int BufferFreePayloadLength = 8192;

    [Fact]
    public void AuthorizeFolder_RelativeContainedFolder_Succeeds()
    {
        // arrange
        using var library = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), Payload());
        var reader = CreateReader(library.Root);

        // act
        var act = () => reader.AuthorizeFolder(new LibraryPath(MovieFolder));

        // assert
        Should.NotThrow(act);
    }

    [Fact]
    public void AuthorizeFolder_LegacyRootedContainedFolder_Succeeds()
    {
        // arrange
        using var library = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), Payload());
        var reader = CreateReader(library.Root);
        var rootedFolder = Path.Combine(library.Root, MovieFolder);

        // act
        var act = () => reader.AuthorizeFolder(new LibraryPath(rootedFolder));

        // assert
        Should.NotThrow(act);
    }

    [Fact]
    public void OpenRead_RelativeContainedPrimaryFile_ReturnsSeekableStreamOfExactBytes()
    {
        // arrange
        using var library = new TemporaryDirectory();
        var expected = Payload();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), expected);
        var reader = CreateReader(library.Root);

        // act
        using var stream = reader.OpenRead(new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName)));

        // assert
        stream.CanRead.ShouldBeTrue();
        stream.CanSeek.ShouldBeTrue();
        stream.Length.ShouldBe(expected.Length);
        ReadAllBytes(stream).ShouldBe(expected);
    }

    [Fact]
    public void OpenRead_LegacyRootedContainedFile_ReturnsExactBytes()
    {
        // arrange
        using var library = new TemporaryDirectory();
        var expected = Payload();
        var rootedFile = library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), expected);
        var reader = CreateReader(library.Root);

        // act
        using var stream = reader.OpenRead(new LibraryPath(rootedFile));

        // assert
        ReadAllBytes(stream).ShouldBe(expected);
    }

    [Fact]
    public void AuthorizeFolder_RootedFolderOutsideRoot_ThrowsAccessDenied()
    {
        // arrange
        using var library = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        outside.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), Payload());
        var reader = CreateReader(library.Root);
        var outsideFolder = Path.Combine(outside.Root, MovieFolder);

        // act
        var act = () => reader.AuthorizeFolder(new LibraryPath(outsideFolder));

        // assert
        Should.Throw<LibraryFileAccessDeniedException>(act);
    }

    [Fact]
    public void AuthorizeFolder_RootedSiblingPrefixFolder_ThrowsAccessDenied()
    {
        // arrange
        using var library = new TemporaryDirectory();
        var siblingRoot = library.Root + "-sibling";
        Directory.CreateDirectory(Path.Combine(siblingRoot, MovieFolder));
        try
        {
            var reader = CreateReader(library.Root);
            var siblingFolder = Path.Combine(siblingRoot, MovieFolder);

            // act
            var act = () => reader.AuthorizeFolder(new LibraryPath(siblingFolder));

            // assert
            Should.Throw<LibraryFileAccessDeniedException>(act);
        }
        finally
        {
            Directory.Delete(siblingRoot, recursive: true);
        }
    }

    [Fact]
    public void OpenRead_RootedSiblingPrefixFile_ThrowsAccessDenied()
    {
        // arrange
        using var library = new TemporaryDirectory();
        var siblingRoot = library.Root + "-sibling";
        Directory.CreateDirectory(Path.Combine(siblingRoot, MovieFolder));
        var siblingFile = Path.Combine(siblingRoot, MovieFolder, PrimaryFileName);
        File.WriteAllBytes(siblingFile, Payload());
        try
        {
            var reader = CreateReader(library.Root);

            // act
            var act = () => reader.OpenRead(new LibraryPath(siblingFile));

            // assert
            Should.Throw<LibraryFileAccessDeniedException>(act);
        }
        finally
        {
            Directory.Delete(siblingRoot, recursive: true);
        }
    }

    [Fact]
    public void AuthorizeFolder_LinkedFolderInsideRoot_ThrowsAccessDeniedEvenWhenTargetIsContained()
    {
        // arrange
        using var library = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), Payload());
        CreateDirectoryLinkOrFail(Path.Combine(library.Root, "Linked"), Path.Combine(library.Root, MovieFolder));
        var reader = CreateReader(library.Root);

        // act
        var act = () => reader.AuthorizeFolder(new LibraryPath("Linked"));

        // assert
        Should.Throw<LibraryFileAccessDeniedException>(act);
    }

    [Fact]
    public void OpenRead_LinkedFileInsideRoot_ThrowsAccessDenied()
    {
        // arrange
        using var library = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        var outsideFile = outside.WriteFile(PrimaryFileName, Payload());
        Directory.CreateDirectory(Path.Combine(library.Root, MovieFolder));
        CreateFileLinkOrFail(Path.Combine(library.Root, MovieFolder, PrimaryFileName), outsideFile);
        var reader = CreateReader(library.Root);

        // act
        var act = () => reader.OpenRead(new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName)));

        // assert
        Should.Throw<LibraryFileAccessDeniedException>(act);
    }

    [Fact]
    public void OpenRead_FileInsideLinkedFolder_ThrowsAccessDenied()
    {
        // arrange
        using var library = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        outside.WriteFile(PrimaryFileName, Payload());
        CreateDirectoryLinkOrFail(Path.Combine(library.Root, "Linked"), outside.Root);
        var reader = CreateReader(library.Root);

        // act
        var act = () => reader.OpenRead(new LibraryPath(Path.Combine("Linked", PrimaryFileName)));

        // assert
        Should.Throw<LibraryFileAccessDeniedException>(act);
    }

    [Fact]
    public void AuthorizeFolder_MissingFolder_ThrowsNotFound()
    {
        // arrange
        using var library = new TemporaryDirectory();
        var reader = CreateReader(library.Root);

        // act
        var act = () => reader.AuthorizeFolder(new LibraryPath("Absent (1999)"));

        // assert
        Should.Throw<NotFoundException>(act);
    }

    [Fact]
    public void OpenRead_MissingFile_ThrowsNotFound()
    {
        // arrange
        using var library = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(library.Root, MovieFolder));
        var reader = CreateReader(library.Root);

        // act
        var act = () => reader.OpenRead(new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName)));

        // assert
        Should.Throw<NotFoundException>(act);
    }

    [Fact]
    public void AuthorizeFolder_ContainedFilePath_ThrowsAccessDeniedForNonRegularPath()
    {
        // arrange
        using var library = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), Payload());
        var reader = CreateReader(library.Root);

        // act
        var act = () => reader.AuthorizeFolder(new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName)));

        // assert
        Should.Throw<LibraryFileAccessDeniedException>(act);
    }

    [Fact]
    public void OpenRead_ContainedDirectoryPath_ThrowsAccessDeniedForNonRegularFile()
    {
        // arrange
        using var library = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(library.Root, MovieFolder));
        var reader = CreateReader(library.Root);

        // act
        var act = () => reader.OpenRead(new LibraryPath(MovieFolder));

        // assert
        Should.Throw<LibraryFileAccessDeniedException>(act);
    }

    [Fact]
    public void OpenRead_DanglingLinkedFileInsideRoot_ThrowsAccessDeniedInsteadOfNotFound()
    {
        // arrange
        using var library = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(library.Root, MovieFolder));
        CreateFileLinkOrFail(
            Path.Combine(library.Root, MovieFolder, PrimaryFileName),
            Path.Combine(library.Root, MovieFolder, "Absent-target.mkv"));
        var reader = CreateReader(library.Root);

        // act
        var act = () => reader.OpenRead(new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName)));

        // assert
        Should.Throw<LibraryFileAccessDeniedException>(act);
    }

    [Fact]
    public void OpenRead_DanglingLinkedFolderInsideRoot_ThrowsAccessDeniedInsteadOfNotFound()
    {
        // arrange
        using var library = new TemporaryDirectory();
        CreateDirectoryLinkOrFail(
            Path.Combine(library.Root, "Linked"),
            Path.Combine(library.Root, "Absent-target"));
        var reader = CreateReader(library.Root);

        // act
        var act = () => reader.OpenRead(new LibraryPath(Path.Combine("Linked", PrimaryFileName)));

        // assert
        Should.Throw<LibraryFileAccessDeniedException>(act);
    }

    [Fact]
    public void OpenRead_NonRegularFifoFile_ThrowsAccessDenied()
    {
        Assert.SkipUnless(!OperatingSystem.IsWindows(), "FIFO fixtures require a Unix platform.");
        // arrange
        using var library = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(library.Root, MovieFolder));
        CreateFifoOrFail(Path.Combine(library.Root, MovieFolder, PrimaryFileName));
        var reader = CreateReader(library.Root);

        // act
        var act = () => reader.OpenRead(new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName)));

        // assert
        Should.Throw<LibraryFileAccessDeniedException>(act);
    }

    [Fact]
    public void OpenRead_OutsideRootFileHeldExclusively_ThrowsAccessDeniedBeforeOpening()
    {
        // arrange
        using var library = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        var outsideFile = outside.WriteFile(PrimaryFileName, Payload());
        var reader = CreateReader(library.Root);

        using (new FileStream(outsideFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            // act
            var act = () => reader.OpenRead(new LibraryPath(outsideFile));

            // assert
            Should.Throw<LibraryFileAccessDeniedException>(act);
        }
    }

    [Fact]
    public void OpenRead_ContainedFileHeldExclusively_PropagatesSharingViolationInsteadOfPolicyDenial()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Exclusive FileShare locks are enforced on Windows only.");
        // arrange
        using var library = new TemporaryDirectory();
        var containedFile = library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), Payload());
        var reader = CreateReader(library.Root);

        using (new FileStream(containedFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            // act
            var act = () => reader.OpenRead(new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName)));

            // assert
            Should.Throw<IOException>(act);
        }
    }

    [Fact]
    public void OpenRead_ContainedFile_OpensOnceOnTheAuthorizedCanonicalPath()
    {
        // arrange
        using var library = new TemporaryDirectory();
        var containedFile = library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), Payload());
        var reader = CreateReader(library.Root);

        // act
        using var stream = reader.OpenRead(new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName)));

        // assert
        var fileStream = stream.ShouldBeAssignableTo<FileStream>();
        fileStream.Name.ShouldBe(Path.GetFullPath(containedFile));
        fileStream.CanSeek.ShouldBeTrue();
        fileStream.Length.ShouldBe(new FileInfo(containedFile).Length);
    }

    [Fact]
    public void OpenRead_ContainedFile_AdmitsConcurrentReadersAndRefusesWriters()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "FileShare writer exclusion is enforced on Windows only.");
        // arrange
        using var library = new TemporaryDirectory();
        var containedFile = library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), Payload());
        var reader = CreateReader(library.Root);
        using var stream = reader.OpenRead(new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName)));

        // act
        var writerRefused = false;
        try
        {
            using var writer = new FileStream(containedFile, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        }
        catch (IOException)
        {
            writerRefused = true;
        }

        using var secondReader = new FileStream(containedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

        // assert
        writerRefused.ShouldBeTrue();
        secondReader.Length.ShouldBe(new FileInfo(containedFile).Length);
    }

    [Fact]
    public async Task OpenRead_ConcurrentReaders_ReturnMatchingBytes()
    {
        // arrange
        using var library = new TemporaryDirectory();
        var expected = Payload();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), expected);
        var reader = CreateReader(library.Root);
        var relative = new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName));

        // act
        var reads = Enumerable.Range(0, 4)
            .Select(_ => Task.Run(() =>
            {
                using var stream = reader.OpenRead(relative);
                return ReadAllBytes(stream);
            }))
            .ToArray();
        var results = await Task.WhenAll(reads);

        // assert
        results.Length.ShouldBe(4);
        results.ShouldAllBe(bytes => bytes.SequenceEqual(expected));
    }

    [Fact]
    public void EnumerateFiles_EligibleSidecarsAndUnrelatedFiles_ReturnsOnlyTopLevelRegularFileNamesInOrdinalOrder()
    {
        // arrange
        using var library = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), Payload());
        library.WriteFile(Path.Combine(MovieFolder, "Heat.en.srt"), Payload());
        library.WriteFile(Path.Combine(MovieFolder, "Heat.srt"), Payload());
        library.WriteFile(Path.Combine(MovieFolder, "nested", "Heat.de.srt"), Payload());
        var reader = CreateReader(library.Root);

        // act
        var files = reader.EnumerateFiles(new LibraryPath(MovieFolder));

        // assert
        files.ShouldBe(["Heat.en.srt", PrimaryFileName, "Heat.srt"]);
    }

    [Fact]
    public void EnumerateFiles_FolderOutsideRoot_ThrowsAccessDenied()
    {
        // arrange
        using var library = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        outside.WriteFile(Path.Combine(MovieFolder, "Heat.en.srt"), Payload());
        var reader = CreateReader(library.Root);
        var outsideFolder = Path.Combine(outside.Root, MovieFolder);

        // act
        var act = () => reader.EnumerateFiles(new LibraryPath(outsideFolder));

        // assert
        Should.Throw<LibraryFileAccessDeniedException>(act);
    }

    [Fact]
    public void EnumerateFiles_MissingFolder_ThrowsNotFound()
    {
        // arrange
        using var library = new TemporaryDirectory();
        var reader = CreateReader(library.Root);

        // act
        var act = () => reader.EnumerateFiles(new LibraryPath(MovieFolder));

        // assert
        Should.Throw<NotFoundException>(act);
    }

    [Fact]
    public void EnumerateFiles_RegularFilePath_ThrowsAccessDenied()
    {
        // arrange
        using var library = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), Payload());
        var reader = CreateReader(library.Root);

        // act
        var act = () => reader.EnumerateFiles(new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName)));

        // assert
        Should.Throw<LibraryFileAccessDeniedException>(act);
    }

    [Fact]
    public void EnumerateFiles_LinkedFolderInsideRoot_ThrowsAccessDeniedEvenWhenTargetIsContained()
    {
        // arrange
        using var library = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, "Heat.en.srt"), Payload());
        var linkPath = Path.Combine(library.Root, "linked-movie");
        CreateDirectoryLinkOrFail(linkPath, Path.Combine(library.Root, MovieFolder));
        var reader = CreateReader(library.Root);

        // act
        var act = () => reader.EnumerateFiles(new LibraryPath("linked-movie"));

        // assert
        Should.Throw<LibraryFileAccessDeniedException>(act);
    }

    private static ILibraryFileReader CreateReader(string rootPath)
    {
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ContentRootPath.Returns(rootPath);
        return new LibraryFileReader(
            Microsoft.Extensions.Options.Options.Create(new LibraryOptions { RootPath = rootPath }),
            hostEnvironment);
    }

    private static byte[] Payload()
    {
        var payload = new byte[BufferFreePayloadLength];
        for (var index = 0; index < payload.Length; index++)
        {
            payload[index] = (byte)(index % 251);
        }

        return payload;
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static void CreateFileLinkOrFail(string linkPath, string targetPath)
    {
        try
        {
            File.CreateSymbolicLink(linkPath, targetPath);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            Assert.Fail($"A real symlink fixture is mandatory for this proof but could not be created: {exception.Message}");
        }
    }

    private static void CreateDirectoryLinkOrFail(string linkPath, string targetPath)
    {
        try
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            Assert.Fail($"A real symlink fixture is mandatory for this proof but could not be created: {exception.Message}");
        }
    }

    private static void CreateFifoOrFail(string fifoPath)
    {
        try
        {
            var created = NativeMkfifo(fifoPath);
            if (created != 0)
            {
                Assert.Fail($"A real FIFO fixture is mandatory for this proof but could not be created: errno {Marshal.GetLastWin32Error()}.");
            }
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            Assert.Fail($"A real FIFO fixture is mandatory for this proof but could not be created: {exception.Message}");
        }
    }

    private static int NativeMkfifo(string fifoPath)
    {
        try
        {
            return MkfifoLibc(fifoPath, 0x180);
        }
        catch (DllNotFoundException)
        {
            return MkfifoLibSystem(fifoPath, 0x180);
        }
    }

    [DllImport("libc", SetLastError = true, CharSet = CharSet.Ansi, EntryPoint = "mkfifo")]
    private static extern int MkfifoLibc(string pathname, uint mode);

    [DllImport("libSystem.dylib", SetLastError = true, CharSet = CharSet.Ansi, EntryPoint = "mkfifo")]
    private static extern int MkfifoLibSystem(string pathname, uint mode);

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                "lamuflix-library-file-reader-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public string WriteFile(string relativePath, byte[] contents)
        {
            var fullPath = Path.Combine(Root, relativePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(fullPath, contents);
            return fullPath;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Test cleanup is best effort.
            }
        }
    }
}
