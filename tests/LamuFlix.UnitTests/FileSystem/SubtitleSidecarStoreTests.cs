using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.FileSystem;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace LamuFlix.UnitTests.FileSystem;

public sealed class SubtitleSidecarStoreTests
{
    private const string MovieFolder = "Heat (1995)";
    private const string PrimaryFileName = "Heat.1995.1080p.mkv";
    private const string SidecarFileName = "Heat.1995.1080p.srt";
    private const int ServingBound = 4096;

    [Fact]
    public async Task ExistsAsync_ExactBasenameSidecarPresent_ReturnsTrue()
    {
        // arrange
        using var library = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), PrimaryPayload());
        library.WriteFile(Path.Combine(MovieFolder, SidecarFileName), SidecarPayload());
        var store = CreateStore(library.Root);
        var ct = TestContext.Current.CancellationToken;

        // act
        var exists = await store.ExistsAsync(new LibraryPath(MovieFolder), PrimaryFileName, ct);

        // assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsAsync_OnlyLanguageSuffixedSidecars_ReturnsFalse()
    {
        // arrange
        using var library = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), PrimaryPayload());
        library.WriteFile(Path.Combine(MovieFolder, "Heat.1995.1080p.en.srt"), SidecarPayload());
        library.WriteFile(Path.Combine(MovieFolder, "Heat.1995.1080p.pob.srt"), SidecarPayload());
        var store = CreateStore(library.Root);
        var ct = TestContext.Current.CancellationToken;

        // act
        var exists = await store.ExistsAsync(new LibraryPath(MovieFolder), PrimaryFileName, ct);

        // assert
        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsAsync_MissingFolder_ThrowsNotFound()
    {
        // arrange
        using var library = new TemporaryDirectory();
        var store = CreateStore(library.Root);
        var ct = TestContext.Current.CancellationToken;

        // act
        var act = () => store.ExistsAsync(new LibraryPath("Absent (1999)"), PrimaryFileName, ct);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task ReadAsync_ExactBasenameSidecar_ReturnsExactBytes()
    {
        // arrange
        using var library = new TemporaryDirectory();
        var expected = SidecarPayload();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), PrimaryPayload());
        library.WriteFile(Path.Combine(MovieFolder, SidecarFileName), expected);
        var store = CreateStore(library.Root);
        var ct = TestContext.Current.CancellationToken;

        // act
        var bytes = await store.ReadAsync(new LibraryPath(MovieFolder), PrimaryFileName, ServingBound, ct);

        // assert
        bytes.ShouldBe(expected);
    }

    [Fact]
    public async Task ReadAsync_SidecarAbsent_ThrowsNotFound()
    {
        // arrange
        using var library = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), PrimaryPayload());
        var store = CreateStore(library.Root);
        var ct = TestContext.Current.CancellationToken;

        // act
        var act = () => store.ReadAsync(new LibraryPath(MovieFolder), PrimaryFileName, ServingBound, ct);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task ReadAsync_PayloadExactlyAtBound_ReturnsBytes()
    {
        // arrange
        using var library = new TemporaryDirectory();
        var payload = SidecarPayload();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), PrimaryPayload());
        library.WriteFile(Path.Combine(MovieFolder, SidecarFileName), payload);
        var store = CreateStore(library.Root);
        var ct = TestContext.Current.CancellationToken;

        // act
        var bytes = await store.ReadAsync(new LibraryPath(MovieFolder), PrimaryFileName, payload.Length, ct);

        // assert
        bytes.Length.ShouldBe(payload.Length);
    }

    [Fact]
    public async Task ReadAsync_PayloadOverBound_ThrowsValidation()
    {
        // arrange
        using var library = new TemporaryDirectory();
        var payload = SidecarPayload();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), PrimaryPayload());
        library.WriteFile(Path.Combine(MovieFolder, SidecarFileName), payload);
        var store = CreateStore(library.Root);
        var ct = TestContext.Current.CancellationToken;

        // act
        var act = () => store.ReadAsync(new LibraryPath(MovieFolder), PrimaryFileName, payload.Length - 1, ct);

        // assert
        await Should.ThrowAsync<ValidationException>(act);
    }

    [Fact]
    public async Task ReadAsync_SidecarOutsideAuthorizedRoot_ThrowsAccessDenied()
    {
        // arrange
        using var library = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), PrimaryPayload());
        outside.WriteFile(Path.Combine(MovieFolder, SidecarFileName), SidecarPayload());
        var store = CreateStore(library.Root);
        var ct = TestContext.Current.CancellationToken;

        // act
        var act = () => store.ReadAsync(
            new LibraryPath(Path.Combine(outside.Root, MovieFolder)),
            PrimaryFileName,
            ServingBound,
            ct);

        // assert
        await Should.ThrowAsync<LibraryFileAccessDeniedException>(act);
    }

    [Fact]
    public async Task ReadAsync_LinkedSidecar_ThrowsAccessDeniedWithoutServingTargetBytes()
    {
        // arrange
        using var library = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), PrimaryPayload());
        var target = outside.WriteFile("outside.srt", SidecarPayload());
        CreateFileLinkOrFail(Path.Combine(library.Root, MovieFolder, SidecarFileName), target);
        var store = CreateStore(library.Root);
        var ct = TestContext.Current.CancellationToken;

        // act
        var act = () => store.ReadAsync(new LibraryPath(MovieFolder), PrimaryFileName, ServingBound, ct);

        // assert
        await Should.ThrowAsync<LibraryFileAccessDeniedException>(act);
    }

    [Fact]
    public async Task CreateAsync_AbsentSidecar_CreatesExactBasenameAtomicallyWithExactBytes()
    {
        // arrange
        using var library = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), PrimaryPayload());
        var store = CreateStore(library.Root);
        var ct = TestContext.Current.CancellationToken;
        var content = SidecarPayload();

        // act
        var result = await store.CreateAsync(new LibraryPath(MovieFolder), PrimaryFileName, content, ct);

        // assert
        result.ShouldBe(SubtitleSidecarWriteResult.Created);
        var created = Path.Combine(library.Root, MovieFolder, SidecarFileName);
        File.ReadAllBytes(created).ShouldBe(content);
        Directory.GetFiles(Path.Combine(library.Root, MovieFolder), "*.tmp").ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_ExistingSidecar_ReturnsAlreadyPresentAndNeverOverwrites()
    {
        // arrange
        using var library = new TemporaryDirectory();
        var original = SidecarPayload();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), PrimaryPayload());
        library.WriteFile(Path.Combine(MovieFolder, SidecarFileName), original);
        var store = CreateStore(library.Root);
        var ct = TestContext.Current.CancellationToken;

        // act
        var result = await store.CreateAsync(
            new LibraryPath(MovieFolder),
            PrimaryFileName,
            "replacement content"u8.ToArray(),
            ct);

        // assert
        result.ShouldBe(SubtitleSidecarWriteResult.AlreadyPresent);
        File.ReadAllBytes(Path.Combine(library.Root, MovieFolder, SidecarFileName)).ShouldBe(original);
    }

    [Fact]
    public async Task CreateAsync_ConcurrentCreates_ExactlyOneWinsAndTheRestReportAlreadyPresent()
    {
        // arrange
        using var library = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), PrimaryPayload());
        var root = library.Root;
        var ct = TestContext.Current.CancellationToken;
        using var start = new Barrier(2);

        Task<SubtitleSidecarWriteResult> Create(int fill) => Task.Run(
            async () =>
            {
                var store = CreateStore(root);
                start.SignalAndWait(ct);
                return await store.CreateAsync(
                    new LibraryPath(MovieFolder),
                    PrimaryFileName,
                    Enumerable.Repeat((byte)fill, 64).ToArray(),
                    ct);
            },
            ct);

        // act
        var results = await Task.WhenAll(Create(7), Create(9));

        // assert
        results.Count(result => result == SubtitleSidecarWriteResult.Created).ShouldBe(1);
        results.Count(result => result == SubtitleSidecarWriteResult.AlreadyPresent).ShouldBe(1);
        var content = File.ReadAllBytes(Path.Combine(root, MovieFolder, SidecarFileName));
        content.Length.ShouldBe(64);
        content.ShouldAllBe(value => value == content[0]);
    }

    [Fact]
    public async Task CreateAsync_CancelledBeforeCreate_LeavesNoSidecarAndNoTempFile()
    {
        // arrange
        using var library = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), PrimaryPayload());
        var store = CreateStore(library.Root);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // act
        var act = () => store.CreateAsync(
            new LibraryPath(MovieFolder),
            PrimaryFileName,
            SidecarPayload(),
            cancellation.Token);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
        var folder = Path.Combine(library.Root, MovieFolder);
        File.Exists(Path.Combine(folder, SidecarFileName)).ShouldBeFalse();
        Directory.GetFiles(folder, "*.tmp").ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_FolderOutsideAuthorizedRoot_ThrowsAccessDeniedWithoutWriting()
    {
        // arrange
        using var library = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        outside.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), PrimaryPayload());
        var store = CreateStore(library.Root);
        var ct = TestContext.Current.CancellationToken;

        // act
        var act = () => store.CreateAsync(
            new LibraryPath(Path.Combine(outside.Root, MovieFolder)),
            PrimaryFileName,
            SidecarPayload(),
            ct);

        // assert
        await Should.ThrowAsync<LibraryFileAccessDeniedException>(act);
        Directory.GetFiles(Path.Combine(outside.Root, MovieFolder), "*.srt").ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_PathTraversalFolder_ThrowsAccessDenied()
    {
        // arrange
        using var library = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), PrimaryPayload());
        var store = CreateStore(library.Root);
        var ct = TestContext.Current.CancellationToken;

        // act
        var act = () => store.CreateAsync(
            new LibraryPath(MovieFolder),
            "../../escape.mkv",
            SidecarPayload(),
            ct);

        // assert
        await Should.ThrowAsync<LibraryFileAccessDeniedException>(act);
        Directory.GetFiles(library.Root, "*.srt", SearchOption.AllDirectories).ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_TraversalPrimaryFileName_ThrowsAccessDenied()
    {
        // arrange
        using var library = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), PrimaryPayload());
        var store = CreateStore(library.Root);
        var ct = TestContext.Current.CancellationToken;

        // act
        var act = () => store.CreateAsync(
            new LibraryPath(MovieFolder),
            "..",
            SidecarPayload(),
            ct);

        // assert
        await Should.ThrowAsync<LibraryFileAccessDeniedException>(act);
    }

    [Fact]
    public async Task CreateAsync_LinkedExistingSidecarTarget_FailsClosedWithoutWriting()
    {
        // arrange
        using var library = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        library.WriteFile(Path.Combine(MovieFolder, PrimaryFileName), PrimaryPayload());
        var target = outside.WriteFile("outside.srt", SidecarPayload());
        CreateFileLinkOrFail(Path.Combine(library.Root, MovieFolder, SidecarFileName), target);
        var store = CreateStore(library.Root);
        var ct = TestContext.Current.CancellationToken;

        // act
        var act = () => store.CreateAsync(
            new LibraryPath(MovieFolder),
            PrimaryFileName,
            "replacement"u8.ToArray(),
            ct);

        // assert
        await Should.ThrowAsync<LibraryFileAccessDeniedException>(act);
        File.ReadAllBytes(target).ShouldBe(SidecarPayload());
    }

    [Fact]
    public async Task CreateAsync_MissingPrimaryVideo_ThrowsNotFoundWithoutWriting()
    {
        // arrange
        using var library = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(library.Root, MovieFolder));
        var store = CreateStore(library.Root);
        var ct = TestContext.Current.CancellationToken;

        // act
        var act = () => store.CreateAsync(
            new LibraryPath(MovieFolder),
            PrimaryFileName,
            SidecarPayload(),
            ct);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
        Directory.GetFiles(Path.Combine(library.Root, MovieFolder), "*.srt").ShouldBeEmpty();
    }

    private static SubtitleSidecarStore CreateStore(string rootPath)
    {
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ContentRootPath.Returns(rootPath);
        var reader = new LibraryFileReader(
            Microsoft.Extensions.Options.Options.Create(new LibraryOptions { RootPath = rootPath }),
            hostEnvironment);
        return new SubtitleSidecarStore(
            reader,
            Microsoft.Extensions.Options.Options.Create(new LibraryOptions { RootPath = rootPath }),
            hostEnvironment);
    }

    private static byte[] PrimaryPayload() => [0x1A, 0x45, 0xDF, 0xA3, 0x42];

    private static byte[] SidecarPayload() =>
        "1\n00:00:01,000 --> 00:00:02,000\nhello subtitle\n"u8.ToArray();

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

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                "lamuflix-subtitle-sidecar-" + Guid.NewGuid().ToString("N"));
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
